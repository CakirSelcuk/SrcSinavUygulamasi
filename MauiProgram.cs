using Microsoft.Extensions.Logging;
using Plugin.MauiMTAdmob;
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
            .UseMauiMTAdmob()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ═══════════════════════════════════════════════════════════
        // ADMOB CONFIGURATION
        // ═══════════════════════════════════════════════════════════
        // Plugin.MauiMTAdmob initializes via UseMauiMTAdmob() and AndroidManifest.xml

        // ═══════════════════════════════════════════════════════════
        // SERVICES
        // ═══════════════════════════════════════════════════════════
        
        // Premium & Purchase
        builder.Services.AddSingleton<PremiumService>();
        builder.Services.AddSingleton<PurchaseService>();
        
        // AdMob
        builder.Services.AddSingleton<AdMobService>();
        
        // Question & Exam
        builder.Services.AddSingleton<QuestionService>();
        builder.Services.AddSingleton<ExamProgressService>();
        
        // ═══════════════════════════════════════════════════════════
        // VIEWMODELS
        // ═══════════════════════════════════════════════════════════
        builder.Services.AddSingleton<QuizViewModel>();
        
        // ═══════════════════════════════════════════════════════════
        // PAGES
        // ═══════════════════════════════════════════════════════════
        builder.Services.AddSingleton<QuizPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
