using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;

namespace SrcSinavUygulamasi.Views;

public partial class ResultPage : ContentPage
{
    private QuizResultModel _result;
    private ExamProgressService _progressService = new();
    private bool _allExamsCompleted = false;

    // Boş constructor (Sigorta)
    public ResultPage()
    {
        InitializeComponent();
    }

    // Veri ile gelen constructor
    public ResultPage(QuizResultModel result)
    {
        InitializeComponent();
        _result = result;

        if (result != null)
        {
            SetupUI();
            CheckAnalysisButtonState();
        }
    }

    private void SetupUI()
    {
        // Verileri ekrana bas
        LblScore.Text = _result.Score.ToString("F0");
        LblCorrect.Text = _result.CorrectCount.ToString();
        LblWrong.Text = _result.WrongCount.ToString();
        LblEmpty.Text = _result.EmptyCount.ToString();

        // Renkleri ayarla
        HeaderBox.Color = _result.ThemeColor;

        // Gerçek sınav mı?
        if (_result.IsRealExam)
        {
            SetupRealExamUI();
        }
        else
        {
            SetupPracticeExamUI();
        }
    }

    /// <summary>
    /// Analiz butonunun pasif/aktif durumunu kontrol et
    /// </summary>
    private async void CheckAnalysisButtonState()
    {
        var questionService = new QuestionService();
        var expectedExamIds = new List<string>();

        // Beklenen sınav ID'lerini hesapla
        var allQuestions = await questionService.SorulariGetir(_result.CategoryId);
        int examCount = (int)Math.Ceiling((double)allQuestions.Count / 20);
        
        for (int i = 0; i < examCount; i++)
        {
            expectedExamIds.Add($"deneme_{i + 1}");
        }

        // Resimli sorular varsa ekle
        var imageQuestions = await questionService.ResimliSorulariGetir(_result.CategoryId);
        if (imageQuestions.Count > 0)
        {
            expectedExamIds.Add("image_exam");
        }

        // Gerçek sınav varsa ekle
        var realExamQuestions = await questionService.SinavSorulariniGetir(_result.CategoryId);
        if (realExamQuestions.Count > 0)
        {
            expectedExamIds.Add("real_exam");
        }

        _allExamsCompleted = _progressService.AreAllExamsCompleted(_result.CategoryId, expectedExamIds);

        // Buton durumunu ayarla
        if (_allExamsCompleted)
        {
            BtnAnalysis.IsEnabled = true;
            BtnAnalysis.BackgroundColor = Color.FromArgb("#2563eb");
            BtnAnalysis.Text = "📊 Sınav Analizimi Göster";
        }
        else
        {
            BtnAnalysis.IsEnabled = true; // Tıklanabilir ama uyarı verecek
            BtnAnalysis.BackgroundColor = Color.FromArgb("#64748b");
            BtnAnalysis.Text = "📊 Sınav Analizi (Tüm denemeler tamamlanmalı)";
        }
    }

    private void SetupRealExamUI()
    {
        // Gerçek sınav için özel mesajlar
        if (_result.IsPassed)
        {
            // BAŞARILI
            LblMessage.Text = "🎉 Tebrikler! Sınavı Geçtiniz!";
            LblSubMessage.Text = $"{_result.CategoryTitle} gerçek sınav simülasyonunu başarıyla tamamladınız!\n\nArtık gerçek sınava hazırsınız!";
            LblScore.TextColor = Colors.LightGreen;

            BtnRetry.IsVisible = false;
            BtnNextExam.Text = "Ana Menüye Dön";
            BtnNextExam.BackgroundColor = Color.FromArgb("#22c55e");
            BtnNextExam.Clicked -= OnNextExamClicked;
            BtnNextExam.Clicked += OnGoHomeClicked;
        }
        else
        {
            // BAŞARISIZ
            LblMessage.Text = "❌ Maalesef Başarısız Oldunuz";
            LblSubMessage.Text = $"70 puan barajını geçemediniz.\nDaha fazla pratik yapmanızı öneriyoruz.";
            LblScore.TextColor = Colors.OrangeRed;

            BtnRetry.IsVisible = true;
            BtnRetry.Text = "Sınavı Tekrarla 🔄";
            BtnNextExam.Text = "Ana Menüye Dön";
            BtnNextExam.BackgroundColor = Color.FromArgb("#475569");
            BtnNextExam.Clicked -= OnNextExamClicked;
            BtnNextExam.Clicked += OnGoHomeClicked;
        }
    }

