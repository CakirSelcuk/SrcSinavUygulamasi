using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.Views;
using SrcSinavUygulamasi.Constants;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace SrcSinavUygulamasi.ViewModels
{
    public partial class QuizViewModel : ObservableObject
    {
        [ObservableProperty] private QuestionModel currentQuestion = new();
        [ObservableProperty] private double score;
        [ObservableProperty] private int questionNumber;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private double progressValue;
        [ObservableProperty] private bool isButtonsEnabled = true;
        [ObservableProperty] private Color themeColor;
        [ObservableProperty] private string examTitle = "Deneme Sınavı";

        // Timer properties
        [ObservableProperty] private string timerText = "45:00";
        [ObservableProperty] private bool isTimerVisible = false;
        [ObservableProperty] private Color timerColor = Colors.White;
        private System.Timers.Timer? _examTimer;
        private int _remainingSeconds = 0;
        private int _totalExamTimeMinutes = 0;

        private int _correctCount = 0;
        private int _wrongCount = 0;

        // Renkler
        private Color defaultColor = Color.FromArgb("#334155");
        [ObservableProperty] private Color btnAColor;
        [ObservableProperty] private Color btnBColor;
        [ObservableProperty] private Color btnCColor;
        [ObservableProperty] private Color btnDColor;

        // Resimli sorular için görsel
        [ObservableProperty] private ImageSource currentImageSource;
        [ObservableProperty] private bool hasCurrentImage;

        private List<QuestionModel> _allQuestions = new();
        private List<QuestionModel> _examQuestions = new();  // Bu denemenin soruları
        private int _currentIndex = 0;
        private string _categoryId = "";
        private string _categoryTitle = "Sınav";
        private int _examIndex = 0;
        private int _totalExams = 1;
        private bool _isRealExam = false;
        private bool _isImageExam = false;
        private bool _isMiniExam = false;
        private double _pointsPerQuestion = ExamRules.PointsPerQuestion;
        private List<string>? _specificQuestionIds = null;  // Mini sınav için spesifik soru ID'leri
        private List<string> _miniExamClearedIds = new();     // Mini sınavda doğru cevaplanan soru ID'leri
        private int _miniExamTotalWrongsBefore = 0;           // Mini sınav başlamadan önceki toplam yanlış sayısı

        private QuestionService _questionService = new QuestionService();
        private ExamProgressService _progressService = new ExamProgressService();
        private BalancedExamBuilder _examBuilder = new BalancedExamBuilder();
        private AdMobService? _adMobService;
        private string _examId = "";
        private Guid? _bankId = null;  // CourseSpecial için API bank ID

        private Dictionary<string, (string title, Color color)> _categoryInfo = new()
        {
            { "src1", ("SRC 1", Color.FromArgb("#FF5722")) },
            { "src2", ("SRC 2", Color.FromArgb("#2196F3")) },
            { "src3", ("SRC 3", Color.FromArgb("#4CAF50")) },
            { "src4", ("SRC 4", Color.FromArgb("#9C27B0")) },
            { "src5", ("SRC Kurye", Color.FromArgb("#00BCD4")) }
        };

        public QuizViewModel()
        {
            ResetColors();
            ThemeColor = Color.FromArgb("#0f172a");
            
            // DI'dan AdMobService'i al
            _adMobService = Application.Current?.Handler?.MauiContext?.Services
                .GetService<AdMobService>();
        }

        public async void LoadExam(string categoryId, int examIndex, int totalExams, bool isRealExam = false, bool isImageExam = false, double pointsPerQuestion = 5, bool isMiniExam = false, List<string>? specificQuestionIds = null)
        {
            _categoryId = categoryId?.ToLower() ?? "src3";
            _examIndex = examIndex;
            _totalExams = totalExams;
            _isRealExam = isRealExam;
            _isImageExam = false;
            _isMiniExam = isMiniExam;
            _pointsPerQuestion = ExamRules.PointsPerQuestion;
            _specificQuestionIds = specificQuestionIds;
            _correctCount = 0;
            _wrongCount = 0;
            Score = 0;

            SetCategoryTheme();
            
            if (_isMiniExam)
            {
                _examId = ExamCatalog.MINI_EXAM_ID;
                ExamTitle = ExamCatalog.GetDisplayTitle(_categoryId, _examId);
                ThemeColor = Color.FromArgb("#8B5CF6");  // Mor rengi
                
                // Mini sınav başlamadan önceki toplam yanlış sayısını kaydet
                _miniExamTotalWrongsBefore = _progressService.GetTotalWrongCount(_categoryId);
                _miniExamClearedIds.Clear();
            }
            else if (_isRealExam)
            {
                _examId = ExamCatalog.REAL_EXAM_ID;
                ExamTitle = ExamCatalog.GetDisplayTitle(_categoryId, _examId);
                ThemeColor = Color.FromArgb("#FFD700");  // Altın rengi
            }
            else if (_isImageExam)
            {
                _examId = ExamCatalog.IMAGE_EXAM_ID;
                ExamTitle = ExamCatalog.GetDisplayTitle(_categoryId, _examId);
                ThemeColor = Color.FromArgb("#E91E63");  // Pembe rengi
            }
            else
            {
                _examId = ExamCatalog.GetPracticeExamId(_examIndex);
                ExamTitle = ExamCatalog.GetDisplayTitle(_categoryId, _examId);
            }

            await LoadQuestionsForExam();
        }

        // Eski metod için uyumluluk
        public void LoadCategory(string categoryId)
        {
            LoadExam(categoryId, 0, 1, false, false, ExamRules.PointsPerQuestion);
        }

        /// <summary>
        /// API'den gelen soruları doğrudan yükle (CourseSpecial için).
        /// QuestionService kullanmaz, doğrudan QuestionModel listesi alır.
        /// </summary>
        public void LoadQuestionsDirectly(
            List<Models.QuestionModel> questions, 
            string categoryId, 
            string examId, 
            string examTitle,
            Guid? bankId = null)
        {
            if (questions == null || questions.Count == 0)
            {
                return;
            }

            _categoryId = categoryId?.ToLower() ?? "course";
            _examId = examId;
            _bankId = bankId;
            _examIndex = 0;
            _totalExams = 1;
            _isRealExam = false;
            _isImageExam = false;
            _isMiniExam = false;
            _pointsPerQuestion = ExamRules.PointsPerQuestion;
            _correctCount = 0;
            _wrongCount = 0;
            Score = 0;

            // Theme - CourseSpecial için yeşil
            ThemeColor = Color.FromArgb("#10b981");
            _categoryTitle = examTitle;
            ExamTitle = examTitle;

            // Soruları güncel e-Sınav formatına göre karıştır ve 40'a indir
            _allQuestions = questions;
            _examQuestions = questions
                .OrderBy(x => Guid.NewGuid())
                .Take(ExamRules.QuestionCount)
                .ToList();
            
            _currentIndex = 0;
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📝 LoadQuestionsDirectly: {_examQuestions.Count} questions, BankId={bankId}");
#endif
            
            ShowQuestion();
            StartExamTimer();
        }

        private void SetCategoryTheme()
        {
            if (_categoryInfo.TryGetValue(_categoryId, out var info))
            {
                ThemeColor = info.color;
                _categoryTitle = info.title;
            }
            else
            {
                ThemeColor = Color.FromArgb("#0f172a");
                _categoryTitle = "Sınav";
            }
        }

        private void ResetColors()
        {
            BtnAColor = defaultColor;
            BtnBColor = defaultColor;
            BtnCColor = defaultColor;
            BtnDColor = defaultColor;
        }

        private async Task LoadQuestionsForExam()
        {
            IsBusy = true;
            try
            {
                QuestionLoadResult loadResult;

                // Sınav tipine göre uygun soruları FAIL FAST validasyonu ile getir
                if (_isImageExam)
                {
                    loadResult = await _questionService.ResimliSorulariGetirWithValidation(_categoryId);
                }
                else if (_isRealExam)
                {
                    loadResult = await _questionService.SinavSorulariniGetirWithValidation(_categoryId);
                }
                else
                {
                    loadResult = await _questionService.SorulariGetirWithValidation(_categoryId);
                }

                // ═══════════════════════════════════════════════════════════
                // FAIL FAST: Veri geçersizse sınavı ENGELLE
                // ═══════════════════════════════════════════════════════════
                if (!loadResult.IsValid)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sınav Yüklenemedi",
                        loadResult.ErrorMessage,
                        "Tamam");
                    
                    // Ana menüye geri dön
                    await Application.Current.MainPage.Navigation.PopToRootAsync();
                    return;
                }

                var gelenSorular = loadResult.Questions;

                if (gelenSorular.Count > 0)
                {
                    _allQuestions = gelenSorular;
                    
                    if (_isRealExam || _isImageExam)
                    {
                        // e-Sınav simülasyonunda güncel formatta 40 soru kullan
                        _examQuestions = gelenSorular
                            .OrderBy(x => Guid.NewGuid())
                            .Take(ExamRules.QuestionCount)
                            .ToList();
                    }
                    else if (_isMiniExam)
                    {
                        // ═══════════════════════════════════════════════════════════
                        // MINI SINAV: Spesifik ID'ler verilmişse onları kullan
                        // Verilmemişse rastgele 15 soru seç
                        // ═══════════════════════════════════════════════════════════
                        if (_specificQuestionIds != null && _specificQuestionIds.Count > 0)
                        {
                            // Spesifik ID'lere göre filtrele
                            var specificIdSet = new HashSet<string>(_specificQuestionIds);
                            _examQuestions = gelenSorular
                                .Where(q => specificIdSet.Contains(q.Id))
                                .OrderBy(x => Guid.NewGuid())
                                .ToList();
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"📝 Mini sınav: {_specificQuestionIds.Count} ID verildi, {_examQuestions.Count} soru bulundu");
#endif
                        }
                        else
                        {
                            // Rastgele 15 soru
                            _examQuestions = gelenSorular
                                .OrderBy(x => Guid.NewGuid())
                                .Take(Math.Min(15, gelenSorular.Count))
                                .ToList();
                        }
                    }
                    else
                    {
                        // ═══════════════════════════════════════════════════════════
                        // ANA DENEME: BalancedExamBuilder ile 5A/5B/5C/5D seçimi
                        // Deterministik - aynı deneme = aynı sorular
                        // ═══════════════════════════════════════════════════════════
                        var balancedResult = _examBuilder.BuildExam(gelenSorular, _categoryId, _examIndex);
                        
                        if (!balancedResult.IsValid)
                        {
                            // FAIL FAST: Havuzda yeterli soru yok
                            await Application.Current.MainPage.DisplayAlert(
                                "Sınav Yüklenemedi",
                                balancedResult.ErrorMessage,
                                "Tamam");
                            
                            await Application.Current.MainPage.Navigation.PopToRootAsync();
                            return;
                        }
                        
                        _examQuestions = balancedResult.Questions;
                    }

                    // ═══════════════════════════════════════════════════════════
                    // FAIL FAST: Ana sınavlarda şık dağılımını doğrula
                    // (BalancedExamBuilder zaten dengeli dağılımı garanti eder ama double-check)
                    // Mini sınavlarda bu kural UYGULANMAZ
                    // ═══════════════════════════════════════════════════════════
                    if (_examQuestions.Count == ExamRules.QuestionCount && !_isMiniExam && !_isRealExam && !_isImageExam)
                    {
                        if (!_questionService.ValidateSikDagilimi(_examQuestions))
                        {
                            string errorMsg = _questionService.GetSikDagilimiHataMesaji(_examQuestions);
                            await Application.Current.MainPage.DisplayAlert(
                                "Sınav Yüklenemedi",
                                errorMsg,
                                "Tamam");
                            
                            await Application.Current.MainPage.Navigation.PopToRootAsync();
                            return;
                        }
                    }

                    _currentIndex = 0;
                    ShowQuestion();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sınav Yüklenemedi",
                        $"{_categoryTitle} soruları henüz yüklenmedi veya hatalı.",
                        "Tamam");
                    
                    await Application.Current.MainPage.Navigation.PopToRootAsync();
                    return;
                }

                // Timer başlat
                StartExamTimer();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void StartExamTimer()
        {
            _totalExamTimeMinutes = ExamRules.DurationMinutes;

            _remainingSeconds = _totalExamTimeMinutes * 60;
            IsTimerVisible = true;
            UpdateTimerDisplay();

            _examTimer?.Stop();
            _examTimer = new System.Timers.Timer(1000);
            _examTimer.Elapsed += OnTimerElapsed;
            _examTimer.AutoReset = true;
            _examTimer.Start();
        }

        private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _remainingSeconds--;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateTimerDisplay();

                if (_remainingSeconds <= 0)
                {
                    _examTimer?.Stop();
                    // Süre doldu, sınavı bitir
                    EndExamDueToTimeout();
                }
            });
        }

        private void UpdateTimerDisplay()
        {
            int minutes = _remainingSeconds / 60;
            int seconds = _remainingSeconds % 60;
            TimerText = $"{minutes:D2}:{seconds:D2}";

            // Son 5 dakikada kırmızı renk
            if (_remainingSeconds <= 300)
            {
                TimerColor = Colors.Red;
            }
            else if (_remainingSeconds <= 600)
            {
                TimerColor = Colors.Orange;
            }
            else
            {
                TimerColor = Colors.White;
            }
        }

        private async void EndExamDueToTimeout()
        {
            StopTimer();

            // Boş (cevaplanmamış) soru sayısını hesapla
            int emptyCount = _examQuestions.Count - _currentIndex;

            var resultModel = new QuizResultModel
            {
                Score = Score,
                CorrectCount = _correctCount,
                WrongCount = _wrongCount,
                EmptyCount = emptyCount,
                ThemeColor = ThemeColor,
                CategoryTitle = ExamTitle + " (Süre Doldu)",
                ExamIndex = _examIndex,
                TotalExams = _totalExams,
                CategoryId = _categoryId,
                ExamId = _examId,
                IsRealExam = _isRealExam,
                IsImageExam = _isImageExam,
                BankId = _bankId,
                // Mini sınav (Laundry) alanları
                IsMiniExam = _isMiniExam,
                TotalWrongsBefore = _miniExamTotalWrongsBefore,
                ClearedCount = _miniExamClearedIds.Count,
                RemainingWrongs = _miniExamTotalWrongsBefore - _miniExamClearedIds.Count,
                ClearedQuestionIds = new List<string>(_miniExamClearedIds)
            };
            
            // Interstitial reklam göster (premium değilse)
            await ShowInterstitialAndNavigateAsync(resultModel);
        }

        private void StopTimer()
        {
            _examTimer?.Stop();
            _examTimer?.Dispose();
            _examTimer = null;
            IsTimerVisible = false;
        }

        /// <summary>
        /// Sınav sonucunu Preferences'a kaydet
        /// </summary>
        private void SaveExamProgress()
        {
            try
            {
                // Tüm cevapları topla
                var allAnswers = new Dictionary<string, string>();
                var wrongAnswers = new Dictionary<string, string>();

                foreach (var q in _examQuestions)
                {
                    if (!string.IsNullOrEmpty(q.Id))
                    {
                        allAnswers[q.Id] = q.UserAnswer;
                        
                        // Yanlış cevapları ayır
                        if (!string.IsNullOrEmpty(q.UserAnswer) && q.UserAnswer != q.DogruCevap)
                        {
                            wrongAnswers[q.Id] = q.UserAnswer;
                        }
                    }
                }

                var progress = new ExamProgressModel
                {
                    CategoryId = _categoryId,
                    ExamId = _examId,
                    CompletedDate = DateTime.UtcNow,
                    TotalQuestionCount = _examQuestions.Count,
                    CorrectCount = _correctCount,
                    WrongCount = _wrongCount,
                    BlankCount = _examQuestions.Count - _correctCount - _wrongCount,
                    Answers = allAnswers,
                    WrongAnswers = wrongAnswers
                };

                _progressService.SaveExamProgress(progress);
                
                System.Diagnostics.Debug.WriteLine($"Sınav kaydedildi: {_categoryId}/{_examId} - Doğru: {_correctCount}, Yanlış: {_wrongCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sınav kaydetme hatası: {ex.Message}");
            }
        }

        private async void ShowQuestion()
        {
            if (_currentIndex < _examQuestions.Count)
            {
                IsButtonsEnabled = true;
                ResetColors();
                CurrentQuestion = _examQuestions[_currentIndex];
                QuestionNumber = _currentIndex + 1;
                ProgressValue = (double)QuestionNumber / _examQuestions.Count;

                // Resimli soru ise görseli yükle
                if (CurrentQuestion.HasImage)
                {
                    HasCurrentImage = true;
                    try
                    {
                        // MauiImage dosyaları Resources\Raw\Images klasöründen
                        // otomatik olarak küçük harfe dönüştürülür ve dosya adıyla erişilir
                        // Örn: src1_1.png -> "src1_1.png" veya "src1_1" 
                        string imagePath = CurrentQuestion.ResimYolu.ToLowerInvariant();
                        
                        // MauiImage dosyaları doğrudan dosya adıyla yüklenir
                        CurrentImageSource = ImageSource.FromFile(imagePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Görsel yükleme hatası: {ex.Message}");
                        HasCurrentImage = false;
                        CurrentImageSource = null;
                    }
                }
                else
                {
                    HasCurrentImage = false;
                    CurrentImageSource = null;
                }
            }
            else
            {
                // SINAV BİTTİ -> SONUÇ SAYFASINA GİT
                StopTimer();
                
                // Sınav sonucunu kaydet
                SaveExamProgress();
                
                var resultModel = new QuizResultModel
                {
                    Score = Score,
                    CorrectCount = _correctCount,
                    WrongCount = _wrongCount,
                    EmptyCount = 0,
                    ThemeColor = ThemeColor,
                    CategoryTitle = ExamTitle,
                    ExamIndex = _examIndex,
                    TotalExams = _totalExams,
                    CategoryId = _categoryId,
                    ExamId = _examId,
                    IsRealExam = _isRealExam,
                    IsImageExam = _isImageExam,
                    BankId = _bankId,
                    // Mini sınav (Laundry) alanları
                    IsMiniExam = _isMiniExam,
                    TotalWrongsBefore = _miniExamTotalWrongsBefore,
                    ClearedCount = _miniExamClearedIds.Count,
                    RemainingWrongs = _miniExamTotalWrongsBefore - _miniExamClearedIds.Count,
                    ClearedQuestionIds = new List<string>(_miniExamClearedIds)
                };
                
                // Interstitial reklam göster (premium değilse)
                await ShowInterstitialAndNavigateAsync(resultModel);
            }
        }

        [RelayCommand]
        private async Task Answer(string selectedOption)
        {
            if (!IsButtonsEnabled) return;
            IsButtonsEnabled = false;

            int selectedIndex = CurrentQuestion.Siklar.IndexOf(selectedOption);
            string dogruCevap = CurrentQuestion.DogruCevap;

            int correctIndex = 0;
            if (dogruCevap == "B") correctIndex = 1;
            else if (dogruCevap == "C") correctIndex = 2;
            else if (dogruCevap == "D") correctIndex = 3;

            Color targetColor = (selectedIndex == correctIndex) ? Colors.Green : Colors.Red;

            if (selectedIndex == 0) BtnAColor = targetColor;
            else if (selectedIndex == 1) BtnBColor = targetColor;
            else if (selectedIndex == 2) BtnCColor = targetColor;
            else if (selectedIndex == 3) BtnDColor = targetColor;

            // Kullanıcının cevabını kaydet
            string userAnswer = selectedIndex switch
            {
                0 => "A",
                1 => "B",
                2 => "C",
                3 => "D",
                _ => ""
            };
            CurrentQuestion.UserAnswer = userAnswer;
            
            if (selectedIndex != correctIndex)
            {
                if (correctIndex == 0) BtnAColor = Colors.Green;
                else if (correctIndex == 1) BtnBColor = Colors.Green;
                else if (correctIndex == 2) BtnCColor = Colors.Green;
                else if (correctIndex == 3) BtnDColor = Colors.Green;
                _wrongCount++;
                
                // Yanlış cevabı progress service'e kaydet
                _progressService.MarkQuestionWrong(_categoryId, CurrentQuestion.Id);
            }
            else
            {
                _correctCount++;
                // Puan hesaplama: deneme = 5 puan/soru, gerçek sınav = 2.5 puan/soru
                Score += _pointsPerQuestion;
                
                // Doğru cevabı progress service'e kaydet
                _progressService.MarkQuestionCorrect(_categoryId, CurrentQuestion.Id);
                
                // Mini sınavda doğru cevaplanan soruyu izle (laundry için)
                if (_isMiniExam && !string.IsNullOrEmpty(CurrentQuestion.Id))
                {
                    _miniExamClearedIds.Add(CurrentQuestion.Id);
                }
            }

            await Task.Delay(1200);
            _currentIndex++;
            ShowQuestion();
        }

        /// <summary>
        /// Interstitial reklam göster ve ResultPage'e git.
        /// Premium kullanıcılar için direkt navigate eder.
        /// </summary>
        private async Task ShowInterstitialAndNavigateAsync(QuizResultModel resultModel)
        {
            try
            {
                if (_adMobService != null)
                {
                    // Interstitial reklamı önceden yükle
                    await _adMobService.LoadInterstitialAdAsync();
                    
                    // Reklamı göster (premium değilse)
                    // Premium ise veya reklam yüklenemezse direkt navigate eder
                    await _adMobService.ShowInterstitialAdAsync();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Interstitial ad error: {ex.Message}");
#endif
            }
            finally
            {
                // Reklam gösterilsin veya gösterilmesin, sonuç sayfasına git
                await Application.Current.MainPage.Navigation.PushAsync(new ResultPage(resultModel));
            }
        }
    }
}
