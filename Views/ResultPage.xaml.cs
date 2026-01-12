using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Result Page - Sınav sonuç ekranı
/// Kurallar:
/// 1. "Yeni denemeye geç" sadece aynı SRC içinde sonraki DENEME varsa görünür
/// 2. "Sınav Sonucu Değerlendirme" tüm denemeler bitene kadar kilitli
/// 3. Resimli/Gerçek sınava otomatik geçiş YOK
/// </summary>
public partial class ResultPage : ContentPage
{
    private QuizResultModel _result;
    private ExamProgressService _progressService = new();
    private bool _allPracticeExamsCompleted = false;
    private string? _nextPracticeExamId = null;

    public ResultPage()
    {
        InitializeComponent();
    }

    public ResultPage(QuizResultModel result)
    {
        InitializeComponent();
        _result = result;

        if (result != null)
        {
            SetupUI();
            CheckExamState();
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

        // Başlık - ExamCatalog'dan gelen DisplayTitle kullan
        string displayTitle = ExamCatalog.GetDisplayTitle(_result.CategoryId, _result.ExamId);

        // Sınav tipine göre UI ayarla
        if (_result.IsRealExam || _result.IsImageExam)
        {
            SetupSpecialExamUI(displayTitle);
        }
        else
        {
            SetupPracticeExamUI(displayTitle);
        }
    }

    /// <summary>
    /// Sınav durumunu kontrol et - Analiz ve Sonraki Deneme butonları için
    /// </summary>
    private void CheckExamState()
    {
        var completedExamIds = _progressService.GetCompletedExamIds(_result.CategoryId);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"📋 ResultPage CheckExamState:");
        System.Diagnostics.Debug.WriteLine($"   CategoryId: {_result.CategoryId}");
        System.Diagnostics.Debug.WriteLine($"   ExamId: {_result.ExamId}");
        System.Diagnostics.Debug.WriteLine($"   Completed: [{string.Join(", ", completedExamIds)}]");
#endif

        // ═══════════════════════════════════════════════════════════
        // 1. TÜM DENEME SINAVLARI TAMAMLANDI MI? (Değerlendirme butonu için)
        // ═══════════════════════════════════════════════════════════
        _allPracticeExamsCompleted = ExamCatalog.AreAllPracticeExamsCompleted(
            _result.CategoryId, 
            completedExamIds);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"   All Practice Exams Completed: {_allPracticeExamsCompleted}");
#endif

        UpdateAnalysisButtonState();