    private void SetupPracticeExamUI()
    {
        // Pratik sınav için mevcut davranış
        if (_result.IsPassed)
        {
            // BAŞARILI
            LblMessage.Text = "Tebrikler! 🎉";
            LblSubMessage.Text = $"{_result.CategoryTitle} sınavını başarıyla geçtiniz.\nBu başarıyla diğer deneme sınavına geçebilirsiniz.";
            LblScore.TextColor = Colors.LightGreen;

            BtnRetry.IsVisible = false;
            BtnNextExam.Text = "Sonraki Denemeye Geç ▶";
            BtnNextExam.BackgroundColor = Color.FromArgb("#22c55e");
        }
        else
        {
            // BAŞARISIZ
            LblMessage.Text = "Maalesef Kaldınız";
            LblSubMessage.Text = $"70 puan barajını geçemediniz.\nSınavı tekrar etmenizi tavsiye ederiz.";
            LblScore.TextColor = Colors.OrangeRed;

            BtnRetry.IsVisible = true;
            BtnRetry.Text = "Sınavı Tekrarla 🔄";
            BtnNextExam.Text = "Yeni Denemeye Geç ▶";
            BtnNextExam.BackgroundColor = Color.FromArgb("#3b82f6");
        }

        // Son denemeyse "Sonraki" butonunu gizle (sadece pratik için)
        if (_result.ExamIndex >= _result.TotalExams - 1)
        {
            BtnNextExam.Text = "Tüm Denemeler Tamamlandı";
            BtnNextExam.IsEnabled = false;
            BtnNextExam.BackgroundColor = Color.FromArgb("#475569");
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        // Aynı denemeyi tekrar başlat
        double pointsPerQuestion = _result.IsRealExam ? 2.5 : 5;
        
        await Shell.Current.GoToAsync($"../{nameof(QuizPage)}",
            new Dictionary<string, object>
            {
                { "KategoriId", _result.CategoryId },
                { "ExamIndex", _result.ExamIndex.ToString() },
                { "TotalExams", _result.TotalExams.ToString() },
                { "IsRealExam", _result.IsRealExam.ToString() },
                { "PointsPerQuestion", pointsPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
    }

    private async void OnAnalysisClicked(object sender, EventArgs e)
    {
        if (!_allExamsCompleted)
        {
            // Pasif durumda - uyarı göster
            await DisplayAlert(
                "Analiz Kullanılamıyor",
                "Sınav analizini görüntülemek için bu SRC setindeki tüm denemeleri çözmeniz gerekmektedir.",
                "Tamam");
            return;
        }

        // Aktif durumda - Analiz sayfasına git
        await Shell.Current.GoToAsync($"{nameof(AnalysisPage)}",
            new Dictionary<string, object>
            {
                { "CategoryId", _result.CategoryId }
            });
    }

    private async void OnNextExamClicked(object sender, EventArgs e)
    {
        // Sonraki denemeye geç (sadece pratik için)
        int nextExamIndex = _result.ExamIndex + 1;
        
        if (nextExamIndex < _result.TotalExams - (_result.IsRealExam ? 0 : 1))
        {
            await Shell.Current.GoToAsync($"../{nameof(QuizPage)}",
                new Dictionary<string, object>
                {
                    { "KategoriId", _result.CategoryId },
                    { "ExamIndex", nextExamIndex.ToString() },
                    { "TotalExams", _result.TotalExams.ToString() },
                    { "IsRealExam", "False" },
                    { "PointsPerQuestion", "5" }
                });
        }
    }

    private async void OnGoHomeClicked(object sender, EventArgs e)
    {
        // Ana Menüye (En başa) dön
        await Navigation.PopToRootAsync();
    }
}