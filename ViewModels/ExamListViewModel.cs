using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.Views;
using SrcSinavUygulamasi.Constants;

namespace SrcSinavUygulamasi.ViewModels
{
    public partial class ExamListViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ExamModel> exams = new();

        [ObservableProperty]
        private string categoryTitle = "";

        [ObservableProperty]
        private string categoryColor = "#4CAF50";

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool showAnalysisButton;

        [ObservableProperty]
        private string readinessLabel = "";

        [ObservableProperty]
        private string readinessColor = "#64748b";

        private string _categoryId = "";
        private QuestionService _questionService = new();
        private ExamProgressService _progressService = new();
        private BalancedExamBuilder _examBuilder = new();
        private List<string> _expectedExamIds = new();

        private Dictionary<string, (string title, string color)> _categoryInfo = new()
        {
            { "src1", ("SRC 1", "#FF5722") },
            { "src2", ("SRC 2", "#2196F3") },
            { "src3", ("SRC 3", "#4CAF50") },
            { "src4", ("SRC 4", "#9C27B0") },
            { "src5", ("SRC Kurye", "#00BCD4") }
        };

        public async void LoadExams(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId)) return;

            IsBusy = true;
            _categoryId = categoryId.ToLower();
            _expectedExamIds.Clear();

            // Kategori bilgilerini ayarla
            if (_categoryInfo.TryGetValue(_categoryId, out var info))
            {
                CategoryTitle = info.title;
                CategoryColor = info.color;
            }

            try
            {
                // Pratik soruları getir
                var allQuestions = await _questionService.SorulariGetir(_categoryId);
                int totalQuestions = allQuestions.Count;

                // Tamamlanan sınavları getir
                var completedExamIds = _progressService.GetCompletedExamIds(_categoryId);

                Exams.Clear();

                if (totalQuestions > 0)
                {
                    int maxExamCount = 5;
                    int examCount = Math.Min(maxExamCount, _examBuilder.CalculateMaxExams(allQuestions));

                    for (int i = 0; i < examCount; i++)
                    {
                        int startIndex = 0;
                        int count = ExamRules.QuestionCount;
                        string examId = $"deneme_{i + 1}";
                        bool isCompleted = completedExamIds.Contains(examId);

                        _expectedExamIds.Add(examId);

                        Exams.Add(new ExamModel
                        {
                            Id = i + 1,
                            IconText = isCompleted ? "✓" : (i + 1).ToString(),
                            Title = $"{CategoryTitle} - Deneme {i + 1}",
                            QuestionCount = count,
                            CategoryId = _categoryId,
                            StartIndex = startIndex,
                            Color = isCompleted ? "#22c55e" : CategoryColor,
                            IsRealExam = false,
                            PointsPerQuestion = ExamRules.PointsPerQuestion,
                            Subtitle = isCompleted
                                ? $"✓ Tamamlandı · {count} Soru"
                                : $"{ExamRules.QuestionCount} Soru · {ExamRules.DurationMinutes} Dakika · {ExamRules.PassScore}+ Geçer"
                        });
                    }
                }

                // Gerçek sınav sorularını kontrol et
                var realExamQuestions = await _questionService.SinavSorulariniGetir(_categoryId);
                if (realExamQuestions.Count >= ExamRules.QuestionCount)
                {
                    string examId = "real_exam";
                    bool isCompleted = completedExamIds.Contains(examId);
                    _expectedExamIds.Add(examId);

                    Exams.Add(new ExamModel
                    {
                        Id = 999,
                        IconText = isCompleted ? "✓" : "S",
                        Title = "🎯 SRC e-Sınav Simülasyonu",
                        QuestionCount = ExamRules.QuestionCount,
                        CategoryId = _categoryId,
                        StartIndex = 0,
                        Color = isCompleted ? "#22c55e" : "#FFD700",
                        IsRealExam = true,
                        PointsPerQuestion = ExamRules.PointsPerQuestion,
                        Subtitle = isCompleted
                            ? $"✓ Tamamlandı · {ExamRules.QuestionCount} Soru"
                            : $"{ExamRules.QuestionCount} Soru · {ExamRules.DurationMinutes} Dakika · {ExamRules.PassScore}+ Geçer"
                    });
                }

                // En az 1 sınav tamamlandı mı kontrol et (Analiz butonu için)
                var completedExams = _progressService.GetCompletedExamIds(_categoryId);
                ShowAnalysisButton = completedExams.Count > 0;

                // ExamCatalog'a kaydet (diğer sayfalarda kullanılacak)
                int practiceExamCount = Exams.Count(e => !e.IsRealExam && !e.IsImageExam && e.QuestionCount > 0);
                bool hasImageExam = false;
                bool hasRealExam = Exams.Any(e => e.IsRealExam);
                ExamCatalog.RegisterCategoryExams(_categoryId, practiceExamCount, hasImageExam, hasRealExam);

                // Hazırlık durumu
                var readiness = _progressService.GetReadinessStatus(_categoryId);
                ReadinessLabel = readiness.Label;
                ReadinessColor = readiness.Color;

                // Hiç soru yoksa uyarı
                if (Exams.Count == 0)
                {
                    Exams.Add(new ExamModel
                    {
                        Id = 0,
                        IconText = "?",
                        Title = "Henüz soru eklenmemiş",
                        QuestionCount = 0,
                        CategoryId = _categoryId,
                        Color = CategoryColor,
                        Subtitle = "Sorular yakında eklenecek"
                    });
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectExam(ExamModel exam)
        {
            if (exam == null || exam.QuestionCount == 0) return;

            // Özel sınavlar için ExamIndex = 0, normal denemeler için kendi indexi
            string examIndex = (exam.IsRealExam || exam.IsImageExam) ? "0" : (exam.Id - 1).ToString();

            await Shell.Current.GoToAsync($"{nameof(QuizPage)}",
                new Dictionary<string, object>
                {
                    { "KategoriId", exam.CategoryId },
                    { "ExamIndex", examIndex },
                    { "TotalExams", Exams.Count.ToString() },
                    { "IsRealExam", exam.IsRealExam.ToString() },
                    { "IsImageExam", exam.IsImageExam.ToString() },
                    { "PointsPerQuestion", exam.PointsPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                });
        }

        [RelayCommand]
        private async Task OpenAnalysis()
        {
            await Shell.Current.GoToAsync($"{nameof(AnalysisPage)}",
                new Dictionary<string, object>
                {
                    { "CategoryId", _categoryId }
                });
        }
    }
}
