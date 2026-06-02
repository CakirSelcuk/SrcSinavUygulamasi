using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.Constants;
using Microsoft.Maui.Controls.Shapes;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Sınav Sonuç Sayfası
/// Business Rules:
/// 1. Laundry Mode (Mini Sınav): Puan gizle, temizlenen yanlış göster
/// 2. Fear Logic: Geçme puanı altı = Kırmızı, &lt; 85 = Turuncu tema
/// 3. Subject Analysis: Konu bazlı başarı analizi
/// 4. Premium Check: Yanlışları Çöz butonu için paywall
/// </summary>
public partial class ResultPage : ContentPage
{
    // ═══════════════════════════════════════════════════════════
    // RENK SABİTLERİ
    // ═══════════════════════════════════════════════════════════
    private const string COLOR_CRITICAL = "#B00020";      // Kan kırmızısı
    private const string COLOR_HEADER_FAIL = "#450a0a";   // Koyu kırmızı
    private const string COLOR_HEADER_WARNING = "#78350f"; // Koyu turuncu
    private const string COLOR_HEADER_NORMAL = "#334155"; // Normal mavi-gri
    private const string COLOR_HEADER_MINI = "#581c87";   // Mor (mini sınav)
    private const string COLOR_SUCCESS = "#22c55e";       // Yeşil
    private const string COLOR_WARNING = "#f59e0b";       // Turuncu
    private const string COLOR_DANGER = "#ef4444";        // Kırmızı

