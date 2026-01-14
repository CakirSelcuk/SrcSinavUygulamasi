using SrcSinavUygulamasi.Services;

namespace SrcSinavUygulamasi.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly ExamProgressService _progressService = new();

    public CategoriesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDashboardStats();
    }

    // ═══════════════════════════════════════════════════════════
    // DASHBOARD VERİLERİNİ YÜKLE
    // ═══════════════════════════════════════════════════════════
    private void LoadDashboardStats()
    {
        try
        {
            var stats = _progressService.GetUserStatistics();

            if (stats.TotalExams > 0)
            {
                // Başarı oranını göster
                double successRate = stats.SuccessRate;
                LblSuccessRate.Text = $"%{successRate:F0}";
                
                // Durum metnini belirle
                if (successRate >= 70)
                    LblStatusText.Text = "Hazırsın! ✓";
                else if (successRate >= 50)
                    LblStatusText.Text = "İyi gidiyorsun";
                else
                    LblStatusText.Text = "Biraz daha çalış";

                // Progress bar animasyonu
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(200);
                    double maxWidth = 200;
                    double targetWidth = maxWidth * (successRate / 100);
                    ProgressBar.WidthRequest = targetWidth;
                });
            }
            else
            {
                // Henüz sınav yok
                LblSuccessRate.Text = "%0";
                LblStatusText.Text = "Başla!";
                ProgressBar.WidthRequest = 0;
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
#endif
        }
    }

    // ═══════════════════════════════════════════════════════════
    // NAVİGASYON OLAYLARI
    // ═══════════════════════════════════════════════════════════

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    private async void OnAboutTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        // Analiz sayfasına git (tüm kategoriler)
        await Shell.Current.GoToAsync(nameof(AnalysisPage));
    }

    // ═══════════════════════════════════════════════════════════
    // KATEGORİ SEÇİMİ
    // ═══════════════════════════════════════════════════════════

    private async void OnCategorySrc1Tapped(object sender, EventArgs e)
    {
        await NavigateToCategory("src1");
    }

    private async void OnCategorySrc2Tapped(object sender, EventArgs e)
    {
        await NavigateToCategory("src2");
    }

    private async void OnCategorySrc3Tapped(object sender, EventArgs e)
    {
        await NavigateToCategory("src3");
    }

    private async void OnCategorySrc4Tapped(object sender, EventArgs e)
    {
        await NavigateToCategory("src4");
    }

    private async void OnCategorySrc5Tapped(object sender, EventArgs e)
    {
        await NavigateToCategory("src5");
    }

    private async Task NavigateToCategory(string categoryId)
    {
        await Shell.Current.GoToAsync($"{nameof(ExamListPage)}?CategoryId={categoryId}");
    }
}