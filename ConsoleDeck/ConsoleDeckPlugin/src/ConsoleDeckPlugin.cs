namespace Loupedeck.ConsoleDeckPlugin
{
    using System;

    // The plugin-level logic of the Console Deck plugin.
    // It owns the connection to the ConsoleDeck app. The actions forward the hardware inputs through it
    // and draw the feedback that the app sends back.

    public class ConsoleDeckPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        // Console Deck works regardless of which application is in the foreground.
        public override Boolean HasNoApplication => true;

        // The connection to the ConsoleDeck app, shared by all the actions of this plugin.
        public DeckLink Link { get; } = new DeckLink();

        // Initializes a new instance of the plugin class.
        public ConsoleDeckPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            this.Link.Start();
            PluginLog.Info("Console Deck loaded, waiting for the ConsoleDeck app");
        }

        // This method is called when the plugin is unloaded.
        public override void Unload() => this.Link.Dispose();
    }
}
