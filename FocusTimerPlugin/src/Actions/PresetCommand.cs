namespace Loupedeck.FocusTimerPlugin
{
    using System;

    // A command with parameters: one action per preset duration.
    // Each preset appears as a separate action in the "Focus Timer###Presets" group.
    // Pressing one sets the duration and starts a fresh countdown.

    public class PresetCommand : PluginDynamicCommand
    {
        private static readonly Int32[] PresetMinutes = { 5, 15, 25, 50 };

        // Initializes the command class and registers one parameter per preset.
        // The parameter is the duration in minutes, which keeps each one unique.
        public PresetCommand()
            : base()
        {
            foreach (var minutes in PresetMinutes)
            {
                this.AddParameter(minutes.ToString(), $"{minutes} min session", "Focus Timer###Presets");
            }
        }

        private FocusTimerPlugin FocusPlugin => (FocusTimerPlugin)this.Plugin;

        // This method is called when the user presses one of the preset buttons.
        protected override void RunCommand(String actionParameter)
        {
            if (Int32.TryParse(actionParameter, out var minutes))
            {
                this.FocusPlugin.Timer.Reset();
                this.FocusPlugin.SetDuration(minutes);
                this.FocusPlugin.Timer.Start();
                PluginLog.Info($"Started a {minutes} min focus session");
            }
        }

        // The button label for each preset.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            Int32.TryParse(actionParameter, out var minutes) ? $"{minutes} min" : null;
    }
}
