using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ShotForge.App.ViewModels;
using ShotForge.Data;
using ShotForge.Data.Repositories;

namespace ShotForge.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
                viewModel.ExportRequested += OnExportRequested;
                viewModel.ManageCharactersRequested += OnManageCharactersRequested;
                viewModel.UnlockRequested += OnUnlockRequested;
            }
        }

        private async void OnUnlockRequested(LicenseWindowViewModel licenseViewModel)
        {
            var licenseWindow = new LicenseWindow { DataContext = licenseViewModel };

            // Give the user a moment to read "Pro unlocked", then close.
            licenseViewModel.ActivatedSuccessfully += async () =>
            {
                await Task.Delay(1200);
                licenseWindow.Close();
            };

            await licenseWindow.ShowDialog(this);

            if (DataContext is MainWindowViewModel main)
            {
                main.RefreshLicenseState();
            }
        }

        private async void OnManageCharactersRequested(int projectId, string projectName)
        {
            // A fresh, short-lived DbContext for this window's lifetime -
            // same pattern used for MainWindowViewModel itself. Because
            // it's a separate DbContext, changes made here don't appear
            // in MainWindowViewModel's own AvailableCharacters list until
            // explicitly refreshed below, once this window closes.
            var db = new ShotForgeDbContext();
            var characterRepository = new CharacterRepository(db);

            var charactersWindow = new CharactersWindow
            {
                DataContext = new CharactersWindowViewModel(characterRepository, projectId, projectName)
            };

            await charactersWindow.ShowDialog(this);

            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.RefreshAvailableCharactersAsync();
            }
        }

        private async void OnCopyToClipboardRequested(string text)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var clipboard = topLevel?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(text);
            }
        }

        private async void OnExportRequested(string fileNameBase, string textContent, string jsonContent)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is not { } storageProvider)
                return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Shot List",
                SuggestedFileName = $"{fileNameBase}_ShotList",
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text file (paste-ready for Kling)") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("JSON file (structured, for re-import/scripting)") { Patterns = new[] { "*.json" } }
                }
            });

            // File is null if the user cancels the dialog - that's a
            // normal outcome, not an error, so just do nothing.
            if (file is null) return;

            // The dialog only tells us the chosen file's name/extension,
            // not which FileTypeChoice was selected - checking the
            // actual extension the user ended up with is the reliable
            // way to know which content to write.
            var isJson = file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            var contentToWrite = isJson ? jsonContent : textContent;

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(contentToWrite);
        }
    }
}
