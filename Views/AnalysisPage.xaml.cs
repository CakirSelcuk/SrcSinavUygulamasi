using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SrcSinavUygulamasi.Views;

[QueryProperty(nameof(CategoryId), "CategoryId")]
public partial class AnalysisPage : ContentPage
{
    private string _categoryId = "";
    private ExamProgressService _progressService = new();
    private QuestionService _questionService = new();
    private List<string> _wrongQuestionIds = new();

    public string CategoryId
    {
        get => _categoryId;
        set
        {
            _categoryId = value;
            LoadAnalysis();
        }
    }

    public AnalysisPage()
    {
        InitializeComponent();
    }

    private async void LoadAnalysis()
    {
        if (string.IsNullOrEmpty(_categoryId)) return;

        // Kategori başlığı
        var categoryTitles = new Dictionary<string, string>
        {
            { "src1", "SRC 1 - Uluslararası Yolcu" },
            { "src2", "SRC 2 - Yurtiçi Yolcu" },
            { "src3", "SRC 3 - Uluslararası Eşya" },
            { "src4", "SRC 4 - Yurtiçi Eşya" },
            { "src5", "SRC 5 - Tehlikeli Madde (ADR)" }
        };

        CategoryLabel.Text = categoryTitles.TryGetValue(_categoryId, out var title) ? title : _categoryId.ToUpper();

        // Hazırlık durumu
        var readiness = _progressService.GetReadinessStatus(_categoryId);
        ReadinessLabel.Text = readiness.Label;
        ReadinessLabel.TextColor = Color.FromArgb(readiness.Color);

        // Tüm yanlış soruları getir
        var wrongQuestions = _progressService.GetAllWrongQuestionsUnique(_categoryId);

        if (wrongQuestions.Count == 0)
        {
            // Hiç yanlış yok - tebrik mesajı göster
            CongratsBorder.IsVisible = true;
            WrongQuestionsScroll.IsVisible = false;
            MiniExamButton.IsVisible = false;
            return;
        }

        // Yanlış sorular var
        CongratsBorder.IsVisible = false;
        WrongQuestionsScroll.IsVisible = true;
        MiniExamButton.IsVisible = true;

        // Soruları yükle (pratik + sınav + resimli)
        var allQuestions = new List<QuestionModel>();
        allQuestions.AddRange(await _questionService.SorulariGetir(_categoryId));
        allQuestions.AddRange(await _questionService.SinavSorulariniGetir(_categoryId));
        allQuestions.AddRange(await _questionService.ResimliSorulariGetir(_categoryId));

        // Yanlış soruları listele
        WrongQuestionsStack.Children.Clear();
        _wrongQuestionIds.Clear();

        int index = 1;
        foreach (var wrongEntry in wrongQuestions)
        {
            var questionId = wrongEntry.Key;
            var userAnswer = wrongEntry.Value;

            // Soruyu bul
            var question = allQuestions.FirstOrDefault(q => q.Id == questionId);
            if (question == null) continue;

            _wrongQuestionIds.Add(questionId);

            // Soru kartı oluştur
            var card = CreateQuestionCard(index, question, userAnswer);
            WrongQuestionsStack.Children.Add(card);
            index++;
        }

        // Yanlış sayısı bilgisi
        var infoLabel = new Label
        {
            Text = $"Toplam {wrongQuestions.Count} yanlış sorunuz bulunmaktadır.",
            TextColor = Color.FromArgb("#94a3b8"),
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        WrongQuestionsStack.Children.Insert(0, infoLabel);
    }

    private Border CreateQuestionCard(int index, QuestionModel question, string userAnswer)
    {
        var card = new Border
        {
            Stroke = Colors.Transparent,
            BackgroundColor = Color.FromArgb("#1e293b"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(15)
        };

        var stack = new VerticalStackLayout { Spacing = 10 };

        // Soru numarası ve metni
        stack.Children.Add(new Label
        {
            Text = $"Soru {index}",
            TextColor = Color.FromArgb("#ef4444"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        });

        stack.Children.Add(new Label
        {
            Text = question.Soru,
            TextColor = Colors.White,
            FontSize = 14,
            LineHeight = 1.3
        });

        // Kullanıcının cevabı
        var userAnswerIndex = userAnswer switch
        {
            "A" => 0,
            "B" => 1,
            "C" => 2,
            "D" => 3,
            _ => -1
        };

        if (userAnswerIndex >= 0 && userAnswerIndex < question.Siklar.Count)
        {
            stack.Children.Add(new Label
            {
                Text = $"Sizin Cevabınız: {userAnswer}) {question.Siklar[userAnswerIndex]}",
                TextColor = Color.FromArgb("#ef4444"),
                FontSize = 13
            });
        }

        // Doğru cevap
        var correctIndex = question.DogruCevap switch
        {
            "A" => 0,
            "B" => 1,
            "C" => 2,
            "D" => 3,
            _ => -1
        };

        if (correctIndex >= 0 && correctIndex < question.Siklar.Count)
        {
            stack.Children.Add(new Label
            {
                Text = $"Doğru Cevap: {question.DogruCevap}) {question.Siklar[correctIndex]}",
                TextColor = Color.FromArgb("#22c55e"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });
        }

        card.Content = stack;
        return card;
    }

    private async void OnMiniExamClicked(object sender, EventArgs e)
    {
        // Mini sınav için soru ID'lerini al
        var miniExamQuestionIds = _progressService.BuildMiniExamQuestionIds(_categoryId, 15);

        if (miniExamQuestionIds.Count == 0)
        {
            await DisplayAlert("Bilgi", "Mini sınav için uygun soru bulunamadı.", "Tamam");
            return;
        }

        // Mini sınav sayfasına git
        await Shell.Current.GoToAsync($"{nameof(QuizPage)}",
            new Dictionary<string, object>
            {
                { "KategoriId", _categoryId },
                { "ExamIndex", "0" },
                { "TotalExams", "1" },
                { "IsRealExam", "False" },
                { "IsImageExam", "False" },
                { "PointsPerQuestion", "6.67" },
                { "IsMiniExam", "True" },
                { "MiniExamQuestionIds", string.Join(",", miniExamQuestionIds) }
            });
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CategoriesPage");
    }
}
