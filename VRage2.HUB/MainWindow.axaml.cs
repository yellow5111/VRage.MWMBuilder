using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VRage2.HUB
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            BrowseBuilder.Click += async (_, __) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select mwmbuilder.exe",
                    Filters = { new FileDialogFilter { Name = "EXE", Extensions = { "exe" } } }
                };
                var res = await dlg.ShowAsync(this);
                if (res?.Length > 0) BuilderPathBox.Text = res[0];
            };

            BrowseSource.Click += async (_, __) =>
            {
                var dlg = new OpenFolderDialog { Title = "Select Source Folder" };
                var res = await dlg.ShowAsync(this);
                if (!string.IsNullOrEmpty(res)) SourceFolderBox.Text = res;
            };

            BrowseOutput.Click += async (_, __) =>
            {
                var dlg = new OpenFolderDialog { Title = "Select Output Folder" };
                var res = await dlg.ShowAsync(this);
                if (!string.IsNullOrEmpty(res)) OutputFolderBox.Text = res;
            };

            BrowseMaterials.Click += async (_, __) =>
            {
                var dlg = new OpenFolderDialog { Title = "Select Materials Folder" };
                var res = await dlg.ShowAsync(this);
                if (!string.IsNullOrEmpty(res)) MaterialsBox.Text = res;
            };

            RunButton.Click += RunButton_Click;
            CloseButton.Click += CloseButton_Click;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RunButton_Click(object? sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            StatusText.Text = "Running MWMBuilder…";

            string builder = BuilderPathBox.Text;
            string src = SourceFolderBox.Text;
            string subtype = SubtypeBox.Text;
            string outDir = OutputFolderBox.Text;
            string mats = MaterialsBox.Text;
            string logFile = Path.Combine(src, $"{subtype}.mwm.log");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = builder,
                    Arguments = $"/f /s:\"{src}\" /m:{subtype}*.fbx /o:\"{outDir}\" /x:\"{mats}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = src
                };

                var proc = Process.Start(psi)!;
                string outp = await proc.StandardOutput.ReadToEndAsync();
                string errp = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                // write to log
                File.WriteAllText(logFile, outp + Environment.NewLine + errp);

                // optional cleanup (no longer deletes the .mwm.log file)
                if (CleanCheck.IsChecked == true)
                {
                    var files = Directory
                        .GetFiles(src)
                        .Where(f =>
                        {
                            var n = Path.GetFileName(f)!;
                            bool matchName =
                                n.Contains($"{subtype}_BS") ||
                                n.Contains($"{subtype}_LOD") ||
                                n.StartsWith($"{subtype}.");
                            bool matchExt = new[] { ".fbx", ".xml", ".hkt", ".log" }
                                .Any(ext => n.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                            bool isPrimaryLog = n.Equals($"{subtype}.mwm.log", StringComparison.OrdinalIgnoreCase);
                            return matchName && matchExt && !isPrimaryLog;
                        });

                    foreach (var f in files)
                        File.Delete(f);
                }

                StatusText.Text = $"Done. Exit code {proc.ExitCode}. Log: {logFile}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
            }
            finally
            {
                RunButton.IsEnabled = true;
            }
        }
    }
}
