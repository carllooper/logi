namespace ConsoleDeck.App
{
    using System;
    using System.Net.Sockets;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Primitives;
    using Avalonia.Input;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Avalonia.Threading;

    using ConsoleDeck.Protocol;

    // The main window: an on-screen keypad and dialpad that mirror the MX Creative Console.
    // Inputs arrive from the ConsoleDeck plugin through `DeckServer`, and every change is sent back
    // so the hardware keys show the same state as the window.

    public partial class MainWindow : Window
    {
        private const Int32 KeyCount = 9;
        private const Int32 ButtonCount = 4;
        private const Int32 MaxLogEntries = 200;
        private static readonly TimeSpan FlashTime = TimeSpan.FromMilliseconds(180);

        // One color per keypad key, used when the key is on.
        private static readonly String[] KeyColors =
        {
            "#E53935", "#FB8C00", "#FDD835", "#43A047", "#00ACC1", "#1E88E5", "#5E35B1", "#D81B60", "#6D4C41",
        };

        // One accent per dialpad button. The selected accent colors the dial and roller.
        private static readonly String[] ButtonColors = { "#4FC3F7", "#81C784", "#FFB74D", "#F06292" };

        private const String OffColor = "#2B2F36";

        private readonly DeckServer _server = new DeckServer();

        private readonly Boolean[] _keyOn = new Boolean[KeyCount];
        private readonly Int32[] _keyPresses = new Int32[KeyCount];
        private readonly Border[] _keyTiles = new Border[KeyCount];
        private readonly TextBlock[] _keyStateTexts = new TextBlock[KeyCount];
        private readonly TextBlock[] _keyCountTexts = new TextBlock[KeyCount];

        private readonly Int32[] _buttonPresses = new Int32[ButtonCount];
        private readonly Border[] _buttonTiles = new Border[ButtonCount];
        private readonly TextBlock[] _buttonCountTexts = new TextBlock[ButtonCount];
        private Int32 _selectedButton;

        private Int32 _dial = 50;
        private Int32 _roller = 50;

        public MainWindow()
        {
            this.InitializeComponent();

            this.CreateKeyTiles();
            this.CreateButtonTiles();

            this.DialPanel.PointerWheelChanged += (s, e) => this.TurnDial(Math.Sign(e.Delta.Y), "mouse");
            this.RollerPanel.PointerWheelChanged += (s, e) => this.TurnRoller(Math.Sign(e.Delta.Y), "mouse");

            this.UpdateAll();

            this._server.MessageReceived += (s, message) => Dispatcher.UIThread.Post(() => this.OnMessage(message));
            this._server.ConnectionChanged += (s, e) => Dispatcher.UIThread.Post(this.OnConnectionChanged);

            try
            {
                this._server.Start();
                this.Log($"Listening on 127.0.0.1:{DeckMessage.Port} for the Console Deck plugin");
            }
            catch (SocketException ex)
            {
                this.SetStatus($"Port {DeckMessage.Port} is busy. Is the app already running?", "#E53935", "#3A1A1A");
                this.Log($"Could not listen on port {DeckMessage.Port}: {ex.Message}");
            }

            this.Closed += (s, e) => this._server.Dispose();
        }

        // ---------------------------------------------------------------- Inputs

        private void OnMessage(DeckMessage message)
        {
            var id = message.Id ?? "";

            switch (message.Type)
            {
                case "press" when TryIndex(id, "key", KeyCount, out var key):
                    this.PressKey(key, "console");
                    break;
                case "press" when TryIndex(id, "button", ButtonCount, out var button):
                    this.PressButton(button, "console");
                    break;
                case "press" when id == "dial":
                    this.Log("Dial pressed: reset to 50");
                    this.SetDial(50);
                    break;
                case "press" when id == "roller":
                    this.Log("Roller pressed: reset to 50");
                    this.SetRoller(50);
                    break;
                case "release":
                    this.Log($"{Describe(id)} released");
                    break;
                case "turn" when id == "dial":
                    this.TurnDial(message.Delta ?? 0, "console");
                    break;
                case "turn" when id == "roller":
                    this.TurnRoller(message.Delta ?? 0, "console");
                    break;
                default:
                    this.Log($"Unknown message: {message.ToJson()}");
                    break;
            }
        }

        private void PressKey(Int32 index, String source)
        {
            this._keyOn[index] = !this._keyOn[index];
            this._keyPresses[index]++;
            this.Log($"Key {index + 1} pressed ({source}): {(this._keyOn[index] ? "ON" : "off")}");
            this.Flash(this._keyTiles[index]);
            this.UpdateKey(index);
        }

        private void PressButton(Int32 index, String source)
        {
            this._buttonPresses[index]++;
            var previous = this._selectedButton;
            this._selectedButton = index;
            this.Log($"Button {index + 1} pressed ({source}): accent {ButtonColors[index]}");
            this.Flash(this._buttonTiles[index]);

            this.UpdateButton(previous);
            this.UpdateButton(index);
            this.UpdateDial();
            this.UpdateRoller();
        }

        private void TurnDial(Int32 delta, String source)
        {
            if (delta != 0)
            {
                this.SetDial(this._dial + delta);
                this.Log($"Dial turned {delta:+0;-0} ({source}): {this._dial}");
            }
        }

        private void TurnRoller(Int32 delta, String source)
        {
            if (delta != 0)
            {
                this.SetRoller(this._roller + delta);
                this.Log($"Roller turned {delta:+0;-0} ({source}): {this._roller}");
            }
        }

        private void SetDial(Int32 value)
        {
            this._dial = Math.Clamp(value, 0, 100);
            this.UpdateDial();
        }

        private void SetRoller(Int32 value)
        {
            this._roller = Math.Clamp(value, 0, 100);
            this.UpdateRoller();
        }

        private void OnConnectionChanged()
        {
            if (this._server.IsConnected)
            {
                this.SetStatus("Console connected", "#43A047", "#1A3320");
                this.Log("Console Deck plugin connected");
                this.UpdateAll();
            }
            else
            {
                this.SetStatus("Waiting for the plugin…", "#F0A030", "#3A2A1A");
                this.Log("Console Deck plugin disconnected");
            }
        }

        // ---------------------------------------------------------------- Feedback (window and hardware)

        private void UpdateAll()
        {
            for (var i = 0; i < KeyCount; i++)
            {
                this.UpdateKey(i);
            }

            for (var i = 0; i < ButtonCount; i++)
            {
                this.UpdateButton(i);
            }

            this.UpdateDial();
            this.UpdateRoller();
        }

        private void UpdateKey(Int32 index)
        {
            var on = this._keyOn[index];
            var color = on ? KeyColors[index] : OffColor;

            this._keyTiles[index].Background = Brush.Parse(color);
            this._keyStateTexts[index].Text = on ? "ON" : "off";
            this._keyCountTexts[index].Text = $"{this._keyPresses[index]} presses";

            this._server.Send(DeckMessage.State($"key{index + 1}", $"{index + 1}{Environment.NewLine}{(on ? "ON" : "off")}", color));
        }

        private void UpdateButton(Int32 index)
        {
            var selected = index == this._selectedButton;
            var color = selected ? ButtonColors[index] : OffColor;

            this._buttonTiles[index].Background = Brush.Parse(color);
            this._buttonCountTexts[index].Text = $"{this._buttonPresses[index]}";

            this._server.Send(DeckMessage.State($"button{index + 1}", $"B{index + 1}{Environment.NewLine}{this._buttonPresses[index]}", color));
        }

        private void UpdateDial()
        {
            var accent = Brush.Parse(ButtonColors[this._selectedButton]);
            var sweep = this._dial * 2.7;

            this.DialArc.SweepAngle = sweep;
            this.DialArc.Stroke = accent;
            this.DialNeedle.RenderTransform = new RotateTransform(sweep - 135);
            this.DialValue.Text = $"{this._dial}";

            this._server.Send(DeckMessage.State("dial", $"{this._dial}"));
        }

        private void UpdateRoller()
        {
            const Double TrackHeight = 230;
            const Double ThumbHeight = 22;

            var top = (1 - this._roller / 100.0) * (TrackHeight - ThumbHeight);
            Canvas.SetTop(this.RollerThumb, top);
            Canvas.SetTop(this.RollerFill, top + ThumbHeight / 2);
            this.RollerFill.Height = TrackHeight - top - ThumbHeight / 2;
            this.RollerFill.Background = Brush.Parse(ButtonColors[this._selectedButton]);
            this.RollerValue.Text = $"Roller {this._roller}";

            this._server.Send(DeckMessage.State("roller", $"{this._roller}"));
        }

        // Briefly outlines a tile in white.
        private void Flash(Border tile)
        {
            tile.BorderBrush = Brushes.White;
            DispatcherTimer.RunOnce(() => tile.BorderBrush = Brushes.Transparent, FlashTime);
        }

        private void SetStatus(String text, String dotColor, String background)
        {
            this.StatusText.Text = text;
            this.StatusDot.Fill = Brush.Parse(dotColor);
            this.StatusPill.Background = Brush.Parse(background);
        }

        private void Log(String text)
        {
            var items = this.EventLog.Items;
            items.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {text}");
            while (items.Count > MaxLogEntries)
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        // ---------------------------------------------------------------- Building the tiles

        private void CreateKeyTiles()
        {
            for (var i = 0; i < KeyCount; i++)
            {
                var index = i;

                var number = new TextBlock { Text = $"{i + 1}", FontSize = 13, Foreground = Brushes.White, Opacity = 0.8 };
                var state = new TextBlock
                {
                    FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                };
                var count = new TextBlock
                {
                    FontSize = 11, Foreground = Brushes.White, Opacity = 0.7,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
                };

                var tile = new Border
                {
                    Margin = new Thickness(6),
                    Padding = new Thickness(10, 8),
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(3),
                    BorderBrush = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = new Panel { Children = { number, state, count } },
                };
                tile.PointerPressed += (s, e) => this.PressKey(index, "app");

                this._keyTiles[i] = tile;
                this._keyStateTexts[i] = state;
                this._keyCountTexts[i] = count;
                this.KeyGrid.Children.Add(tile);
            }
        }

        private void CreateButtonTiles()
        {
            for (var i = 0; i < ButtonCount; i++)
            {
                var index = i;

                var label = new TextBlock
                {
                    Text = $"Button {i + 1}", FontWeight = FontWeight.SemiBold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                var count = new TextBlock { FontSize = 20, FontWeight = FontWeight.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };

                var tile = new Border
                {
                    Margin = new Thickness(6, 0),
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(3),
                    BorderBrush = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2, Children = { label, count } },
                };
                tile.PointerPressed += (s, e) => this.PressButton(index, "app");

                this._buttonTiles[i] = tile;
                this._buttonCountTexts[i] = count;
                this.ButtonGrid.Children.Add(tile);
            }
        }

        // ---------------------------------------------------------------- Helpers

        // "key3" with prefix "key" -> index 2.
        private static Boolean TryIndex(String id, String prefix, Int32 count, out Int32 index)
        {
            index = -1;
            return id.StartsWith(prefix) && Int32.TryParse(id.Substring(prefix.Length), out var number)
                && number >= 1 && number <= count && (index = number - 1) >= 0;
        }

        private static String Describe(String id) =>
            id.StartsWith("key") ? $"Key {id.Substring(3)}" : id.StartsWith("button") ? $"Button {id.Substring(6)}" : id;
    }
}
