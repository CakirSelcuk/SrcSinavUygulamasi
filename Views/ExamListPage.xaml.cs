using SrcSinavUygulamasi.ViewModels;

namespace SrcSinavUygulamasi.Views;

[QueryProperty(nameof(KategoriId), "KategoriId")]
public partial class ExamListPage : ContentPage
{
    private ExamListViewModel _viewModel;

    public string KategoriId
    {
        set => _viewModel?.LoadExams(value);
    }

    public ExamListPage()
    {
        InitializeComponent();
        _viewModel = BindingContext as ExamListViewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
