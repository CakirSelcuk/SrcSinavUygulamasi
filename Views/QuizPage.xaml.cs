using SrcSinavUygulamasi.ViewModels;

namespace SrcSinavUygulamasi.Views;

[QueryProperty(nameof(KategoriId), "KategoriId")]
[QueryProperty(nameof(ExamIndex), "ExamIndex")]
[QueryProperty(nameof(TotalExams), "TotalExams")]
[QueryProperty(nameof(IsRealExam), "IsRealExam")]
[QueryProperty(nameof(IsImageExam), "IsImageExam")]
[QueryProperty(nameof(PointsPerQuestion), "PointsPerQuestion")]
public partial class QuizPage : ContentPage
{
    private QuizViewModel _viewModel;
    private string _kategoriId = "src3";
    private int _examIndex = 0;
    private int _totalExams = 1;
    private bool _isRealExam = false;
    private bool _isImageExam = false;
    private double _pointsPerQuestion = 5;

    public string KategoriId
    {
        set
        {
            _kategoriId = value ?? "src3";
            TryLoadExam();
        }
    }

    public string ExamIndex
    {
        set
        {
            if (int.TryParse(value, out int idx))
                _examIndex = idx;
            TryLoadExam();
        }
    }

    public string TotalExams
    {
        set
        {
            if (int.TryParse(value, out int total))
                _totalExams = total;
            TryLoadExam();
        }
    }

    public string IsRealExam
    {
        set
        {
            if (bool.TryParse(value, out bool isReal))
                _isRealExam = isReal;
            TryLoadExam();
        }
    }

    public string IsImageExam
    {
        set
        {
            if (bool.TryParse(value, out bool isImage))
                _isImageExam = isImage;
            TryLoadExam();
        }
    }

    public string PointsPerQuestion
    {
        set
        {
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out double pts))
                _pointsPerQuestion = pts;
            TryLoadExam();
        }
    }

    public QuizPage()
    {
        InitializeComponent();
        _viewModel = new QuizViewModel();
        BindingContext = _viewModel;
    }

    private void TryLoadExam()
    {
        // Tüm parametreler ayarlandığında yükle
        if (!string.IsNullOrEmpty(_kategoriId))
        {
            _viewModel.LoadExam(_kategoriId, _examIndex, _totalExams, _isRealExam, _isImageExam, _pointsPerQuestion);
        }
    }
}