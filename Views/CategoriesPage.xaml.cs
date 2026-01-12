namespace SrcSinavUygulamasi.Views;

public partial class CategoriesPage : ContentPage
{
	public CategoriesPage()
	{
		InitializeComponent();
	}

	private async void OnAboutTapped(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new AboutPage());
	}

	private async void OnProfileTapped(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(ProfilePage));
	}
}