    // ═══════════════════════════════════════════════════════════
    // ALANLAR
    // ═══════════════════════════════════════════════════════════
    private QuizResultModel? _result;
    private readonly ExamProgressService _progressService = new();
    private readonly QuestionService _questionService = new();
    private readonly PremiumService _premiumService = new();
    private bool _allPracticeExamsCompleted = false;
    private string? _nextPracticeExamId = null;

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════
    public ResultPage(QuizResultModel result)
    {
        InitializeComponent();
        _result = result;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (_result != null)
        {
            LoadData();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 1. ANA VERİ YÜKLEME
    // ═══════════════════════════════════════════════════════════
    private async void LoadData()
    {
        if (_result == null) return;

        // İstatistikleri güncelle
        LblCorrect.Text = _result.CorrectCount.ToString();
        LblWrong.Text = _result.WrongCount.ToString();
        LblEmpty.Text = _result.EmptyCount.ToString();

        // ═══════════════════════════════════════════════════════
        // LAUNDRY MODE (Mini Sınav)
        // ═══════════════════════════════════════════════════════
        if (_result.IsMiniExam)
        {
            ApplyLaundryMode();
        }
        else
        {
            // ═══════════════════════════════════════════════════
            // NORMAL SINAV MODU
            // ═══════════════════════════════════════════════════
            ApplyNormalExamMode();
            
            // Subject Analysis yükle (sadece normal sınavlarda)
            await LoadSubjectAnalysis();
        }

        // Exam state kontrol (sonraki deneme butonu için)
        CheckExamState();

        // Premium buton durumunu ayarla
        UpdatePremiumButtonState();

    }

    // ═══════════════════════════════════════════════════════════
    // 2. LAUNDRY MODE (Mini Sınav)
    // ═══════════════════════════════════════════════════════════
    private void ApplyLaundryMode()
    {
        if (_result == null) return;

        // Puan yerine temizlenen yanlış sayısını göster
        ScoreBorder.IsVisible = false;
        ClearedBorder.IsVisible = true;
        LblClearedCount.Text = $"{_result.ClearedCount}/{_result.TotalWrongsBefore}";

        // Doğru cevaplanan soruları WrongAnswers'dan temizle (LAUNDRY)
        if (_result.ClearedQuestionIds.Count > 0)
        {
            _progressService.ClearCorrectAnswersFromWrongList(
                _result.CategoryId, 
                _result.ClearedQuestionIds);
        }

        // Kalan yanlış sayısını kontrol et
        int remainingWrongs = _progressService.GetTotalWrongCount(_result.CategoryId);

        // Header rengi mor
        HeaderBox.Color = Color.FromArgb(COLOR_HEADER_MINI);

        // Critical frame gizle
        CriticalFrame.IsVisible = false;

        if (remainingWrongs == 0)
        {
            // ════════════════════════════════════════════════════
            // TÜM YANLIŞ TEMİZLENDİ!
            // ════════════════════════════════════════════════════
            LblMessage.Text = "🎉 Mükemmel!";
            LblMessage.TextColor = Color.FromArgb(COLOR_SUCCESS);
            LblSubMessage.Text = "Tüm eksiklerini kapattın!\nArtık bu konularda hatasızsın.";
            
            BtnSolveWrongs.IsVisible = false;
            BtnRetry.IsVisible = false;
        }
        else
        {
            // ════════════════════════════════════════════════════
            // HALA YANLIŞ VAR
            // ════════════════════════════════════════════════════
            LblMessage.Text = $"Kalan: {remainingWrongs} Yanlış";
            LblMessage.TextColor = Color.FromArgb(COLOR_WARNING);
            LblSubMessage.Text = "Hala eksiklerin var.\nTamamlamak için tekrarla.";
            
            // Tekrar dene butonunu göster (premium kontrol yok)
            BtnSolveWrongs.IsVisible = true;
            BtnSolveWrongs.Text = $"🔄 Kalan {remainingWrongs} Yanlışı Tekrar Dene";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb(COLOR_WARNING);
            
            BtnRetry.IsVisible = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 3. NORMAL SINAV MODU + FEAR LOGIC
    // ═══════════════════════════════════════════════════════════
    private void ApplyNormalExamMode()
    {
        if (_result == null) return;

        double score = _result.Score;

        // Puan göster
        ScoreBorder.IsVisible = true;
        ClearedBorder.IsVisible = false;
        LblScore.Text = score.ToString("F0");

        // ═══════════════════════════════════════════════════════
        // FEAR LOGIC: Puan bazlı tema uygulama
        // ═══════════════════════════════════════════════════════
        if (score < ExamRules.PassScore)
        {
            // ════════════════════════════════════════════════════
            // BAŞARISIZ
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb(COLOR_CRITICAL);
            LblCriticalTitle.Text = "⛔ BAŞARISIZ OLDUN!";
            LblCriticalMessage.Text = $"Bugün sınav olsaydı KALIRDIN!\n{ExamRules.PassScore} puan barajını geçemedin. En az {ExamRules.MinimumCorrectToPass} doğru gerekiyor.";

            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_FAIL);
            
            LblMessage.Text = "Maalesef Kaldınız";
            LblMessage.TextColor = Color.FromArgb(COLOR_DANGER);
            LblSubMessage.Text = $"{ExamRules.PassScore} puan barajını geçemediniz.\nDaha fazla çalışmanız gerekiyor.";
            LblScore.TextColor = Color.FromArgb(COLOR_DANGER);
            
            BtnRetry.Text = "🔄 Sınavı Tekrarla";
            BtnRetry.BackgroundColor = Color.FromArgb(COLOR_WARNING);
        }
        else if (score < 85)
        {
            // ════════════════════════════════════════════════════
            // RİSKLİ BÖLGE
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb(COLOR_WARNING);
            CriticalFrame.Stroke = Color.FromArgb("#FF8C00");
            LblCriticalTitle.Text = "⚠️ RİSKLİ BÖLGE";
            LblCriticalMessage.Text = "Geçtin ama güvende değilsin!\nSınavda stres altında bu puanı koruman zor olabilir.";

            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_WARNING);
            
            LblMessage.Text = "Geçtiniz Ama...";
            LblMessage.TextColor = Color.FromArgb(COLOR_WARNING);
            LblSubMessage.Text = "Riskli bölgedesiniz.\nDaha fazla pratik yapmanız önerilir.";
            LblScore.TextColor = Color.FromArgb(COLOR_WARNING);
        }
        else
        {
            // ════════════════════════════════════════════════════
            // BAŞARILI (85+)
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = false;
            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_NORMAL);
            
            LblMessage.Text = "Tebrikler! 🎉";
            LblMessage.TextColor = Color.FromArgb(COLOR_SUCCESS);
            LblSubMessage.Text = "Harika bir performans!\nSınava hazırsın.";
            LblScore.TextColor = Color.FromArgb(COLOR_SUCCESS);
        }

        // Yanlış varsa "Yanlışları Çöz" butonunu göster
        if (_result.WrongCount > 0)
        {
            BtnSolveWrongs.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 4. SUBJECT ANALYSIS (Konu Bazlı Analiz)
    // ═══════════════════════════════════════════════════════════
    private async Task LoadSubjectAnalysis()
    {
        if (_result == null) return;

        try
        {
            var examProgress = _progressService.GetExamProgress(_result.CategoryId, _result.ExamId);
            if (examProgress == null || examProgress.Answers.Count == 0)
                return;

            var allQuestions = await _questionService.SorulariGetir(_result.CategoryId);
            if (allQuestions == null || allQuestions.Count == 0)
                return;

            // Soruları otomatik etiketle
            SubjectTaggerService.AutoTagQuestions(allQuestions);

            var questionDict = allQuestions.ToDictionary(q => q.Id, q => q);
            var subjectStats = new Dictionary<string, (int Correct, int Wrong)>();

            foreach (var answer in examProgress.Answers)
            {
                if (!questionDict.TryGetValue(answer.Key, out var question))
                    continue;

                var subject = question.Subject ?? "Genel";
                
                if (!subjectStats.ContainsKey(subject))
                    subjectStats[subject] = (0, 0);

                var current = subjectStats[subject];
                bool isCorrect = !string.IsNullOrEmpty(answer.Value) && 
                                 answer.Value == question.DogruCevap;

                subjectStats[subject] = isCorrect 
                    ? (current.Correct + 1, current.Wrong) 
                    : (current.Correct, current.Wrong + 1);
            }

            if (subjectStats.Count == 0)
                return;

            // UI'ı güncelle
            SubjectScoresFrame.IsVisible = true;
            SubjectProgressContainer.Children.Clear();

            var sortedStats = subjectStats
                .Where(s => s.Value.Correct + s.Value.Wrong > 0)
                .OrderByDescending(s => s.Value.Correct + s.Value.Wrong)
                .ToList();

            foreach (var stat in sortedStats)
            {
                int total = stat.Value.Correct + stat.Value.Wrong;
                double percentage = total > 0 ? (double)stat.Value.Correct / total * 100 : 0;

                var progressRow = CreateSubjectProgressRow(stat.Key, percentage, stat.Value.Correct, total);
                SubjectProgressContainer.Children.Add(progressRow);
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📊 Subject analysis error: {ex.Message}");
#endif
        }
    }

    private View CreateSubjectProgressRow(string subject, double percentage, int correct, int total)
    {
        string colorHex;
        string statusIcon;

        if (percentage < 50)
        {
            colorHex = COLOR_DANGER;
            statusIcon = "🔴";
        }
        else if (percentage < ExamRules.PassScore)
        {
            colorHex = COLOR_WARNING;
            statusIcon = "🟠";
        }
        else
        {
            colorHex = COLOR_SUCCESS;
            statusIcon = "🟢";
        }

        var container = new VerticalStackLayout { Spacing = 6 };

        // Başlık satırı
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        headerGrid.Children.Add(new Label
        {
            Text = $"{statusIcon} {subject}",
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        });

        var statsLabel = new Label
        {
            Text = $"{correct}/{total} (%{percentage:F0})",
            TextColor = Color.FromArgb(colorHex),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };
        Grid.SetColumn(statsLabel, 1);
        headerGrid.Children.Add(statsLabel);

        container.Children.Add(headerGrid);

        // Progress bar
        var progressBg = new Border
        {
            BackgroundColor = Color.FromArgb("#374151"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            HeightRequest = 8,
            Stroke = Colors.Transparent
        };

        var progressFill = new Border
        {
            BackgroundColor = Color.FromArgb(colorHex),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            HeightRequest = 8,
            WidthRequest = 0,
            HorizontalOptions = LayoutOptions.Start,
            Stroke = Colors.Transparent
        };

        var progressGrid = new Grid();
        progressGrid.Children.Add(progressBg);
        progressGrid.Children.Add(progressFill);
        container.Children.Add(progressGrid);

        // Animasyonlu genişleme
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);
            double maxWidth = 280;
            double targetWidth = maxWidth * (percentage / 100);
            progressFill.WidthRequest = targetWidth;
        });

        return container;
    }

    // ═══════════════════════════════════════════════════════════
    // 5. PREMIUM CHECK & PAYWALL
    // ═══════════════════════════════════════════════════════════
    private void UpdatePremiumButtonState()
    {
        if (_result == null) return;

        // Mini sınavda premium kontrolü yapma
        if (_result.IsMiniExam) return;

        bool isPremium = _premiumService.IsUserPremium;
        
        if (isPremium)
        {
            BtnSolveWrongs.Text = $"✅ Yanlışları Çöz ({_result.WrongCount})";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb("#22c55e");
        }
        else
        {
            BtnSolveWrongs.Text = $"🔒 Yanlışları Çöz ({_result.WrongCount})";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb("#9333ea");
        }
    }

    private async void OnSolveWrongsClicked(object sender, EventArgs e)
    {
        if (_result == null) return;

        // Mini sınav sonucu ise direkt tekrar navigasyon yap
        if (_result.IsMiniExam)
        {
            await NavigateToMiniExam();
            return;
        }

        // Normal sınav - Premium kontrolü
        bool isPremium = _premiumService.IsUserPremium;

        if (isPremium)
        {
            await NavigateToMiniExam();
        }
        else
        {
            await _premiumService.ShowUpsellPopupAsync(PremiumService.Features.WRONG_ANSWERS);
        }
    }

    private async Task NavigateToMiniExam()
    {
        if (_result == null) return;

        // Yanlış soru ID'lerini al
        var wrongQuestionIds = _progressService.BuildMiniExamQuestionIds(_result.CategoryId, ExamRules.QuestionCount);

        if (wrongQuestionIds.Count == 0)
        {
            await DisplayAlert("Bilgi", "Çözülecek yanlış soru bulunamadı.", "Tamam");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(QuizPage)}",
            new Dictionary<string, object>
            {
                { "KategoriId", _result.CategoryId },
                { "ExamIndex", "0" },
                { "TotalExams", "1" },
                { "IsRealExam", "False" },
                { "IsImageExam", "False" },
                { "PointsPerQuestion", ExamRules.PointsPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "IsMiniExam", "True" },
                { "MiniExamQuestionIds", string.Join(",", wrongQuestionIds) }
            });
    }

    // ═══════════════════════════════════════════════════════════
    // 6. EXAM STATE & NAVIGATION
    // ═══════════════════════════════════════════════════════════
    private void CheckExamState()
    {
        if (_result == null) return;

        var completedExamIds = _progressService.GetCompletedExamIds(_result.CategoryId);

        _allPracticeExamsCompleted = ExamCatalog.AreAllPracticeExamsCompleted(
            _result.CategoryId,
            completedExamIds);

        // Sonraki denemeyi bul
        if (!_result.IsRealExam && !_result.IsImageExam && !_result.IsMiniExam)
        {
            _nextPracticeExamId = ExamCatalog.GetNextPracticeExamId(
                _result.CategoryId,
                _result.ExamId);
        }

        UpdateNextExamButtonState();
    }

    private void UpdateNextExamButtonState()
    {
        if (_result == null) return;

        if (_result.IsRealExam || _result.IsImageExam || _result.IsMiniExam)
        {
            BtnNextExam.IsVisible = false;
            return;
        }

        bool hasNextPracticeExam = !string.IsNullOrEmpty(_nextPracticeExamId);

        if (hasNextPracticeExam)
        {
            BtnNextExam.IsVisible = true;
            BtnNextExam.Text = "Sonraki Denemeye Geç ▶";
            BtnNextExam.BackgroundColor = _result.IsPassed
                ? Color.FromArgb(COLOR_SUCCESS)
                : Color.FromArgb("#3b82f6");
        }
        else
        {
            BtnNextExam.IsVisible = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 7. EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════
    private void OnDismissCriticalClicked(object? sender, EventArgs e)
    {
        CriticalFrame.IsVisible = false;
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        if (_result == null) return;

        double pointsPerQuestion = ExamRules.PointsPerQuestion;

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

    private async void OnNextExamClicked(object? sender, EventArgs e)
    {
        if (_result == null) return;

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
                { "PointsPerQuestion", ExamRules.PointsPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
    }

    private async void OnGoHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}
