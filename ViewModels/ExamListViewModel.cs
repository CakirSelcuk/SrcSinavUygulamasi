using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.Views;

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

        private string _categoryId = "";
        private QuestionService _questionService = new();

        private Dictionary<string, (string title, string color)> _categoryInfo = new()
        {
            { "src1", ("SRC 1", "#FF5722") },
            { "src2", ("SRC 2", "#2196F3") },
            { "src3", ("SRC 3", "#4CAF50") },
            { "src4", ("SRC 4", "#9C27B0") }
        };

        public async void LoadExams(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId)) return;

            IsBusy = true;
            _categoryId = categoryId.ToLower();

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

                Exams.Clear();

                if (totalQuestions > 0)
                {
                    // 20'şerli gruplara böl
                    int questionsPerExam = 20;
                    int examCount = (int)Math.Ceiling((double)totalQuestions / questionsPerExam);

                    for (int i = 0; i < examCount; i++)
                    {
                        int startIndex = i * questionsPerExam;
                        int count = Math.Min(questionsPerExam, totalQuestions - startIndex);

                        Exams.Add(new ExamModel
                        {
                            Id = i + 1,
                            IconText = (i + 1).ToString(),  // Numara göster
                            Title = $"{CategoryTitle} - Deneme {i + 1}",
                            QuestionCount = count,
                            CategoryId = _categoryId,
                            StartIndex = startIndex,
                            Color = CategoryColor,
                            IsRealExam = false,
                            PointsPerQuestion = 5,
                            Subtitle = $"{count} Soru"
                        });
                    }
                }

                // Resimli soruları kontrol et (gerçek sınavdan önce)
                var imageQuestions = await _questionService.ResimliSorulariGetir(_categoryId);
                if (imageQuestions.Count > 0)
                {
                    // 15 soru = 100 puan: ilk 14 soru 7 puan, son soru 2 puan
                    // PointsPerQuestion = 6.67 kullanacağız (yaklaşık)
                    Exams.Add(new ExamModel
                    {
                        Id = 998,
                        IconText = "R",  // Resimli için "R"
                        Title = "🖼️ Resimli Sorular",
                        QuestionCount = imageQuestions.Count,
                        CategoryId = _categoryId,
                        StartIndex = 0,
                        Color = "#E91E63",  // Pembe/Magenta rengi
                        IsRealExam = false,
                        IsImageExam = true,
                        PointsPerQuestion = 6.67,  // 15 × 6.67 ≈ 100
                        Subtitle = $"{imageQuestions.Count} Görsel Soru · 100 Puan · 70+ Geçer"
                    });
                }

                // Gerçek sınav sorularını kontrol et
                var realExamQuestions = await _questionService.SinavSorulariniGetir(_categoryId);
                if (realExamQuestions.Count > 0)
                {
                    // Gerçek sınav simülasyonu ekle
                    Exams.Add(new ExamModel
                    {
                        Id = 999,
                        IconText = "S",  // Sınav için "S"
                        Title = "🎯 Gerçek Sınav Simülasyonu",
                        QuestionCount = realExamQuestions.Count,
                        CategoryId = _categoryId,
                        StartIndex = 0,
                        Color = "#FFD700",  // Altın rengi
                        IsRealExam = true,
                        PointsPerQuestion = 2.5,
                        Subtitle = $"{realExamQuestions.Count} Soru · 100 Puan · 70+ Geçer"
                    });
                }

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

            // Quiz sayfasına git, exam bilgilerini gönder
            // Not: QueryProperty string bekler, bu yüzden ToString() kullanıyoruz
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
    }
}
