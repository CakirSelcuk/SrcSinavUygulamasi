namespace SrcSinavUygulamasi.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        LoadConsentDate();
    }

    private void LoadConsentDate()
    {
        var consentDate = Preferences.Get("ConsentDate", "");
        if (!string.IsNullOrEmpty(consentDate))
        {
            ConsentDateLabel.Text = consentDate;
        }
        else
        {
            ConsentDateLabel.Text = "Kayıt bulunamadı";
        }
    }
}
