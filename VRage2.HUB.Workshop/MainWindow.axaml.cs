using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Steamworks;

namespace VRage2.HUB.Workshop
{
    public partial class MainWindow : Window
    {
        private readonly string modsRoot;
        private CallResult<CreateItemResult_t> createItemResult;
        private CallResult<SubmitItemUpdateResult_t> submitItemUpdateResult;
        private UGCUpdateHandle_t currentUpdateHandle;
        private DispatcherTimer progressTimer;
        private bool isNew;
        private PublishedFileId_t existingId;
        private string currentThumbnailPath = "";

        public ObservableCollection<TagItem> TagItems { get; }
            = new ObservableCollection<TagItem>();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            var categories = new[]
            {
                "Block", "Respawn Ship", "Script", "Modpack", "Skybox",
                "Character", "Animation", "Asteroid", "Planet",
                "Production", "NPC", "Other", "Font"
            };
            var tags = new[] { "hud", "ServerScripts", "NoScripts" };
            foreach (var t in categories.Concat(tags))
                TagItems.Add(new TagItem(t));

            ModFolderListBox.SelectionChanged += ModFolderListBox_SelectionChanged;
            BrowseThumbnailButton.Click += BrowseThumbnailButton_Click;
            UploadButton.Click += UploadButton_Click;

            UploadButton.IsEnabled = false;

            var steamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            steamTimer.Tick += (_, __) => SteamAPI.RunCallbacks();
            steamTimer.Start();

            modsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "Mods");

            createItemResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
            submitItemUpdateResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);

            LoadModFolders();
            VisibilityComboBox.SelectedIndex = 0;

            if (ModFolderListBox.SelectedItem is string folder)
            {
                var fullPath = Path.Combine(modsRoot, folder);
                currentThumbnailPath = Path.Combine(fullPath, "thumb.jpg");
                RefreshThumbnail();
            }
        }

        private void LoadModFolders()
        {
            if (!Directory.Exists(modsRoot)) return;
            var dirs = Directory
                .GetDirectories(modsRoot)
                .Select(Path.GetFileName)
                .ToList();

            ModFolderListBox.ItemsSource = dirs;
            if (dirs.Any())
                ModFolderListBox.SelectedIndex = 0;
        }

        private void ModFolderListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ModFolderListBox.SelectedItem is not string folder) return;
            var fullPath = Path.Combine(modsRoot, folder);

            currentThumbnailPath = Path.Combine(fullPath, "thumb.jpg");
            RefreshThumbnail();
        }

        private void RefreshThumbnail()
        {
            if (File.Exists(currentThumbnailPath))
            {
                ThumbnailPreview.Source = new Bitmap(currentThumbnailPath);
                ThumbnailPreview.IsVisible = true;
                MissingThumbTextBlock.IsVisible = false;
                UploadButton.IsEnabled = true;
            }
            else
            {
                ThumbnailPreview.IsVisible = false;
                MissingThumbTextBlock.IsVisible = true; 
                UploadButton.IsEnabled = false;
            }
        }

        private async void BrowseThumbnailButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = { new FileDialogFilter { Name = "JPEG", Extensions = { "jpg", "jpeg" } } }
            };
            var res = await dlg.ShowAsync(this);
            if (res?.Length > 0)
            {
                currentThumbnailPath = res[0];
                RefreshThumbnail();
            }
        }

        private void UploadButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ModFolderListBox.SelectedItem is not string folder) return;
            var fullPath = Path.Combine(modsRoot, folder);

            if (!Directory.Exists(fullPath))
            {
                StatusTextBlock.Text = $"❌ Content folder not found:\n{fullPath}";
                return;
            }
            if (string.IsNullOrEmpty(currentThumbnailPath) || !File.Exists(currentThumbnailPath))
            {
                StatusTextBlock.Text = $"❌ Thumbnail not found:\n{currentThumbnailPath}";
                return;
            }
            var fileInfo = new FileInfo(currentThumbnailPath);
            if (fileInfo.Length < 16)
            {
                StatusTextBlock.Text = $"❌ Thumbnail too small (must be ≥16 bytes):\n{currentThumbnailPath}";
                return;
            }

            // Decide create vs update
            var vrg2 = Directory.GetFiles(fullPath, "*.VRG2").FirstOrDefault();
            if (vrg2 != null && ulong.TryParse(Path.GetFileNameWithoutExtension(vrg2), out ulong id))
            {
                isNew = false;
                existingId = new PublishedFileId_t(id);
                StatusTextBlock.Text = "Updating existing item...";
                StartUpdate(existingId, fullPath);
            }
            else
            {
                isNew = true;
                StatusTextBlock.Text = "Creating workshop item...";
                var handle = SteamUGC.CreateItem(
                    new AppId_t(244850),
                    EWorkshopFileType.k_EWorkshopFileTypeCommunity
                );
                createItemResult.Set(handle);
            }

            UploadProgressBar.IsVisible = true;
            UploadProgressBar.Value = 0;
        }


        private void StartUpdate(PublishedFileId_t fileId, string contentPath)
        {
                /*
                StatusTextBlock.Text =
                $"ID: {fileId.m_PublishedFileId}\n" +
                $"Content Folder:\n{contentPath}\n" +
                $"Thumbnail File:\n{currentThumbnailPath}";
                */

            if (!Directory.Exists(contentPath))
            {
                StatusTextBlock.Text = $"❌ Content folder not found:\n{contentPath}";
                UploadProgressBar.IsVisible = false;
                return;
            }
            if (string.IsNullOrEmpty(currentThumbnailPath) || !File.Exists(currentThumbnailPath))
            {
                StatusTextBlock.Text = $"❌ Thumbnail not found:\n{currentThumbnailPath}";
                UploadProgressBar.IsVisible = false;
                return;
            }

            currentUpdateHandle = SteamUGC.StartItemUpdate(
                new AppId_t(244850),
                fileId);

            SteamUGC.SetItemTitle(currentUpdateHandle, TitleTextBox.Text ?? "");
            SteamUGC.SetItemDescription(currentUpdateHandle, DescriptionTextBox.Text ?? "");
            SteamUGC.SetItemVisibility(currentUpdateHandle,
                (ERemoteStoragePublishedFileVisibility)VisibilityComboBox.SelectedIndex);

            SteamUGC.SetItemContent(currentUpdateHandle, contentPath);
            SteamUGC.SetItemPreview(currentUpdateHandle, currentThumbnailPath);

            var chosen = TagItems
                .Where(t => t.IsSelected)
                .Select(t => t.Name)
                .ToArray();
            if (chosen.Length > 0)
                SteamUGC.SetItemTags(currentUpdateHandle, chosen);

            var call = SteamUGC.SubmitItemUpdate(
                currentUpdateHandle,
                ChangeNoteTextBox.Text ?? "");
            submitItemUpdateResult.Set(call);

            progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            progressTimer.Tick += UpdateProgress;
            progressTimer.Start();
        }

        private void OnCreateItemResult(CreateItemResult_t cb, bool ioFail)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ioFail || cb.m_eResult != EResult.k_EResultOK)
                {
                    StatusTextBlock.Text = $"❌ Create failed: {cb.m_eResult}";
                    UploadProgressBar.IsVisible = false;
                    return;
                }

                var newId = cb.m_nPublishedFileId.m_PublishedFileId;
                var folder = ModFolderListBox.SelectedItem as string;
                var path = Path.Combine(modsRoot, folder!, $"{newId}.VRG2");
                File.WriteAllText(path, string.Empty);

                StatusTextBlock.Text = "Uploading content and metadata...";
                StartUpdate(new PublishedFileId_t(newId), Path.Combine(modsRoot, folder!));
            });
        }

        private void UpdateProgress(object? s, EventArgs e)
        {
            if (currentUpdateHandle == UGCUpdateHandle_t.Invalid) return;
            SteamUGC.GetItemUpdateProgress(
                currentUpdateHandle,
                out var done,
                out var total);
            if (total > 0)
                UploadProgressBar.Value = (double)done / total * 100;
        }

        private void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t cb, bool ioFail)
        {
            Dispatcher.UIThread.Post(() =>
            {
                progressTimer?.Stop();
                UploadProgressBar.IsVisible = false;
                StatusTextBlock.Text = ioFail || cb.m_eResult != EResult.k_EResultOK
                    ? $"❌ Failed to update: {cb.m_eResult}"
                    : $"✅ Success! ID: {cb.m_nPublishedFileId.m_PublishedFileId}";
            });
        }
    }
}
