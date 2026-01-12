using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Result Page - Sınav sonuç ekranı
/// ═══════════════════════════════════════════════════════════════════════
/// BUSINESS RULES:
/// 1. CriticalFrame EN ÜSTTE - kullanıcı puanı görmeden önce "KALDIN" görmeli
/// 2. % < 70 = Kritik (Kırmızı), % < 85 = Riskli (Turuncu), >= 85 = Başarılı
/// 3. Yanlışları Çöz = Premium (Paywall ile korumalı)
/// 4. Subject bazlı progress bar'lar dinamik olarak oluşturulur
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
public partial class ResultPage : ContentPage
{
    // ═══════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════
    private const string COLOR_CRITICAL = "#B00020";      // Kan kırmızısı
    private const string COLOR_HEADER_FAIL = "#450a0a";   // Dark red header
    private const string COLOR_HEADER_NORMAL = "#334155"; // Normal blue-gray
    private const string COLOR_SUCCESS = "#22c55e";       // Yeşil
    private const string COLOR_WARNING = "#f59e0b";       // Turuncu
    private const string COLOR_DANGER = "#ef4444";        // Kırmızı
    
    private const string PREMIUM_BACKDOOR_CODE = "SRC2024PREMIUM";
    private const string PREF_KEY_PREMIUM = "IsUserPremium";

    // ═══════════════════════════════════════════════════════════
    // FIELDS
    // ═══════════════════════════════════════════════════════════
    private QuizResultModel? _result;
    private readonly ExamProgressService _progressService = new();
    private readonly QuestionService _questionService = new();
    private bool _allPracticeExamsCompleted = false;
    private string? _nextPracticeExamId = null;

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTORS
    // ═══════════════════════════════════════════════════════════
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
            LoadData();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 1. DATA LOADING & SCORE CALCULATION
    // ═══════════════════════════════════════════════════════════
    private async void LoadData()
    {
        if (_result == null) return;

        // Calculate score (CorrectCount * 2.5 for 40 questions = 100 max)
        // But we use the Score from result which is already calculated
        double score = _result.Score;
        int correctCount = _result.CorrectCount;
        int wrongCount = _result.WrongCount;
        int emptyCount = _result.EmptyCount;

        // Update UI labels
        LblScore.Text = score.ToString("F0");
        LblCorrect.Text = correctCount.ToString();
        LblWrong.Text = wrongCount.ToString();
        LblEmpty.Text = emptyCount.ToString();

        // ═══════════════════════════════════════════════════════
        // FEAR LOGIC: Critical State Detection
        // ═══════════════════════════════════════════════════════
        ApplyFearLogic(score);

        // Check exam state for navigation buttons
        CheckExamState();

        // Update Premium button state
        UpdatePremiumButtonState();

        // Load subject analysis (async)
        await LoadSubjectAnalysis();
    }

    /// <summary>
    /// Fear Logic: Apply psychological pressure based on score
    /// </summary>
    private void ApplyFearLogic(double score)
    {
        if (score < 70)
        {
            // ════════════════════════════════════════════════════
            // CRITICAL STATE: User FAILED
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb(COLOR_CRITICAL);
            LblCriticalTitle.Text = "⛔ BAŞARISIZ OLDUN!";
            LblCriticalMessage.Text = "Bugün sınav olsaydı KALIRDIN!\n70 puan barajını geçemedin. Eksiklerini acilen kapat.";

            // Change header to dark red
            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_FAIL);

            // Update message labels
            LblMessage.Text = "Maalesef Kaldınız";
            LblMessage.TextColor = Color.FromArgb(COLOR_DANGER);
            LblSubMessage.Text = "70 puan barajını geçemediniz.\nDaha fazla çalışmanız gerekiyor.";
            LblScore.TextColor = Color.FromArgb(COLOR_DANGER);

            // Show retry button
            BtnRetry.IsVisible = true;
            BtnRetry.Text = "Sınavı Tekrarla 🔄";
        }
        else if (score < 85)
        {
            // ════════════════════════════════════════════════════
            // WARNING STATE: User passed but risky
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = true;
            CriticalFrame.BackgroundColor = Color.FromArgb(COLOR_WARNING);
            CriticalFrame.Stroke = Color.FromArgb("#FF8C00");
            LblCriticalTitle.Text = "⚠️ RİSKLİ BÖLGE";
            LblCriticalMessage.Text = "Geçtin ama sınırdasın!\nEksiklerini kapatmazsan gerçek sınavda tehlike var.";
            BtnDismissCritical.BackgroundColor = Colors.White;
            BtnDismissCritical.TextColor = Color.FromArgb(COLOR_WARNING);

            // Normal header
            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_NORMAL);

