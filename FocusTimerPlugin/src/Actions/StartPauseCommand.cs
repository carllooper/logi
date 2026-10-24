namespace Loupedeck.FocusTimerPlugin
{
    using System;

    // A simple command: pressing the button starts or pauses the countdown.
    // The button shows the remaining time on a background that is green while running,
    // grey while paused, and red when the session has finished.

    public class StartPauseCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor RunningColor = new BitmapColor(46, 125, 50);
        private static readonly BitmapColor PausedColor = new BitmapColor(66, 66, 66);
        private static readonly BitmapColor FinishedColor = new BitmapColor(198, 40, 40);

        // Initializes the command class.
        public StartPauseCommand()
            : base(displayName: "Start / Pause", description: "Starts or pauses the focus timer", groupName: "Focus Timer")
        {
        }

        private FocusTimer Timer => ((FocusTimerPlugin)this.Plugin).Timer;

        // This method is called when the plugin loads the command.
        protected override Boolean OnLoad()
        {
            this.Timer.StateChanged += this.OnTimerStateChanged;
            return true;
        }

        // This method is called when the plugin unloads the command.
        protected override Boolean OnUnload()
        {
            this.Timer.StateChanged -= this.OnTimerStateChanged;
            return true;
        }

        // This method is called when the user presses the button.
        protected override void RunCommand(String actionParameter)
        {
            this.Timer.Toggle();
            PluginLog.Info(this.Timer.IsRunning ? "Focus timer started" : "Focus timer paused");
        }

        // This method is called when the plugin service needs to show the command in the UI.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"Focus{Environment.NewLine}{FormatTime(this.Timer.Remaining)}";

        // This method is called when the plugin service needs to draw the button on the device.
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var timer = this.Timer;
            var remaining = timer.Remaining;

            var background = timer.IsRunning
                ? RunningColor
                : remaining <= TimeSpan.Zero ? FinishedColor : PausedColor;

            var label = timer.IsRunning
                ? FormatTime(remaining)
                : remaining <= TimeSpan.Zero ? $"Done!{Environment.NewLine}Press" : $"{FormatTime(remaining)}{Environment.NewLine}Paused";

            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.Clear(background);
                bitmapBuilder.DrawText(label);
                return bitmapBuilder.ToImage();
            }
        }

        // Rounds up, so "00:00" only shows once the countdown has really finished.
        private static String FormatTime(TimeSpan time)
        {
            var totalSeconds = Math.Max(0, (Int32)Math.Ceiling(time.TotalSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        // Tells the plugin service to redraw the button.
        private void OnTimerStateChanged(Object sender, EventArgs e) => this.ActionImageChanged();
    }
}
