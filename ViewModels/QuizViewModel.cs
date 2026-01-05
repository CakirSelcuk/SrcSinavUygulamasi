using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using SrcSinavUygulamasi.Views;
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
        private double _pointsPerQuestion = 5;

        private QuestionService _questionService = new QuestionService();

        private Dictionary<string, (string title, Color color)> _categoryInfo = new()
        {
            { "src1", ("SRC 1", Color.FromArgb("#FF5722")) },
            { "src2", ("SRC 2", Color.FromArgb("#2196F3")) },
            { "src3", ("SRC 3", Color.FromArgb("#4CAF50")) },
            { "src4", ("SRC 4", Color.FromArgb("#9C27B0")) }
        };

        public QuizViewModel()
        {
            ResetColors();
            ThemeColor = Color.FromArgb("#0f172a");
        }

        public async void LoadExam(string categoryId, int examIndex, int totalExams, bool isRealExam = false, bool isImageExam = false, double pointsPerQuestion = 5)
        {
            _categoryId = categoryId?.ToLower() ?? "src3";
            _examIndex = examIndex;
            _totalExams = totalExams;
            _isRealExam = isRealExam;
            _isImageExam = isImageExam;
            _pointsPerQuestion = pointsPerQuestion;
            _correctCount = 0;
            _wrongCount = 0;
            Score = 0;

            SetCategoryTheme();
            
            if (_isRealExam)
            {
                ExamTitle = $"{_categoryTitle} - Gerçek Sınav";
                ThemeColor = Color.FromArgb("#FFD700");  // Altın rengi
            }
            else if (_isImageExam)
            {
                ExamTitle = $"{_categoryTitle} - Resimli Sorular";
                ThemeColor = Color.FromArgb("#E91E63");  // Pembe rengi
            }
            else
            {
                ExamTitle = $"{_categoryTitle} - Deneme {_examIndex + 1}";
            }

            await LoadQuestionsForExam();
        }

        // Eski metod için uyumluluk
        public void LoadCategory(string categoryId)
        {
            LoadExam(categoryId, 0, 1, false, false, 5);
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
                List<QuestionModel> gelenSorular;

                // Sınav tipine göre uygun soruları getir
                if (_isImageExam)
                {
                    gelenSorular = await _questionService.ResimliSorulariGetir(_categoryId);
                }
                else if (_isRealExam)
                {
                    gelenSorular = await _questionService.SinavSorulariniGetir(_categoryId);
                }
                else
                {
                    gelenSorular = await _questionService.SorulariGetir(_categoryId);
                }

                if (gelenSorular.Count > 0)
                {
                    _allQuestions = gelenSorular;
                    
                    if (_isRealExam || _isImageExam)
                    {
                        // Gerçek sınav veya resimli sorularda tüm soruları karıştırarak al
                        _examQuestions = gelenSorular
                            .OrderBy(x => Guid.NewGuid())
                            .ToList();
                    }
                    else
                    {
                        // Deneme sınavında 20'şerli gruplara böl
                        int questionsPerExam = 20;
                        int startIndex = _examIndex * questionsPerExam;
                        int count = Math.Min(questionsPerExam, gelenSorular.Count - startIndex);

                        if (startIndex < gelenSorular.Count)
                        {
                            _examQuestions = gelenSorular
                                .Skip(startIndex)
                                .Take(count)
                                .OrderBy(x => Guid.NewGuid())
                                .ToList();
                        }
                        else
                        {
                            _examQuestions = new List<QuestionModel>();
                        }
                    }

                    _currentIndex = 0;
                    ShowQuestion();
                }
                else
                {
                    string dosyaTipi = _isImageExam ? "resimli" : (_isRealExam ? "sinav" : "pratik");
                    _examQuestions = new List<QuestionModel>
                    {
                        new QuestionModel
                        {
                            Soru = $"{_categoryTitle} {dosyaTipi} soruları henüz yüklenmedi.",
                            Siklar = new List<string> { "Tamam", "-", "-", "-" },
                            DogruCevap = "A"
                        }
                    };
                    _currentIndex = 0;
                    ShowQuestion();
                }
            }
            finally
            {
                IsBusy = false;
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
                    IsRealExam = _isRealExam || _isImageExam  // Resimli de özel sınav gibi davransın
                };
                await Application.Current.MainPage.Navigation.PushAsync(new ResultPage(resultModel));
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

            if (selectedIndex != correctIndex)
            {
                if (correctIndex == 0) BtnAColor = Colors.Green;
                else if (correctIndex == 1) BtnBColor = Colors.Green;
                else if (correctIndex == 2) BtnCColor = Colors.Green;
                else if (correctIndex == 3) BtnDColor = Colors.Green;
                _wrongCount++;
            }
            else
            {
                _correctCount++;
                // Puan hesaplama: deneme = 5 puan/soru, gerçek sınav = 2.5 puan/soru
                Score += _pointsPerQuestion;
            }

            await Task.Delay(1200);
            _currentIndex++;
            ShowQuestion();
        }
    }
}