namespace Loupedeck.FocusTimerPlugin
{
    using System;

    // A simple adjustment: turning the dial changes the session length one minute per tick,
    // and pressing the dial (the reset command) stops the timer and restores the default 25 minutes.
    // The current duration is shown next to the dial.

    public class DurationAdjustment : PluginDynamicAdjustment
    {
        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public DurationAdjustment()
            : base(displayName: "Session Length", description: "Sets the focus session length in minutes", groupName: "Focus Timer", hasReset: true)
        {
        }

        private FocusTimerPlugin FocusPlugin => (FocusTimerPlugin)this.Plugin;

        // This method is called when the plugin loads the adjustment.
        protected override Boolean OnLoad()
        {
            this.FocusPlugin.Timer.StateChanged += this.OnTimerStateChanged;
            return true;
        }

        // This method is called when the plugin unloads the adjustment.
        protected override Boolean OnUnload()
        {
            this.FocusPlugin.Timer.StateChanged -= this.OnTimerStateChanged;
            return true;
        }

        // This method is called when the dial is rotated. `diff` is the number of ticks, negative when turning left.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff) =>
            this.FocusPlugin.SetDuration(this.FocusPlugin.Timer.DurationMinutes + diff);

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter)
        {
            this.FocusPlugin.Timer.Reset();
            this.FocusPlugin.SetDuration(FocusTimer.DefaultMinutes);
            PluginLog.Info("Focus timer reset");
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override String GetAdjustmentValue(String actionParameter) => $"{this.FocusPlugin.Timer.DurationMinutes} min";

        // Notifies the plugin service that the adjustment value has changed. The value only changes with the duration,
        // but the duration can also be changed by a preset button, so the adjustment listens to the timer.
        private void OnTimerStateChanged(Object sender, EventArgs e) => this.AdjustmentValueChanged();
    }
}
