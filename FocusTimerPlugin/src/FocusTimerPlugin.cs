namespace Loupedeck.FocusTimerPlugin
{
    using System;

    // This class contains the plugin-level logic of the Focus Timer plugin.
    // It owns the shared `FocusTimer` instance and persists the chosen duration in the plugin settings.

    public class FocusTimerPlugin : Plugin
    {
        private const String DurationSettingName = "DurationMinutes";

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        // The Focus Timer works regardless of which application is in the foreground.
        public override Boolean HasNoApplication => true;

        // The timer shared by all the actions of this plugin.
        public FocusTimer Timer { get; } = new FocusTimer();

        // Initializes a new instance of the plugin class.
        public FocusTimerPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            // Restore the duration that the user chose last time.
            if (this.TryGetPluginSetting(DurationSettingName, out var value) && Int32.TryParse(value, out var minutes))
            {
                this.Timer.SetDuration(minutes);
            }

            this.Timer.Finished += this.OnTimerFinished;
            PluginLog.Info($"Focus Timer loaded with a duration of {this.Timer.DurationMinutes} min");
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            this.Timer.Finished -= this.OnTimerFinished;
            this.Timer.Dispose();
        }

        // Changes the timer duration and remembers it across restarts.
        public void SetDuration(Int32 minutes)
        {
            this.Timer.SetDuration(minutes);
            this.SetPluginSetting(DurationSettingName, this.Timer.DurationMinutes.ToString(), false);
        }

        private void OnTimerFinished(Object sender, EventArgs e) => PluginLog.Info("Focus session finished");
    }
}
