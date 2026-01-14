namespace SrcSinavUygulamasi.Constants
{
    /// <summary>
    /// RevenueCat API anahtarları ve sabitleri.
    /// </summary>
    public static class PurchaseConstants
    {
        // ═══════════════════════════════════════════════════════════
        // REVENUECAT API KEY
        // RevenueCat Dashboard → Project Settings → API Keys
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Google Play için RevenueCat Public API Key
        /// </summary>
        public const string RevenueCatApiKey = "goog_WtjEOHcFtdepZfHyfjHFJKVkFDL";

        // ═══════════════════════════════════════════════════════════
        // ENTITLEMENTS (Yetkiler)
        // RevenueCat Dashboard → Entitlements
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Premium üyelik yetki ID'si
        /// Bu ID RevenueCat Dashboard'da tanımlanmalı
        /// </summary>
        public const string EntitlementId = "premium";

        // ═══════════════════════════════════════════════════════════
        // LOCAL PREFERENCES
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Premium durumu için local cache key
        /// </summary>
        public const string PrefKeyIsPremium = "IsUserPremium";
        
        /// <summary>
        /// Premium aktivasyon tarihi key
        /// </summary>
        public const string PrefKeyPremiumDate = "PremiumActivationDate";
    }
}
