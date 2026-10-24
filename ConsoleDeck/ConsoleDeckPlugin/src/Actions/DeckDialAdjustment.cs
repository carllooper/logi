namespace Loupedeck.ConsoleDeckPlugin
{
    using System;

    using ConsoleDeck.Protocol;

    // An adjustment with parameters: one action for the big dial and one for the roller of the dialpad.
    // Turning sends a "turn" message with the number of ticks, and pressing (the reset command, on controls that
    // can be pressed) sends a "press" message. The value shown next to the control is the label that the app sends back.

    public class DeckDialAdjustment : PluginDynamicAdjustment
    {
        // Initializes the adjustment class and registers one parameter per control. The parameter is the control id.
        // When `hasReset` is set to true, a reset command is automatically created for each parameter.
        public DeckDialAdjustment()
            : base(hasReset: true)
        {
            this.AddParameter("dial", "Dial", "Console Deck###Dialpad");
            this.AddParameter("roller", "Roller", "Console Deck###Dialpad");
        }

        private DeckLink Link => ((ConsoleDeckPlugin)this.Plugin).Link;

        // This method is called when the plugin loads the adjustment.
        protected override Boolean OnLoad()
        {
            this.Link.StateChanged += this.OnStateChanged;
            this.Link.ConnectionChanged += this.OnConnectionChanged;
            return true;
        }

        // This method is called when the plugin unloads the adjustment.
        protected override Boolean OnUnload()
        {
            this.Link.StateChanged -= this.OnStateChanged;
            this.Link.ConnectionChanged -= this.OnConnectionChanged;
            return true;
        }

        // This method is called when the control is turned. `diff` is the number of ticks, negative when turning left.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff) => this.Link.Send(DeckMessage.Turn(actionParameter, diff));

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter) => this.Link.Send(DeckMessage.Press(actionParameter));

        // Returns the value that is shown next to the control.
        protected override String GetAdjustmentValue(String actionParameter) =>
            !this.Link.IsConnected ? "no app" : this.Link.GetState(actionParameter)?.Label ?? "";

        // Tells the plugin service that the value of the control that the app changed must be redrawn.
        private void OnStateChanged(Object sender, String id)
        {
            if (id == "dial" || id == "roller")
            {
                this.AdjustmentValueChanged(id);
            }
        }

        private void OnConnectionChanged(Object sender, EventArgs e)
        {
            this.AdjustmentValueChanged("dial");
            this.AdjustmentValueChanged("roller");
        }
    }
}