            // Update message labels
            LblMessage.Text = "Geçtiniz!";
            LblMessage.TextColor = Color.FromArgb(COLOR_WARNING);
            LblSubMessage.Text = $"Puanınız: {score:F0}\nEksiklerinizi kapatın.";
            LblScore.TextColor = Color.FromArgb(COLOR_WARNING);

            // Hide retry
            BtnRetry.IsVisible = false;
        }
        else
        {
            // ════════════════════════════════════════════════════
            // SUCCESS STATE: User is ready
            // ════════════════════════════════════════════════════
            CriticalFrame.IsVisible = false;

            // Normal header
            HeaderBox.Color = Color.FromArgb(COLOR_HEADER_NORMAL);

            // Update message labels
            LblMessage.Text = "Tebrikler! 🎉";
            LblMessage.TextColor = Color.FromArgb(COLOR_SUCCESS);
            LblSubMessage.Text = "Sınava hazırsınız!\nBu performansı koruyun.";
            LblScore.TextColor = Color.FromArgb(COLOR_SUCCESS);

            // Hide retry
            BtnRetry.IsVisible = false;
        }

        // Show Solve Wrongs button if there are wrong answers
        if (_result != null && _result.WrongCount > 0)
        {
            BtnSolveWrongs.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 2. DYNAMIC SUBJECT ANALYSIS (THE HARD PART)
    // ═══════════════════════════════════════════════════════════
    private async Task LoadSubjectAnalysis()
    {
        if (_result == null) return;

        try
        {
            // Get exam progress (contains answers)
            var examProgress = _progressService.GetExamProgress(_result.CategoryId, _result.ExamId);
            if (examProgress == null || examProgress.Answers.Count == 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("📊 No exam progress found for subject analysis");
#endif
                return;
            }

            // Fetch ALL questions from QuestionService
            var allQuestions = await _questionService.SorulariGetir(_result.CategoryId);
            if (allQuestions == null || allQuestions.Count == 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("📊 No questions found for subject analysis");
#endif
                return;
            }

            // Auto-tag questions if Subject is missing
            SubjectTaggerService.AutoTagQuestions(allQuestions);

            // Create question dictionary for quick lookup
            var questionDict = allQuestions.ToDictionary(q => q.Id, q => q);

            // Group answers by Subject and calculate success per subject
            var subjectStats = new Dictionary<string, (int Correct, int Wrong)>();

            foreach (var answer in examProgress.Answers)
            {
                if (!questionDict.TryGetValue(answer.Key, out var question))
                    continue;

                var subject = question.Subject ?? "Genel";

                if (!subjectStats.ContainsKey(subject))
                    subjectStats[subject] = (0, 0);

                var current = subjectStats[subject];
                bool isCorrect = answer.Value == question.DogruCevap;

                if (isCorrect)
                    subjectStats[subject] = (current.Correct + 1, current.Wrong);
                else
                    subjectStats[subject] = (current.Correct, current.Wrong + 1);
            }

            // ════════════════════════════════════════════════════
            // DYNAMIC UI GENERATION
            // ════════════════════════════════════════════════════
            if (subjectStats.Count == 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("📊 No subject stats to display");
#endif
                return;
            }

            SubjectScoresFrame.IsVisible = true;
            SubjectProgressContainer.Children.Clear();

            // Sort by total count (descending)
            var sortedStats = subjectStats
                .Where(s => s.Value.Correct + s.Value.Wrong > 0)
                .OrderByDescending(s => s.Value.Correct + s.Value.Wrong)
                .ToList();

            foreach (var stat in sortedStats)
            {
                int total = stat.Value.Correct + stat.Value.Wrong;
                double percentage = total > 0 ? (double)stat.Value.Correct / total * 100 : 0;

                // Create progress row
                var progressRow = CreateSubjectProgressRow(stat.Key, percentage, stat.Value.Correct, total);
                SubjectProgressContainer.Children.Add(progressRow);
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📊 Subject analysis displayed: {sortedStats.Count} subjects");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📊 Subject analysis error: {ex.Message}");
#endif
        }
    }

    /// <summary>
    /// Create a single subject progress row with label and progress bar
    /// </summary>
    private View CreateSubjectProgressRow(string subject, double percentage, int correct, int total)
    {
        // Determine color based on percentage
        string colorHex;
        string statusIcon;

        if (percentage < 50)
        {
            colorHex = COLOR_DANGER;  // Red
            statusIcon = "🔴";
        }
        else if (percentage < 70)
        {
            colorHex = COLOR_WARNING; // Orange
            statusIcon = "🟠";
        }
        else
        {
            colorHex = COLOR_SUCCESS; // Green
            statusIcon = "🟢";
        }

        var color = Color.FromArgb(colorHex);

        // Create main container
        var container = new VerticalStackLayout
        {
            Spacing = 4
        };

        // Row 1: Subject name and percentage
        var labelRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var subjectLabel = new Label
        {
            Text = $"{statusIcon} {subject}",
            TextColor = Colors.White,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(subjectLabel, 0);

        var percentLabel = new Label
        {
            Text = $"{correct}/{total} (%{percentage:F0})",
            TextColor = color,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        Grid.SetColumn(percentLabel, 1);

        labelRow.Children.Add(subjectLabel);
        labelRow.Children.Add(percentLabel);

        // Row 2: Progress bar
        var progressBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#334155"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            HeightRequest = 8,
            Padding = 0
        };

        var progressFill = new BoxView
        {
            BackgroundColor = color,
            CornerRadius = 4,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = Math.Max(5, percentage * 2) // Scale: 100% = 200px
        };

        progressBorder.Content = progressFill;

        container.Children.Add(labelRow);
        container.Children.Add(progressBorder);

        return container;
    }

    // ═══════════════════════════════════════════════════════════
    // 3. PAYWALL LOGIC (PREMIUM CHECK)
    // ═══════════════════════════════════════════════════════════
    private void UpdatePremiumButtonState()
    {
        bool isPremium = Preferences.Get(PREF_KEY_PREMIUM, false);

        if (isPremium)
        {
            BtnSolveWrongs.Text = $"✅ Yanlışları Çöz ({_result?.WrongCount ?? 0})";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb("#22c55e"); // Green
        }
        else
        {
            BtnSolveWrongs.Text = $"🔒 Yanlışları Çöz ({_result?.WrongCount ?? 0})";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb("#9333ea"); // Purple
        }
    }

    private async void OnSolveWrongsClicked(object sender, EventArgs e)
    {
        if (_result == null) return;

        bool isPremium = Preferences.Get(PREF_KEY_PREMIUM, false);

        if (isPremium)
        {
            // ════════════════════════════════════════════════════════
            // PREMIUM USER: Yanlışları Çöz - Mini Sınav navigasyonu
            // ════════════════════════════════════════════════════════
            // Bu sınavdaki yanlış soru ID'lerini al
            var examProgress = _progressService.GetExamProgress(_result.CategoryId, _result.ExamId);
            if (examProgress == null || examProgress.WrongAnswers.Count == 0)
            {
                await DisplayAlert("Bilgi", "Bu sınavda yanlış cevabınız bulunmuyor.", "Tamam");
                return;
            }

            // Yanlış soru ID'lerini virgülle ayrılmış string olarak hazırla
            var wrongQuestionIds = string.Join(",", examProgress.WrongAnswers.Keys);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📝 Yanlışları Çöz navigasyonu:");
            System.Diagnostics.Debug.WriteLine($"   CategoryId: {_result.CategoryId}");
            System.Diagnostics.Debug.WriteLine($"   Wrong IDs: {wrongQuestionIds}");
#endif

            // Mini sınav sayfasına git
            await Shell.Current.GoToAsync($"{nameof(QuizPage)}",
                new Dictionary<string, object>
                {
                    { "KategoriId", _result.CategoryId },
                    { "ExamIndex", "0" },
                    { "TotalExams", "1" },
                    { "IsRealExam", "False" },
                    { "IsImageExam", "False" },
                    { "PointsPerQuestion", "6.67" },
                    { "IsMiniExam", "True" },
                    { "MiniExamQuestionIds", wrongQuestionIds }
                });
        }
        else
        {
            // ════════════════════════════════════════════════════════
            // FREE USER: Show paywall
            // ════════════════════════════════════════════════════════
            bool wantsToBuy = await DisplayAlert(
                "🔒 Kilitli Özellik",
                "Yanlışlarını çözmek ve sınavı GARANTİLEMEK için VIP ol!\n\n" +
                "✅ Yanlış cevaplarını tekrar çöz\n" +
                "✅ Konu bazlı detaylı analiz\n" +
                "✅ Reklamsız deneyim\n\n" +
                "Fiyat: ₺49.99 (Ömür boyu)",
                "Satın Al",
                "Vazgeç");

            if (wantsToBuy)
            {
                await HandlePurchase();
            }
        }
    }

    private async Task HandlePurchase()
    {
        // Ask for activation code (backdoor for support)
        string? code = await DisplayPromptAsync(
            "Aktivasyon Kodu",
            "Satın alma kodunuz varsa girin.\n(Destek ekibinden aldıysanız)",
            "Aktifleştir",
            "İptal",
            placeholder: "Kod girin...",
            maxLength: 20);

        if (string.IsNullOrWhiteSpace(code))
        {
            // No code entered - simulate store redirect
            await DisplayAlert("Mağaza",
                "Uygulama içi satın alma yakında aktif olacak.\n\nDestek için: srcsinav.destek@gmail.com",
                "Tamam");
            return;
        }

        // ════════════════════════════════════════════════════
        // BACKDOOR CHECK (For support purposes)
        // ════════════════════════════════════════════════════
        if (code.Trim().ToUpperInvariant() == PREMIUM_BACKDOOR_CODE)
        {
            // Activate premium
            Preferences.Set(PREF_KEY_PREMIUM, true);
            Preferences.Set("PremiumActivationDate", DateTime.UtcNow.ToString("o"));

            // Update button
            BtnSolveWrongs.Text = $"✅ Yanlışları Çöz ({_result?.WrongCount ?? 0})";
            BtnSolveWrongs.BackgroundColor = Color.FromArgb("#22c55e");

            await DisplayAlert("🎉 Başarılı!",
                "VIP üyeliğiniz aktifleştirildi!\n\nArtık tüm premium özelliklere erişebilirsiniz.",
                "Harika!");
        }
        else
        {
            await DisplayAlert("Geçersiz Kod",
                "Girdiğiniz kod geçerli değil.\n\nLütfen destek ekibinden doğru kodu alın.",
                "Tamam");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 4. EXAM STATE & NAVIGATION
    // ═══════════════════════════════════════════════════════════
    private void CheckExamState()
    {
        if (_result == null) return;

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

        // Get next practice exam
        if (!_result.IsRealExam && !_result.IsImageExam)
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

        if (_result.IsRealExam || _result.IsImageExam)
        {
            BtnNextExam.IsVisible = false;
            return;
        }

        bool hasNextPracticeExam = !string.IsNullOrEmpty(_nextPracticeExamId);

        if (hasNextPracticeExam)
        {
            BtnNextExam.IsVisible = true;
            BtnNextExam.IsEnabled = true;
            BtnNextExam.Text = "Yeni Denemeye Geç ▶";
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
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Dismiss critical warning frame
    /// </summary>
    private void OnDismissCriticalClicked(object? sender, EventArgs e)
    {
        CriticalFrame.IsVisible = false;
    }

    /// <summary>
    /// Retry the same exam
    /// </summary>
    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        if (_result == null) return;

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

    /// <summary>
    /// Show analysis page
    /// </summary>
    private async void OnAnalysisClicked(object? sender, EventArgs e)
    {
        if (!_allPracticeExamsCompleted)
        {
            await DisplayAlert(
                "Değerlendirme Kullanılamıyor",
                "Tüm deneme sınavlarını tamamlamadan genel değerlendirme yapamazsınız.\n\n" +
                "Önce bu kategorideki tüm denemeleri bitirin.",
                "Tamam");
            return;
        }

        if (_result == null) return;

        await Shell.Current.GoToAsync($"{nameof(AnalysisPage)}",
            new Dictionary<string, object>
            {
                { "CategoryId", _result.CategoryId }
            });
    }

    /// <summary>
    /// Navigate to next practice exam
    /// </summary>
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
                { "PointsPerQuestion", "5" }
            });
    }

    /// <summary>
    /// Navigate to home
    /// </summary>
    private async void OnGoHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}