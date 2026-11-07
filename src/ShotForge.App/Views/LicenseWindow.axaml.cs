using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ShotForge.App.Services;
using ShotForge.App.ViewModels;

namespace ShotForge.App.Views
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void OnBuyClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LicenseConfig.PurchaseUrl))
            {
                if (DataContext is LicenseWindowViewModel vm)
                    vm.StatusMessage = "The purchase page isn't live yet.";
                return;
            }

            // Opens the product page in the user's default browser.
            Process.Start(new ProcessStartInfo(LicenseConfig.PurchaseUrl) { UseShellExecute = true });
        }
    }
}
