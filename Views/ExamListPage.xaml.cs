using SrcSinavUygulamasi.ViewModels;

namespace SrcSinavUygulamasi.Views;

[QueryProperty(nameof(CategoryId), "CategoryId")]
public partial class ExamListPage : ContentPage
{
    private ExamListViewModel _viewModel;

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
        _viewModel = BindingContext as ExamListViewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
