using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Result Page - Sınav sonuç ekranı
/// Kurallar:
/// 1. "Yeni denemeye geç" sadece aynı SRC içinde sonraki DENEME varsa görünür
/// 2. "Sınav Sonucu Değerlendirme" tüm denemeler bitene kadar kilitli
/// 3. Fear Logic: Kritik durum frame'i ile kullanıcıyı uyar
/// 4. Premium: Yanlışları Çöz butonu için paywall
/// </summary>
public partial class ResultPage : ContentPage
{
    private QuizResultModel _result;
    private ExamProgressService _progressService = new();
    private ExamAnalysisService _analysisService = new();
    private PremiumService _premiumService = new();
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
            ShowFearLogicAnalysis();
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

        // Yanlışları Çöz butonu (yanlış varsa göster)
        if (_result.WrongCount > 0)
        {
            BtnSolveWrongs.IsVisible = true;
            BtnSolveWrongs.Text = _premiumService.IsUserPremium 
                ? $"Yanlışları Çöz ({_result.WrongCount})" 
                : $"🔒 Yanlışları Çöz ({_result.WrongCount})";
        }
    }

    /// <summary>
    /// Fear Logic: Kritik durum analizi ve gösterimi
    /// </summary>
    private void ShowFearLogicAnalysis()
    {
        int totalQuestions = _result.CorrectCount + _result.WrongCount + _result.EmptyCount;
        var (isCritical, message, color) = _analysisService.GetQuickAnalysis(_result.CorrectCount, totalQuestions);

        if (isCritical)
        {
            // Kritik Frame'i göster
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb(color);
            LblCriticalTitle.Text = "⛔ DURUM KRİTİK";
            LblCriticalMessage.Text = message.Replace("⛔ KALDINIZ!", "").Trim();
        }
        else if (_result.Score < 85)
        {
            // Uyarı Frame'i (turuncu)
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb("#FF6B00");
            CriticalFrame.Stroke = Color.FromArgb("#FF8C00");
            LblCriticalTitle.Text = "⚠️ RİSKLİ BÖLGE";
            LblCriticalMessage.Text = "Sınava hazır değilsiniz. Eksiklerinizi kapatın.";
            BtnDismissCritical.BackgroundColor = Colors.White;
            BtnDismissCritical.TextColor = Color.FromArgb("#FF6B00");
        }

        // Konu bazlı analiz göster (varsa)
        ShowSubjectScores();
    }

    /// <summary>
    /// Konu bazlı progress bar'ları göster
    /// </summary>
    private async void ShowSubjectScores()
    {
        try
        {
            // Sınav progress'i al
            var examProgress = _progressService.GetExamProgress(_result.CategoryId, _result.ExamId);
            if (examProgress == null || examProgress.Answers.Count == 0)
                return;

            // Analiz yap
            var analysisResult = await _analysisService.AnalyzeResults(_result.CategoryId, examProgress);
            
            if (analysisResult.SubjectScores.Count == 0)
                return;

            SubjectScoresFrame.IsVisible = true;
            SubjectProgressContainer.Children.Clear();

            foreach (var score in analysisResult.SubjectScores.OrderByDescending(s => s.TotalCount))
            {
                if (score.TotalCount == 0) continue;

                var progressRow = CreateSubjectProgressRow(score);
                SubjectProgressContainer.Children.Add(progressRow);
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Subject scores error: {ex.Message}");
#endif
        }
    }

    /// <summary>
    /// Tek bir konu için progress bar satırı oluştur
    /// </summary>
    private View CreateSubjectProgressRow(SubjectScoreModel score)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        // Konu adı
        var lblSubject = new Label
        {
            Text = $"{score.StatusIcon} {score.Subject}",
            TextColor = Colors.White,
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(lblSubject, 0);

        // Progress bar container
        var progressBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#334155"),
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            HeightRequest = 12,
            Padding = 0
        };

        // Progress bar fill
        var progressFill = new BoxView
        {
            BackgroundColor = Color.FromArgb(score.StatusColor),
            CornerRadius = 6,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = score.ProgressValue * 100 // Max 100 genişlik
        };

        progressBorder.Content = progressFill;
        Grid.SetColumn(progressBorder, 1);

        // Yüzde
        var lblPercentage = new Label
        {
            Text = $"%{score.Percentage:F0}",
            TextColor = Color.FromArgb(score.StatusColor),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(lblPercentage, 2);

        grid.Children.Add(lblSubject);
        grid.Children.Add(progressBorder);
        grid.Children.Add(lblPercentage);

        return grid;
    }

    private void CheckExamState()
    {
        var completedExamIds = _progressService.GetCompletedExamIds(_result.CategoryId);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"📋 ResultPage CheckExamState:");
        System.Diagnostics.Debug.WriteLine($"   CategoryId: {_result.CategoryId}");
        System.Diagnostics.Debug.WriteLine($"   ExamId: {_result.ExamId}");
        System.Diagnostics.Debug.WriteLine($"   Completed: [{string.Join(", ", completedExamIds)}]");
#endif

        _allPracticeExamsCompleted = ExamCatalog.AreAllPracticeExamsCompleted(
            _result.CategoryId, 
            completedExamIds);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"   All Practice Exams Completed: {_allPracticeExamsCompleted}");
