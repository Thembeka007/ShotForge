using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ShotForge.App.ViewModels;

namespace ShotForge.App.Views
{
    public partial class CharactersWindow : Window
    {
        public CharactersWindow()
        {
            InitializeComponent();
        }

        private async void OnChooseImageClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not CharactersWindowViewModel viewModel) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is not { } storageProvider) return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose Reference Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image files")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" }
                    }
                }
            });

            if (files.Count == 0) return;

            var localPath = files[0].TryGetLocalPath();
            if (localPath is not null)
            {
                viewModel.SetPendingImage(localPath);
            }
        }
    }
}
