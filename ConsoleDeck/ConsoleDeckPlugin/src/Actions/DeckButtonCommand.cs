namespace Loupedeck.ConsoleDeckPlugin
{
    using System;

    using ConsoleDeck.Protocol;

    // A command with parameters: one action per key or button.
    // The nine keypad keys are in the "Console Deck###Keypad" group, the four dialpad buttons in "Console Deck###Dialpad".
    // Pressing one sends a "press" message to the app, and each key shows the label and color that the app sends back.

    public class DeckButtonCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor OfflineColor = new BitmapColor(48, 48, 48);
        private static readonly BitmapColor DefaultColor = new BitmapColor(30, 60, 90);

        // Initializes the command class and registers one parameter per control. The parameter is the control id.
        public DeckButtonCommand()
            : base()
        {
            for (var i = 1; i <= 9; i++)
            {
                this.AddParameter($"key{i}", $"Key {i}", "Console Deck###Keypad");
            }

            for (var i = 1; i <= 4; i++)
            {
                this.AddParameter($"button{i}", $"Button {i}", "Console Deck###Dialpad");
            }
        }

        private DeckLink Link => ((ConsoleDeckPlugin)this.Plugin).Link;

        // This method is called when the plugin loads the command.
        protected override Boolean OnLoad()
        {
            this.Link.StateChanged += this.OnStateChanged;
            this.Link.ConnectionChanged += this.OnConnectionChanged;
            return true;
        }

        // This method is called when the plugin unloads the command.
        protected override Boolean OnUnload()
        {
            this.Link.StateChanged -= this.OnStateChanged;
            this.Link.ConnectionChanged -= this.OnConnectionChanged;
            return true;
        }

        // This method is called when the user presses the key or button.
        protected override void RunCommand(String actionParameter) => this.Link.Send(DeckMessage.Press(actionParameter));

        // Forwards releases as well, on devices that report them. Returning false keeps the normal press handling.
        protected override Boolean ProcessButtonEvent2(String actionParameter, DeviceButtonEvent2 buttonEvent)
        {
            if (buttonEvent.EventType == DeviceButtonEventType.Release)
            {
                this.Link.Send(DeckMessage.Release(actionParameter));
            }

            return false;
        }

        // The action name in the Options+ editor.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            this.Link.GetState(actionParameter)?.Label ?? ControlName(actionParameter);

        // This method is called when the plugin service needs to draw the key on the device.
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var state = this.Link.GetState(actionParameter);

            var background = !this.Link.IsConnected ? OfflineColor : ParseColor(state?.Color) ?? DefaultColor;
            var label = !this.Link.IsConnected
                ? $"{ControlName(actionParameter)}{Environment.NewLine}no app"
                : state?.Label ?? ControlName(actionParameter);

            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.Clear(background);
                bitmapBuilder.DrawText(label);
                return bitmapBuilder.ToImage();
            }
        }

        // "key3" -> "Key 3", "button2" -> "Button 2".
        private static String ControlName(String id) =>
            id.StartsWith("key") ? $"Key {id.Substring(3)}" : id.StartsWith("button") ? $"Button {id.Substring(6)}" : id;

        // Parses "#RRGGBB". Returns null for anything else.
        private static BitmapColor? ParseColor(String color)
        {
            if (color?.Length == 7 && color[0] == '#' && UInt32.TryParse(color.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            {
                return new BitmapColor((Int32)(rgb >> 16) & 0xFF, (Int32)(rgb >> 8) & 0xFF, (Int32)rgb & 0xFF);
            }

            return null;
        }

        // Tells the plugin service to redraw the key that the app changed.
        private void OnStateChanged(Object sender, String id)
        {
            if (id.StartsWith("key") || id.StartsWith("button"))
            {
                this.ActionImageChanged(id);
            }
        }

        // Redraws all the keys when the app starts or stops.
        private void OnConnectionChanged(Object sender, EventArgs e) => this.ActionImageChanged();
    }
}
