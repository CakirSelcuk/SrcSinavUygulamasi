using SrcSinavUygulamasi.Views;

namespace SrcSinavUygulamasi
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Route kayıtları
            Routing.RegisterRoute(nameof(ExamListPage), typeof(ExamListPage));
            Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
            Routing.RegisterRoute(nameof(ResultPage), typeof(ResultPage));
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));
        }
    }
}