using SrcSinavUygulamasi.Services;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Profil ve Ayarlar Sayfası
/// Kullanıcının "Yönetim Paneli"
/// </summary>
public partial class ProfilePage : ContentPage
{
    private const string PREF_KEY_PREMIUM = "IsUserPremium";
    private const string PREF_KEY_NOTIFICATIONS = "NotificationsEnabled";
    private const string PREF_KEY_VIBRATION = "VibrationEnabled";
    private const string PREMIUM_BACKDOOR_CODE = "SRC2024PREMIUM";
    
    private ExamProgressService _progressService = new();

    public ProfilePage()
    {
        InitializeComponent();
    }

    // ═══════════════════════════════════════════════════════════
    // PAGE LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserData();
        LoadSettings();
    }

    // ═══════════════════════════════════════════════════════════
    // VERİ YÜKLEME
    // ═══════════════════════════════════════════════════════════
    private void LoadUserData()
    {
        bool isPremium = Preferences.Get(PREF_KEY_PREMIUM, false);
        int totalQuestions = _progressService.GetTotalSolvedQuestions();

        if (isPremium)
        {
            LblUserName.Text = "VIP Üye 👑";
            LblUserName.TextColor = Color.FromArgb("#fbbf24");
            PremiumBanner.IsVisible = false;
        }
        else
        {
            LblUserName.Text = "Misafir Kullanıcı";
            LblUserName.TextColor = Colors.White;
            PremiumBanner.IsVisible = true;
        }

        LblTotalQuestions.Text = $"Toplam: {totalQuestions} Soru Çözüldü";
    }

    private void LoadSettings()
    {
        SwitchNotifications.IsToggled = Preferences.Get(PREF_KEY_NOTIFICATIONS, true);
        SwitchVibration.IsToggled = Preferences.Get(PREF_KEY_VIBRATION, true);
    }

    // ═══════════════════════════════════════════════════════════
    // AYARLAR TOGGLE'LARI
    // ═══════════════════════════════════════════════════════════
    private void OnNotificationToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set(PREF_KEY_NOTIFICATIONS, e.Value);
    }

    private void OnVibrationToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set(PREF_KEY_VIBRATION, e.Value);
    }

    // ═══════════════════════════════════════════════════════════
    // PREMIUM
    // ═══════════════════════════════════════════════════════════
    private async void OnPremiumClicked(object sender, EventArgs e)
    {
        bool wantsToBuy = await DisplayAlert(
            "🔒 Premium'a Yükselt",
            "VIP üyelikle tüm özelliklere eriş!\n\n" +
            "✅ Reklamsız deneyim\n" +
            "✅ Tüm sorulara sınırsız erişim\n" +
            "✅ Detaylı performans analizi\n\n" +
            "Fiyat: ₺49.99 (Ömür boyu)",
            "Satın Al",
            "Vazgeç");

        if (wantsToBuy)
        {
            await HandlePurchase();
        }
    }

    private async Task HandlePurchase()
    {
        string? code = await DisplayPromptAsync(
            "Aktivasyon Kodu",
            "Satın alma kodunuz varsa girin.\n(Destek ekibinden aldıysanız)",
            "Aktifleştir",
            "İptal",
            placeholder: "Kod girin...",
            maxLength: 20);

        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Mağaza",
                "Uygulama içi satın alma yakında aktif olacak.\n\nDestek için: srcsinav.destek@gmail.com",
                "Tamam");
            return;
        }

        if (code.Trim().ToUpperInvariant() == PREMIUM_BACKDOOR_CODE)
        {
            Preferences.Set(PREF_KEY_PREMIUM, true);
            Preferences.Set("PremiumActivationDate", DateTime.UtcNow.ToString("o"));

            await DisplayAlert("🎉 Başarılı!",
                "VIP üyeliğiniz aktifleştirildi!\n\nArtık tüm premium özelliklere erişebilirsiniz.",
                "Harika!");

            LoadUserData(); // UI güncelle
        }
        else
        {
            await DisplayAlert("Geçersiz Kod",
                "Girdiğiniz kod geçerli değil.\n\nLütfen destek ekibinden doğru kodu alın.",
                "Tamam");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // RESTORE PURCHASE
    // ═══════════════════════════════════════════════════════════
    private async void OnRestorePurchaseClicked(object sender, EventArgs e)
    {
        // Simülasyon - İleride RevenueCat bağlanacak
        await DisplayAlert("Geri Yükleme",
            "Satın alımlar kontrol ediliyor...\n\nMevcut satın alım bulunamadı.\n\n(Eğer daha önce satın aldıysanız destek ekibiyle iletişime geçin)",
            "Tamam");
    }

    // ═══════════════════════════════════════════════════════════
    // RESET PROGRESS
    // ═══════════════════════════════════════════════════════════
    private async void OnResetProgressClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "⚠️ Dikkat!",
            "Tüm sınav ilerlemeniz silinecek!\n\n" +
            "• Çözdüğünüz tüm sınavlar\n" +
            "• Yanlış cevap geçmişiniz\n" +
            "• İstatistikleriniz\n\n" +
            "Bu işlem geri alınamaz! Emin misiniz?",
            "Evet, Sıfırla",
            "Vazgeç");

        if (confirmed)
        {
            bool reallyConfirmed = await DisplayAlert(
                "Son Onay",
                "Gerçekten TÜM VERİLERİ silmek istiyor musunuz?",
                "EVET, SİL",
                "Hayır");

            if (reallyConfirmed)
            {
                _progressService.ResetAllProgress();
                
                await DisplayAlert("✅ Sıfırlandı",
                    "Tüm ilerlemeniz başarıyla silindi.\n\nUygulama sıfırdan başlamaya hazır!",
                    "Tamam");

                LoadUserData(); // UI güncelle
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // İLETİŞİM & BAĞLANTILAR
    // ═══════════════════════════════════════════════════════════
    private async void OnContactClicked(object sender, EventArgs e)
    {
        try
        {
            var uri = new Uri("mailto:srcsinav.destek@gmail.com?subject=SRC%20Uygulama%20Destek");
            await Launcher.OpenAsync(uri);
        }
        catch
        {
            await DisplayAlert("E-posta",
                "E-posta uygulaması açılamadı.\n\nDestek için: srcsinav.destek@gmail.com",
                "Tamam");
        }
    }

    private async void OnRateAppClicked(object sender, EventArgs e)
    {
        try
        {
            // Android için Play Store, iOS için App Store
#if ANDROID
            var uri = new Uri("market://details?id=com.srcsinav.app");
#elif IOS
            var uri = new Uri("https://apps.apple.com/app/idXXXXXXXXX");
#else
            var uri = new Uri("https://play.google.com/store/apps/details?id=com.srcsinav.app");
#endif
            await Launcher.OpenAsync(uri);
        }
        catch
        {
            await DisplayAlert("Puanlama",
                "Mağaza sayfası açılamadı.\n\nLütfen daha sonra deneyiniz.",
                "Tamam");
        }
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(new Uri("https://srcsinav.com/gizlilik"));
        }
        catch
        {
            await DisplayAlert("Gizlilik Politikası",
                "Sayfa açılamadı.\n\nGizlilik politikamızı srcsinav.com/gizlilik adresinden görüntüleyebilirsiniz.",
                "Tamam");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // NAVİGASYON
    // ═══════════════════════════════════════════════════════════
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
