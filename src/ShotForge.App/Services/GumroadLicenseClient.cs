using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShotForge.App.Services
{
    public enum LicenseCheckStatus
    {
        Valid,
        InvalidKey,
        Refunded,
        TooManyActivations,
        NetworkError,
        NotConfigured
    }

    public record LicenseCheckResult(LicenseCheckStatus Status, string? Email = null, string? Message = null);

    /// <summary>
    /// Talks to Gumroad's licence verification endpoint:
    /// POST https://api.gumroad.com/v2/licenses/verify with product_id and
    /// license_key. increment_uses_count=true counts one activation; false is
    /// used for routine re-checks so they don't use up the activation limit.
    /// </summary>
    public class GumroadLicenseClient
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private const string VerifyUrl = "https://api.gumroad.com/v2/licenses/verify";

        public async Task<LicenseCheckResult> VerifyAsync(string licenseKey, bool incrementUses)
        {
            if (!LicenseConfig.IsConfigured)
                return new LicenseCheckResult(LicenseCheckStatus.NotConfigured);

            try
            {
                var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("product_id", LicenseConfig.GumroadProductId),
                    new KeyValuePair<string, string>("license_key", licenseKey.Trim()),
                    new KeyValuePair<string, string>("increment_uses_count", incrementUses ? "true" : "false")
                });

                using var response = await Http.PostAsync(VerifyUrl, form);
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var success = GetBool(root, "success");
                var message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                    ? m.GetString() : null;

                if (!success)
                {
                    // Gumroad answers 404 for a key that doesn't exist or has been disabled.
                    // Anything else (e.g. a 5xx) is a service problem, not the buyer's fault.
                    return (int)response.StatusCode == 404
                        ? new LicenseCheckResult(LicenseCheckStatus.InvalidKey, Message: message)
                        : new LicenseCheckResult(LicenseCheckStatus.NetworkError, Message: message);
                }

                string? email = null;
                var refunded = false;
                if (root.TryGetProperty("purchase", out var purchase) && purchase.ValueKind == JsonValueKind.Object)
                {
                    refunded = GetBool(purchase, "refunded") || GetBool(purchase, "chargebacked");
                    if (purchase.TryGetProperty("email", out var e) && e.ValueKind == JsonValueKind.String)
                        email = e.GetString();
                }

                if (refunded)
                    return new LicenseCheckResult(LicenseCheckStatus.Refunded, email);

                if (incrementUses
                    && root.TryGetProperty("uses", out var uses)
                    && uses.ValueKind == JsonValueKind.Number
                    && uses.GetInt32() > LicenseConfig.MaxActivations)
                {
                    return new LicenseCheckResult(LicenseCheckStatus.TooManyActivations, email);
                }

                return new LicenseCheckResult(LicenseCheckStatus.Valid, email);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                return new LicenseCheckResult(LicenseCheckStatus.NetworkError, Message: ex.Message);
            }
        }

        private static bool GetBool(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }
}
