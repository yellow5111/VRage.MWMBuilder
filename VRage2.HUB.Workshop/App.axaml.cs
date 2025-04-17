using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Steamworks;

namespace VRage2.HUB.Workshop
{
    public class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var splash = new SplashWindow();
                desktop.MainWindow = splash;
                splash.Show();

                await Task.Delay(10000);

                // splash.Close();
                var main = new MainWindow();
                desktop.MainWindow = main;
                main.Show();
                {
                    splash.Close();
                }
                
                // SteamAPI.Init();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
