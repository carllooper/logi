namespace Loupedeck.FocusTimerPlugin
{
    using System;

    // This class can be used to connect the plugin to an application.
    // The Focus Timer is a universal plugin (`HasNoApplication` is true), so it is not linked to any application.

    public class FocusTimerApplication : ClientApplication
    {
        public FocusTimerApplication()
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
