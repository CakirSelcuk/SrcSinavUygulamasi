using Microsoft.Extensions.Logging;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.ViewModels;
using SrcSinavUygulamasi.Views;

namespace SrcSinavUygulamasi;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<QuestionService>();
        builder.Services.AddSingleton<PurchaseService>();
        
        // ViewModels
        builder.Services.AddSingleton<QuizViewModel>();
        
        // Pages
        builder.Services.AddSingleton<QuizPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
