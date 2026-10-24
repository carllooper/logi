namespace Loupedeck.ConsoleDeckPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ConsoleDeck.Protocol;

    // The connection from the plugin to the ConsoleDeck app.
    // It connects to the app on the loopback address, reconnects every two seconds while the app is not running,
    // sends input messages, and keeps the latest feedback state that the app sent for each control.
    // No SDK dependency, so it can be reused or tested on its own.

    public sealed class DeckLink : IDisposable
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<String, DeckMessage> _states = new ConcurrentDictionary<String, DeckMessage>();
        private readonly Object _writeLock = new Object();
        private StreamWriter _writer;

        // Raised (on a background thread) when the app sends new feedback for a control. The argument is the control id.
        public event EventHandler<String> StateChanged;

        // Raised (on a background thread) when the connection to the app opens or closes.
        public event EventHandler ConnectionChanged;

        public Boolean IsConnected => this._writer != null;

        public void Start() => Task.Run(() => this.RunAsync(this._cancellation.Token));

        // Returns the latest feedback for a control, or null if the app has not sent any.
        public DeckMessage GetState(String id) => this._states.TryGetValue(id, out var state) ? state : null;

        // Sends a message to the app. Inputs made while the app is not running are dropped.
        public void Send(DeckMessage message)
        {
            lock (this._writeLock)
            {
                if (this._writer == null)
                {
                    PluginLog.Verbose($"No app, input dropped: {message.ToJson()}");
                    return;
                }

                PluginLog.Verbose($"Input: {message.ToJson()}");

                try
                {
                    this._writer.WriteLine(message.ToJson());
                }
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                {
                    // The read loop notices the broken connection and reconnects.
                }
            }
        }

        public void Dispose()
        {
            this._cancellation.Cancel();
            this.CloseWriter();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(IPAddress.Loopback, DeckMessage.Port, cancellationToken);
                        client.NoDelay = true;

                        var stream = client.GetStream();
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            lock (this._writeLock)
                            {
                                this._writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                            }

                            PluginLog.Info("Connected to the ConsoleDeck app");
                            this.ConnectionChanged?.Invoke(this, EventArgs.Empty);

                            String line;
                            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                            {
                                this.OnMessage(DeckMessage.FromJson(line));
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex) when (ex is IOException || ex is SocketException)
                {
                    // The app is not running, or it was closed. Try again shortly.
                }

                if (this.CloseWriter())
                {
                    PluginLog.Info("Disconnected from the ConsoleDeck app");
                    this._states.Clear();
                    this.ConnectionChanged?.Invoke(this, EventArgs.Empty);
                }

                try
                {
                    await Task.Delay(RetryDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private void OnMessage(DeckMessage message)
        {
            if (message?.Type == "state" && !String.IsNullOrEmpty(message.Id))
            {
                this._states[message.Id] = message;
                this.StateChanged?.Invoke(this, message.Id);
            }
        }

        // Returns true if a connection was open.
        private Boolean CloseWriter()
        {
            lock (this._writeLock)
            {
                if (this._writer == null)
                {
                    return false;
                }

                try
                {
                    this._writer.Dispose();
                }
                catch (IOException)
                {
                }

                this._writer = null;
                return true;
            }
        }
    }
}
