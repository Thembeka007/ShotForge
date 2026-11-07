using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotForge.App.Services;

namespace ShotForge.App.ViewModels
{
    public partial class LicenseWindowViewModel : ObservableObject
    {
        private readonly LicenseService _licenseService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ActivateCommand))]
        private string _licenseKey = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ActivateCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>Raised once Pro has been unlocked, so the View can close the window.</summary>
        public event Action? ActivatedSuccessfully;

        public LicenseWindowViewModel(LicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        private bool CanActivate() => !IsBusy && !string.IsNullOrWhiteSpace(LicenseKey);

        [RelayCommand(CanExecute = nameof(CanActivate))]
        private async Task ActivateAsync()
        {
            IsBusy = true;
            StatusMessage = "Checking your key...";

            var result = await _licenseService.ActivateAsync(LicenseKey);

            IsBusy = false;
            StatusMessage = result.Status switch
            {
                LicenseCheckStatus.Valid => "Pro unlocked. Thank you for supporting ShotForge!",
                LicenseCheckStatus.InvalidKey => "That key wasn't recognised. Copy it exactly as it appears in your Gumroad receipt email.",
                LicenseCheckStatus.Refunded => "This purchase was refunded, so the key no longer works.",
                LicenseCheckStatus.TooManyActivations => $"This key has already been activated on the maximum number of computers ({LicenseConfig.MaxActivations}). Please contact support.",
                LicenseCheckStatus.NotConfigured => "Licensing isn't set up in this build yet.",
                _ => "Couldn't reach the licence server. Check your internet connection and try again."
            };

            if (result.Status == LicenseCheckStatus.Valid)
            {
                ActivatedSuccessfully?.Invoke();
            }
        }
    }
}
