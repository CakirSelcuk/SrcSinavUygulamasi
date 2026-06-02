using SrcSinavUygulamasi.ViewModels;
using SrcSinavUygulamasi.Services;

namespace SrcSinavUygulamasi.Views;

[QueryProperty(nameof(CategoryId), "CategoryId")]
public partial class ExamListPage : ContentPage
{
    private ExamListViewModel _viewModel;
    private readonly AdMobService? _adMobService;

    public string CategoryId
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _viewModel?.LoadExams(value);
            }
        }
    }

    public ExamListPage()
    {
        InitializeComponent();
        BannerAdView.AdsId = AdMobService.BANNER_AD_UNIT_ID;
        _viewModel = BindingContext as ExamListViewModel;
        _adMobService = Application.Current?.Handler?.MauiContext?.Services
            .GetService<AdMobService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await UpdateBannerVisibilityAsync();
    }

    private async Task UpdateBannerVisibilityAsync()
    {
        try
        {
            if (_adMobService == null)
            {
                BannerAdContainer.IsVisible = false;
                return;
            }

            BannerAdContainer.IsVisible = await _adMobService.ShouldShowBannerAsync();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"ExamList banner visibility error: {ex.Message}");
#endif
            BannerAdContainer.IsVisible = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
