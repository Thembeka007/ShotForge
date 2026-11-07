namespace ShotForge.App.Services
{
    /// <summary>
    /// The only things to edit when your Gumroad product goes live. Everything
    /// else in the licensing code reads from here.
    /// </summary>
    public static class LicenseConfig
    {
        /// <summary>
        /// Your Gumroad PRODUCT ID (not the product URL). In Gumroad: open the
        /// product > Content/Settings > turn on "Generate a unique license key
        /// per sale" - the product ID is shown on that page. While this is empty
        /// the Unlock window explains that licensing isn't set up yet.
        /// </summary>
        public const string GumroadProductId = "Y4kE3RpReHDMOXRVJUrEgA==";

        /// <summary>The public Gumroad page where people buy Pro (opened by the "Buy Pro" button).</summary>
        public const string PurchaseUrl = "https://tm007.gumroad.com/l/wsvlys";

        /// <summary>Projects allowed on the Free version.</summary>
        public const int FreeProjectLimit = 1;

        /// <summary>
        /// How many times one key may be activated (each activation counts once
        /// on Gumroad's side). Allows a few reinstalls / a second computer
        /// without letting one key be shared widely.
        /// </summary>
        public const int MaxActivations = 5;

        /// <summary>How often (days) a Pro install quietly re-checks its key when online.</summary>
        public const int RecheckAfterDays = 7;

        public static bool IsConfigured => !string.IsNullOrWhiteSpace(GumroadProductId);
    }
}
