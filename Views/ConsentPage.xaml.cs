namespace SrcSinavUygulamasi.Views;

public partial class ConsentPage : ContentPage
{
    public ConsentPage()
    {
        InitializeComponent();
    }

    private void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        UpdateButtonState();
    }

    private void OnAcceptLabelTapped(object sender, EventArgs e)
    {
        AcceptCheckBox.IsChecked = !AcceptCheckBox.IsChecked;
    }

    private void UpdateButtonState()
    {
        if (AcceptCheckBox.IsChecked)
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.BackgroundColor = Color.FromArgb("#22c55e");
            ContinueButton.TextColor = Colors.White;
            WarningLabel.IsVisible = false;
        }
        else
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.BackgroundColor = Color.FromArgb("#334155");
            ContinueButton.TextColor = Color.FromArgb("#64748b");
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (!AcceptCheckBox.IsChecked)
        {
            WarningLabel.IsVisible = true;
            return;
        }

        // Onayı kaydet
        Preferences.Set("ConsentAccepted", true);
        Preferences.Set("ConsentDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Ana sayfaya git
        Application.Current.MainPage = new AppShell();
    }
}
