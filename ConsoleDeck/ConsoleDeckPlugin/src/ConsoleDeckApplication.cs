namespace Loupedeck.ConsoleDeckPlugin
{
    using System;

    // This class can be used to connect the plugin to an application.
    // Console Deck is a universal plugin (`HasNoApplication` is true), so it is not linked to any application.

    public class ConsoleDeckApplication : ClientApplication
    {
        public ConsoleDeckApplication()
        {
        }

        // This method can be used to link the plugin to a Windows application.
        protected override String GetProcessName() => "";

        // This method can be used to link the plugin to a macOS application.
        protected override String GetBundleName() => "";

        // This method can be used to check whether the application is installed or not.
        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