#endif

        UpdateAnalysisButtonState();

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
            BtnAnalysis.IsEnabled = true;
            BtnAnalysis.BackgroundColor = Color.FromArgb("#2563eb");
            BtnAnalysis.Text = "📊 Sınav Analizimi Göster";
            BtnAnalysis.Opacity = 1.0;
        }
        else
        {
            BtnAnalysis.IsEnabled = true;
            BtnAnalysis.BackgroundColor = Color.FromArgb("#64748b");
            BtnAnalysis.Text = "📊 Sınav Sonucu Değerlendirme";
            BtnAnalysis.Opacity = 0.6;
        }
    }

    private void UpdateNextExamButtonState()
    {
        if (_result.IsRealExam || _result.IsImageExam)
        {
            return;
        }

        bool hasNextPracticeExam = !string.IsNullOrEmpty(_nextPracticeExamId);

        if (hasNextPracticeExam)
        {
            BtnNextExam.IsVisible = true;
            BtnNextExam.IsEnabled = true;
            BtnNextExam.Text = $"Yeni Denemeye Geç ▶";
            BtnNextExam.BackgroundColor = _result.IsPassed 
                ? Color.FromArgb("#22c55e")
                : Color.FromArgb("#3b82f6");
        }
        else
        {
            BtnNextExam.IsVisible = false;
        }
    }

    private void SetupSpecialExamUI(string displayTitle)
    {
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

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════

    private void OnDismissCriticalClicked(object sender, EventArgs e)
    {
        // Kritik uyarıyı kapat
        CriticalFrame.IsVisible = false;
    }

    private async void OnSolveWrongsClicked(object sender, EventArgs e)
    {
        // Premium kontrolü
        if (!_premiumService.IsUserPremium)
        {
            await _premiumService.ShowUpsellPopupAsync(PremiumService.Features.WRONG_ANSWERS);
            return;
        }

        // TODO: Yanlışları çöz sayfasına git
        await DisplayAlert("Yakında", "Yanlışları çöz özelliği yakında aktif olacak.", "Tamam");
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
            await DisplayAlert(
                "Değerlendirme Kullanılamıyor",
                "Değerlendirmeyi görmek için bu SRC setindeki tüm denemeleri tamamlamalısınız.",
                "Tamam");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(AnalysisPage)}",
            new Dictionary<string, object>
            {
                { "CategoryId", _result.CategoryId }
            });
    }

    private async void OnNextExamClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_nextPracticeExamId))
        {
            await Navigation.PopToRootAsync();
            return;
        }

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