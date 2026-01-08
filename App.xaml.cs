using SrcSinavUygulamasi.Views;

namespace SrcSinavUygulamasi;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // İlk açılışta onay kontrolü
        bool consentAccepted = Preferences.Get("ConsentAccepted", false);
        
        if (consentAccepted)
        {
            MainPage = new AppShell();
        }
        else
        {
            MainPage = new NavigationPage(new ConsentPage());
        }
    }
}