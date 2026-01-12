namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Premium üyelik yönetimi.
    /// MVP için Preferences bazlı, Google Play entegrasyonu yok.
    /// </summary>
    public class PremiumService
    {
        private const string PREF_IS_PREMIUM = "is_premium_user";
        private const string PREF_PREMIUM_DATE = "premium_activation_date";
        private const string PREF_RESTORE_CODE = "premium_restore_code";
        
        // Manuel restore için gizli kod (Support ticket için)
        private const string ADMIN_RESTORE_CODE = "SRC2024PREMIUM";

        /// <summary>
        /// Kullanıcı premium mu?
        /// </summary>
        public bool IsUserPremium
        {
            get => Preferences.Get(PREF_IS_PREMIUM, false);
            private set => Preferences.Set(PREF_IS_PREMIUM, value);
        }

        /// <summary>
        /// Premium aktivasyon tarihi
        /// </summary>
        public DateTime? PremiumActivationDate
        {
            get
            {
                var ticks = Preferences.Get(PREF_PREMIUM_DATE, 0L);
                return ticks > 0 ? new DateTime(ticks) : null;
            }
        }

        /// <summary>
        /// Kullanıcının restore kodu (destek için)
        /// </summary>
        public string? UserRestoreCode
        {
            get => Preferences.Get(PREF_RESTORE_CODE, null);
        }

        /// <summary>
        /// Premium özellik listesi
        /// </summary>
        public static class Features
        {
            public const string WRONG_ANSWERS = "wrong_answers";   // Yanlışları Çöz
            public const string ANALYSIS = "analysis";             // Detaylı Analiz
            public const string MINI_EXAM = "mini_exam";          // Mini Sınav (Yanlışlardan)
            public const string AD_FREE = "ad_free";              // Reklamsız
        }

        /// <summary>
        /// Belirli bir özellik açık mı?
        /// </summary>
        public bool HasFeature(string featureName)
        {
            // MVP'de tüm premium özellikler tek paket
            return IsUserPremium;
        }

        /// <summary>
        /// Premium satın alma işlemi (MVP: Simüle)
        /// Gerçek uygulamada Google Play / App Store entegrasyonu
        /// </summary>
        public async Task<bool> PurchasePremiumAsync()
        {
            // TODO: Google Play Billing / App Store IAP entegrasyonu
            // Şimdilik sadece placeholder
            
            await Task.Delay(500); // Simüle işlem süresi
            
            // Gerçek satın alma başarılıysa:
            // ActivatePremium();
            // return true;
            
            return false;
        }

        /// <summary>
        /// Premium'u aktive et (Satın alma veya Restore sonrası)
        /// </summary>
        public void ActivatePremium()
        {
            IsUserPremium = true;
            Preferences.Set(PREF_PREMIUM_DATE, DateTime.UtcNow.Ticks);
            
            // Benzersiz restore kodu oluştur
            var restoreCode = GenerateRestoreCode();
            Preferences.Set(PREF_RESTORE_CODE, restoreCode);
            
            System.Diagnostics.Debug.WriteLine($"✅ Premium activated! Restore code: {restoreCode}");
        }

        /// <summary>
        /// Premium'u kaldır (Test/Debug için)
        /// </summary>
        public void DeactivatePremium()
        {
            IsUserPremium = false;
            // Restore code'u silmiyoruz - destek için kalır
        }

        /// <summary>
        /// Manuel restore (Destek ticket sonrası)
        /// Kullanıcı ayarlar sayfasından kodu girer
        /// </summary>
        public bool ManualRestore(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            // Admin kodu ile restore
            if (code.Trim().ToUpperInvariant() == ADMIN_RESTORE_CODE)
            {
                ActivatePremium();
                return true;
            }

            // Kullanıcının kendi restore kodu ile restore
            var savedCode = Preferences.Get(PREF_RESTORE_CODE, "");
            if (!string.IsNullOrEmpty(savedCode) && code.Trim().ToUpperInvariant() == savedCode.ToUpperInvariant())
            {
                ActivatePremium();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Upsell popup göster
        /// </summary>
        public async Task ShowUpsellPopupAsync(string feature)
        {
            string title = "Premium Özellik 🔒";
            string message = feature switch
            {
                Features.WRONG_ANSWERS => "Yanlış cevapladığınız soruları tekrar çözmek için Premium üyelik gerekiyor.\n\nPremium ile:\n• Tüm yanlışlarınızı çözün\n• Konu bazlı analiz görün\n• Reklamsız deneyim",
                Features.ANALYSIS => "Detaylı konu analizi ve eksik tarama Premium özelliğidir.",
                Features.MINI_EXAM => "Yanlışlardan oluşan Mini Sınav Premium özelliğidir.",
                _ => "Bu özellik Premium üyelere özeldir."
            };

            bool result = await Application.Current.MainPage.DisplayAlert(
                title,
                message,
                "Premium'a Geç",
                "Şimdilik Geç");

            if (result)
            {
                // Satın alma sayfasına yönlendir
                await NavigateToPurchasePageAsync();
            }
        }

        /// <summary>
        /// Satın alma sayfasına git
        /// </summary>
        private async Task NavigateToPurchasePageAsync()
        {
            // TODO: Gerçek satın alma akışı
            // Şimdilik sadece bilgi mesajı
            await Application.Current.MainPage.DisplayAlert(
                "Yakında!",
                "Premium satın alma özelliği yakında aktif olacak.\n\nŞimdilik ücretsiz kullanmaya devam edin.",
                "Tamam");
        }

        /// <summary>
        /// Benzersiz restore kodu oluştur
        /// </summary>
        private string GenerateRestoreCode()
        {
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            return $"SRC-{guid}";
        }

        /// <summary>
        /// Debug: Premium durumunu logla
        /// </summary>
        public void DebugLogStatus()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"Premium Status: {IsUserPremium}");
            System.Diagnostics.Debug.WriteLine($"Activation Date: {PremiumActivationDate}");
            System.Diagnostics.Debug.WriteLine($"Restore Code: {UserRestoreCode}");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════");
#endif
        }
    }
}
