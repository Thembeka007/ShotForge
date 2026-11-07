using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShotForge.App.Services
{
    public enum LicenseTier
    {
        Free,
        Pro
    }

    /// <summary>
    /// Free = LicenseConfig.FreeProjectLimit project(s); Pro = unlimited.
    /// Pro is unlocked by a Gumroad licence key, verified online once at
    /// activation and quietly re-checked every few days when online. After
    /// activation the app works fully offline.
    ///
    /// Honest limit of any local licence check: it stops casual sharing, not a
    /// determined cracker. That is the right trade-off for a one-time-price tool.
    /// </summary>
    public class LicenseService
    {
        private readonly string _licenseFilePath;
        private readonly GumroadLicenseClient _client = new();
        private LicenseState _state;

        /// <summary>Raised (on the UI thread) when the tier changes.</summary>
        public event Action? StateChanged;

        public LicenseService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShotForge");
            Directory.CreateDirectory(folder);
            _licenseFilePath = Path.Combine(folder, "license.json");
            _state = LoadState();
        }

        /// <summary>
        /// Developer convenience for testing Pro without a key: in a DEBUG build,
        /// start the app with the --force-pro argument (or set the environment variable
        /// SHOTFORGE_FORCE_PRO to 1). The
        /// whole check is compiled out of Release builds, so it can never ship.
        /// Trims the value, so a stray trailing space can't break it.
        /// </summary>
        public bool IsDeveloperOverrideActive
        {
            get
            {
#if DEBUG
                if (DevOptions.ForceProRequested) return true;
                var value = Environment.GetEnvironmentVariable("SHOTFORGE_FORCE_PRO")?.Trim();
                return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
#else
                return false;
#endif
            }
        }

        public LicenseTier Tier =>
            IsDeveloperOverrideActive || IsActivated ? LicenseTier.Pro : LicenseTier.Free;

        // Pro needs a stored key that has been VERIFIED at least once. A key
        // typed into the file by hand (never verified) does not count.
        private bool IsActivated =>
            !string.IsNullOrWhiteSpace(_state.LicenseKey) && _state.LastVerifiedUtc is not null;

        public string? LicensedEmail => _state.Email;

        public int FreeProjectLimit => LicenseConfig.FreeProjectLimit;

        public bool CanAddProject(int currentProjectCount) =>
            Tier == LicenseTier.Pro || currentProjectCount < LicenseConfig.FreeProjectLimit;

        /// <summary>Verifies the key with Gumroad (counts one activation) and unlocks Pro if valid.</summary>
        public async Task<LicenseCheckResult> ActivateAsync(string licenseKey)
        {
            var result = await _client.VerifyAsync(licenseKey, incrementUses: true);

            if (result.Status == LicenseCheckStatus.Valid)
            {
                var now = DateTime.UtcNow;
                _state = new LicenseState
                {
                    LicenseKey = licenseKey.Trim(),
                    Email = result.Email,
                    ActivatedUtc = now,
                    LastVerifiedUtc = now
                };
                SaveState();
                StateChanged?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Called at startup. If this install has a key and it hasn't been
        /// re-checked recently, ask Gumroad again WITHOUT using up an activation.
        /// A key that is now invalid or refunded drops back to Free. If the check
        /// can't run (offline), Pro simply carries on - no one is locked out for
        /// being on a plane.
        /// </summary>
        public async Task RevalidateIfDueAsync()
        {
            if (string.IsNullOrWhiteSpace(_state.LicenseKey)) return;

            var due = _state.LastVerifiedUtc is null
                      || (DateTime.UtcNow - _state.LastVerifiedUtc.Value).TotalDays >= LicenseConfig.RecheckAfterDays;
            if (!due) return;

            var result = await _client.VerifyAsync(_state.LicenseKey, incrementUses: false);

            switch (result.Status)
            {
                case LicenseCheckStatus.Valid:
                    _state.LastVerifiedUtc = DateTime.UtcNow;
                    SaveState();
                    StateChanged?.Invoke();
                    break;

                case LicenseCheckStatus.InvalidKey:
                case LicenseCheckStatus.Refunded:
                    _state = new LicenseState();
                    SaveState();
                    StateChanged?.Invoke();
                    break;

                default:
                    // Offline / service trouble / not configured: change nothing.
                    break;
            }
        }

        private LicenseState LoadState()
        {
            try
            {
                if (!File.Exists(_licenseFilePath)) return new LicenseState();
                return JsonSerializer.Deserialize<LicenseState>(File.ReadAllText(_licenseFilePath)) ?? new LicenseState();
            }
            catch
            {
                return new LicenseState();
            }
        }

        private void SaveState()
        {
            try { File.WriteAllText(_licenseFilePath, JsonSerializer.Serialize(_state)); }
            catch { /* not being able to save should never crash the app */ }
        }

        private class LicenseState
        {
            public string? LicenseKey { get; set; }
            public string? Email { get; set; }
            public DateTime? ActivatedUtc { get; set; }
            public DateTime? LastVerifiedUtc { get; set; }
        }
    }
}