        // ═══════════════════════════════════════════════════════════
        // 2. SONRAKİ DENEME VAR MI? (Sadece deneme sınavları için)
        // ═══════════════════════════════════════════════════════════
        if (!_result.IsRealExam && !_result.IsImageExam)
        {
            _nextPracticeExamId = ExamCatalog.GetNextPracticeExamId(
                _result.CategoryId, 
                _result.ExamId);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"   Next Practice Exam: {_nextPracticeExamId ?? "null (son deneme)"}");
#endif
        }

        UpdateNextExamButtonState();
    }

    private void UpdateAnalysisButtonState()
    {
        if (_allPracticeExamsCompleted)
        {
            // Tüm denemeler tamamlandı - buton AKTİF
            BtnAnalysis.IsEnabled = true;
            BtnAnalysis.BackgroundColor = Color.FromArgb("#2563eb");
            BtnAnalysis.Text = "📊 Sınav Analizimi Göster";
            BtnAnalysis.Opacity = 1.0;
        }
        else
        {
            // Denemeler eksik - buton KİLİTLİ görünümde ama tıklanabilir (uyarı için)
            BtnAnalysis.IsEnabled = true;
            BtnAnalysis.BackgroundColor = Color.FromArgb("#64748b");
            BtnAnalysis.Text = "📊 Sınav Sonucu Değerlendirme";
            BtnAnalysis.Opacity = 0.6;
        }
    }

    private void UpdateNextExamButtonState()
    {
        // Gerçek sınav veya resimli sınavda "Yeni denemeye geç" butonu gösterilmez
        // (Bu sınavlar için ayrı UI ayarlanıyor)
        if (_result.IsRealExam || _result.IsImageExam)
        {
            return;
        }

        // Sonraki deneme var mı?
        bool hasNextPracticeExam = !string.IsNullOrEmpty(_nextPracticeExamId);

        if (hasNextPracticeExam)
        {
            BtnNextExam.IsVisible = true;
            BtnNextExam.IsEnabled = true;
            
            // Sonraki sınavın adını göster
            string nextTitle = ExamCatalog.GetDisplayTitle(_result.CategoryId, _nextPracticeExamId!);
            BtnNextExam.Text = $"Yeni Denemeye Geç ▶";
            BtnNextExam.BackgroundColor = _result.IsPassed 
                ? Color.FromArgb("#22c55e")  // Yeşil - başarılı
                : Color.FromArgb("#3b82f6"); // Mavi - başarısız
        }
        else
        {
            // SON DENEME - "Yeni denemeye geç" butonu KESİNLİKLE GÖRÜNMEZ
            BtnNextExam.IsVisible = false;
        }
    }

    private void SetupSpecialExamUI(string displayTitle)
    {
        // Gerçek sınav veya resimli sorular için özel UI
        if (_result.IsPassed)
        {
            LblMessage.Text = "🎉 Tebrikler! Sınavı Geçtiniz!";
            LblSubMessage.Text = $"{displayTitle} tamamlandı!\n\nArtık gerçek sınava hazırsınız!";
            LblScore.TextColor = Colors.LightGreen;

            BtnRetry.IsVisible = false;
            BtnNextExam.Text = "Ana Menüye Dön";
            BtnNextExam.BackgroundColor = Color.FromArgb("#22c55e");
            BtnNextExam.IsVisible = true;
            BtnNextExam.Clicked -= OnNextExamClicked;
            BtnNextExam.Clicked += OnGoHomeClicked;
        }
        else
        {
            LblMessage.Text = "❌ Maalesef Başarısız Oldunuz";
            LblSubMessage.Text = $"70 puan barajını geçemediniz.\nDaha fazla pratik yapmanızı öneriyoruz.";
            LblScore.TextColor = Colors.OrangeRed;

            BtnRetry.IsVisible = true;
            BtnRetry.Text = "Sınavı Tekrarla 🔄";
            BtnNextExam.Text = "Ana Menüye Dön";
            BtnNextExam.BackgroundColor = Color.FromArgb("#475569");
            BtnNextExam.IsVisible = true;
            BtnNextExam.Clicked -= OnNextExamClicked;
            BtnNextExam.Clicked += OnGoHomeClicked;
        }
    }

    private void SetupPracticeExamUI(string displayTitle)
    {
        // Deneme sınavı için UI
        if (_result.IsPassed)
        {
            LblMessage.Text = "Tebrikler! 🎉";
            LblSubMessage.Text = $"{displayTitle} başarıyla tamamlandı.\nDiğer deneme sınavına geçebilirsiniz.";
            LblScore.TextColor = Colors.LightGreen;
            BtnRetry.IsVisible = false;
        }
        else
        {
            LblMessage.Text = "Maalesef Kaldınız";
            LblSubMessage.Text = $"70 puan barajını geçemediniz.\nSınavı tekrar etmenizi tavsiye ederiz.";
            LblScore.TextColor = Colors.OrangeRed;
            BtnRetry.IsVisible = true;
            BtnRetry.Text = "Sınavı Tekrarla 🔄";
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        double pointsPerQuestion = _result.IsRealExam ? 2.5 : (_result.IsImageExam ? 6.67 : 5);
        
        await Shell.Current.GoToAsync($"../{nameof(QuizPage)}",
            new Dictionary<string, object>
            {
                { "KategoriId", _result.CategoryId },
                { "ExamIndex", _result.ExamIndex.ToString() },
                { "TotalExams", _result.TotalExams.ToString() },
                { "IsRealExam", _result.IsRealExam.ToString() },
                { "IsImageExam", _result.IsImageExam.ToString() },
                { "PointsPerQuestion", pointsPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
    }

    private async void OnAnalysisClicked(object sender, EventArgs e)
    {
        if (!_allPracticeExamsCompleted)
        {
            // Denemeler tamamlanmadı - UYARI göster, navigation YAPMA
            await DisplayAlert(
                "Değerlendirme Kullanılamıyor",
                "Değerlendirmeyi görmek için bu SRC setindeki tüm denemeleri tamamlamalısınız.",
                "Tamam");
            return;
        }

        // Tüm denemeler tamamlandı - Analiz sayfasına git
        await Shell.Current.GoToAsync($"{nameof(AnalysisPage)}",
            new Dictionary<string, object>
            {
                { "CategoryId", _result.CategoryId }
            });
    }

    private async void OnNextExamClicked(object sender, EventArgs e)
    {
        // Sonraki denemeye geç (sadece deneme sınavları için)
        if (string.IsNullOrEmpty(_nextPracticeExamId))
        {
            // Bu durumda buton zaten görünmez olmalı ama sigorta
            await Navigation.PopToRootAsync();
            return;
        }

        // ExamId'den ExamIndex hesapla
        int nextExamIndex = ExamCatalog.GetExamIndex(_result.CategoryId, _nextPracticeExamId);
        if (nextExamIndex < 0) nextExamIndex = _result.ExamIndex + 1;

        await Shell.Current.GoToAsync($"../{nameof(QuizPage)}",
            new Dictionary<string, object>
            {
                { "KategoriId", _result.CategoryId },
                { "ExamIndex", nextExamIndex.ToString() },
                { "TotalExams", _result.TotalExams.ToString() },
                { "IsRealExam", "False" },
                { "IsImageExam", "False" },
                { "PointsPerQuestion", "5" }
            });
    }

    private async void OnGoHomeClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}