namespace ConsoleDeck.App
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ConsoleDeck.Protocol;

    // Listens on the loopback address for the ConsoleDeck plugin, which runs inside the Logi Plugin Service.
    // Only one plugin connection is kept; a new one replaces the old one.
    // Events are raised on a background thread.

    public sealed class DeckServer : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Object _writeLock = new Object();
        private TcpListener _listener;
        private TcpClient _client;
        private StreamWriter _writer;

        // Raised for every message that the plugin sends.
        public event EventHandler<DeckMessage> MessageReceived;

        // Raised when the plugin connects or disconnects.
        public event EventHandler ConnectionChanged;

        public Boolean IsConnected => this._writer != null;

        // Throws `SocketException` if the port is in use, for example when the app is already running.
        public void Start()
        {
            this._listener = new TcpListener(IPAddress.Loopback, DeckMessage.Port);
            this._listener.Start();
            Task.Run(() => this.AcceptLoopAsync(this._cancellation.Token));
        }

        public void Send(DeckMessage message)
        {
            lock (this._writeLock)
            {
                try
                {
                    this._writer?.WriteLine(message.ToJson());
                }
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                {
                    // The read loop notices the broken connection.
                }
            }
        }

        public void Dispose()
        {
            this._cancellation.Cancel();
            this._listener?.Stop();
            this.CloseClient(this._client);
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await this._listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is SocketException || ex is ObjectDisposedException)
                {
                    return;
                }

                this.CloseClient(this._client);
                _ = Task.Run(() => this.ReadLoopAsync(client, cancellationToken));
            }
        }

        private async Task ReadLoopAsync(TcpClient client, CancellationToken cancellationToken)
        {
            client.NoDelay = true;
            var stream = client.GetStream();

            lock (this._writeLock)
            {
                this._client = client;
                this._writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            }

            this.ConnectionChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    String line;
                    while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                    {
                        var message = DeckMessage.FromJson(line);
                        if (message != null)
                        {
                            this.MessageReceived?.Invoke(this, message);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is ObjectDisposedException)
            {
            }

            if (this.CloseClient(client))
            {
                this.ConnectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Closes the given connection if it is the current one. Returns true if it was.
        private Boolean CloseClient(TcpClient client)
        {
            lock (this._writeLock)
            {
                if (client == null || client != this._client)
                {
                    client?.Dispose();
                    return false;
                }

                this._writer = null;
                this._client = null;
                client.Dispose();
                return true;
            }
        }
    }
}
