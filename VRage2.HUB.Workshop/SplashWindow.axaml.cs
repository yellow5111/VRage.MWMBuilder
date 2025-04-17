using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace VRage2.HUB.Workshop
{
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _spinnerTimer;
        private readonly RotateTransform _rotateTransform = new RotateTransform(0);

        public SplashWindow()
        {
            InitializeComponent();


            Spinner.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            Spinner.RenderTransform = _rotateTransform;


            _spinnerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0)
            };
            _spinnerTimer.Tick += OnSpinnerTick;


            Opened += OnOpened;
        }

        private void OnSpinnerTick(object? sender, EventArgs e)
        {
            _rotateTransform.Angle =
                (_rotateTransform.Angle +
                 360 * _spinnerTimer.Interval.TotalSeconds) % 360;
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            _spinnerTimer.Start();
            await Task.Delay(TimeSpan.FromSeconds(3));

            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var main = new MainWindow();
                desktop.MainWindow = main;
                await Task.CompletedTask;
                // desktop.MainWindow.Show();
            }

            _spinnerTimer.Stop();
            // this.IsVisible = false;  
            // Close();   
        }

    }
}
