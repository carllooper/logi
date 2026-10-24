namespace ConsoleDeck.App
{
    using System;

    using Avalonia;

    internal static class Program
    {
        [STAThread]
        public static void Main(String[] args) =>
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        // Also used by the visual designer.
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
