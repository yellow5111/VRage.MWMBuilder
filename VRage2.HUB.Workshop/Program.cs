using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
// using Avalonia.ReactiveUI;
using Steamworks;
using System;
using VRage2.HUB.Workshop;

namespace VRage2.HUB.Workshop.Program
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // SteamAPI.Init();
            if (!SteamAPI.Init())
            {
                Console.WriteLine("SteamAPI initialization failed.");
                return;
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            SteamAPI.Shutdown();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        // .UseReactiveUI();
    }
}
