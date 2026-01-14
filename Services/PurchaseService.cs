using SrcSinavUygulamasi.Constants;

#if ANDROID || IOS
using Plugin.InAppBilling;
#endif

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Google Play / App Store satın alma servisi.
    /// Plugin.InAppBilling kullanır.
    /// RevenueCat takibi Server-to-Server (Pub/Sub) ile yapılır.
    /// Singleton olarak kullanılmalı.
    /// </summary>
    public class PurchaseService
    {
        private bool _isInitialized = false;

        // ═══════════════════════════════════════════════════════════
        // BAŞLATMA (Initialize)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Billing SDK'yı başlat.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
#if ANDROID || IOS
                var connected = await CrossInAppBilling.Current.ConnectAsync();
                if (connected)
                {
                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("✅ InAppBilling: Mağaza bağlantısı başarılı");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ InAppBilling: Mağaza bağlantısı kurulamadı");
                }
#else
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("ℹ️ InAppBilling: Windows/Mac - Simülasyon modu");
                await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InAppBilling Initialize Hata: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PAKETLERİ GETİR (Get Offerings)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Mağazadan mevcut satın alma ürünlerini getir.
        /// Google Play Console'da tanımlanan ürün ID'lerini kullanır.
        /// </summary>
        public async Task<List<ProductInfo>?> GetOfferingsAsync()
        {
            try
            {
#if ANDROID || IOS
                if (!_isInitialized)
                    await InitializeAsync();

                // Google Play Console'da tanımlanan ürün ID'leri
                var productIds = new[] { "premium_lifetime", "premium_yearly", "premium_monthly" };
                
                var products = await CrossInAppBilling.Current.GetProductInfoAsync(
                    ItemType.InAppPurchase,
                    productIds);

                if (products == null || !products.Any())
                    return null;

                return products.Select(p => new ProductInfo
                {
                    ProductId = p.ProductId,
                    Title = p.Name,
                    Description = p.Description,
                    FormattedPrice = p.LocalizedPrice,
                    PriceAmount = (decimal)p.MicrosPrice / 1000000m
                }).ToList();
#else
                await Task.Delay(300);
                return new List<ProductInfo>
                {
                    new()
                    {
                        ProductId = "premium_lifetime",
                        Title = "Ömür Boyu Premium",
                        Description = "Tek seferlik ödeme, sınırsız erişim",
                        FormattedPrice = "₺49,99",
                        PriceAmount = 49.99m
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetOfferings Hata: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SATIN ALMA (Purchase)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Seçilen ürünü satın al.
        /// </summary>
        public async Task<PurchaseResult> PurchaseAsync(string productId)
        {
#if ANDROID || IOS
            try
            {
                if (!_isInitialized)
                    await InitializeAsync();

                var purchase = await CrossInAppBilling.Current.PurchaseAsync(
                    productId, 
                    ItemType.InAppPurchase);

                if (purchase != null)
                {
                    // Satın alma başarılı - Google Play Console Pub/Sub ile RevenueCat'e bildirilecek
                    ActivatePremium();
                    return new PurchaseResult { Success = true, Message = "Satın alma başarılı!" };
                }
                
                return new PurchaseResult { Success = false, Message = "Satın alma iptal edildi." };
            }
            catch (InAppBillingPurchaseException pex)
            {
                return new PurchaseResult { Success = false, Message = pex.Message ?? "Satın alma iptal edildi." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Purchase Hata: {ex.Message}");
                return new PurchaseResult { Success = false, Message = $"Hata: {ex.Message}" };
            }
#else
            await Task.Delay(500);
            return new PurchaseResult 
            { 
                Success = false, 
                Message = "Windows'ta gerçek satın alma yapılamaz.\nAndroid veya iOS cihazda deneyin." 
            };
#endif
        }

        // ═══════════════════════════════════════════════════════════
        // SATIN ALIMLARI GERİ YÜKLE (Restore Purchases)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Önceki satın alımları geri yükle.
        /// </summary>
        public async Task<RestoreResult> RestorePurchasesAsync()
        {
            try
            {
#if ANDROID || IOS
                if (!_isInitialized)
                    await InitializeAsync();

                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);

                if (purchases != null && purchases.Any(p => p.State == PurchaseState.Purchased))
                {
                    ActivatePremium();
                    return new RestoreResult 
                    { 
                        Success = true, 
                        HasActivePurchase = true,
                        Message = "Satın alımlarınız başarıyla geri yüklendi!" 
                    };
                }
                
                return new RestoreResult 
                { 
                    Success = true, 
                    HasActivePurchase = false,
                    Message = "Aktif satın alım bulunamadı." 
                };
#else
                await Task.Delay(500);
                return new RestoreResult 
                { 
                    Success = true, 
                    HasActivePurchase = false,
                    Message = "Windows'ta geri yükleme yapılamaz.\nAndroid veya iOS cihazda deneyin." 
                };
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Restore Hata: {ex.Message}");
                return new RestoreResult 
                { 
                    Success = false, 
                    HasActivePurchase = false,
                    Message = $"Geri yükleme hatası: {ex.Message}" 
                };
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PREMIUM AKTİFLEŞTİR (Helper)
        // ═══════════════════════════════════════════════════════════

        private void ActivatePremium()
        {
            Preferences.Set(PurchaseConstants.PrefKeyIsPremium, true);
            Preferences.Set(PurchaseConstants.PrefKeyPremiumDate, DateTime.UtcNow.ToString("o"));
            System.Diagnostics.Debug.WriteLine("✅ Premium aktifleştirildi!");
        }

        /// <summary>
        /// Premium durumunu kontrol et (local cache'den).
        /// </summary>
        public bool IsPremium => Preferences.Get(PurchaseConstants.PrefKeyIsPremium, false);

        // ═══════════════════════════════════════════════════════════
        // BAĞLANTIYI KAPAT
        // ═══════════════════════════════════════════════════════════

        public async Task DisconnectAsync()
        {
            try
            {
#if ANDROID || IOS
                await CrossInAppBilling.Current.DisconnectAsync();
#else
                await Task.CompletedTask;
#endif
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Disconnect Hata: {ex.Message}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // YARDIMCI MODELLER
    // ═══════════════════════════════════════════════════════════

    public class ProductInfo
    {
        public string ProductId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string FormattedPrice { get; set; } = "";
        public decimal PriceAmount { get; set; }
    }

    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public bool HasActivePurchase { get; set; }
        public string Message { get; set; } = "";
    }
}
