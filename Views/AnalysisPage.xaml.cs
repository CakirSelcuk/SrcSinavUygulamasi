using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SrcSinavUygulamasi.Views;

/// <summary>
/// Analiz Dashboard Sayfası
/// Kullanıcının performans istatistiklerini gösteren "Kokpit"
/// </summary>
[QueryProperty(nameof(CategoryId), "CategoryId")]
public partial class AnalysisPage : ContentPage
{
    private ExamProgressService _progressService = new();
    private string? _categoryId;
    
    public string? CategoryId
    {
        get => _categoryId;
        set
        {
            _categoryId = value;
            if (!string.IsNullOrEmpty(value))
            {
                LblCategoryTitle.Text = $"SRC {value.Replace("src", "").ToUpper()}";
            }
        }
    }

    public AnalysisPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStatistics();
    }

    // ═══════════════════════════════════════════════════════════
    // VERİ YÜKLEME
    // ═══════════════════════════════════════════════════════════
    private void LoadStatistics()
    {
        UserStatisticsModel stats;
        
        if (!string.IsNullOrEmpty(_categoryId))
        {
            stats = _progressService.GetCategoryStatistics(_categoryId);
        }
        else
        {
            stats = _progressService.GetUserStatistics();
        }

        if (!stats.HasData)
        {
            ShowEmptyState();
            return;
        }

        ShowMainContent(stats);
    }

    // ═══════════════════════════════════════════════════════════
    // EMPTY STATE
    // ═══════════════════════════════════════════════════════════
    private void ShowEmptyState()
    {
        EmptyStateFrame.IsVisible = true;
        MainContent.IsVisible = false;
    }

    // ═══════════════════════════════════════════════════════════
    // ANA İÇERİK
    // ═══════════════════════════════════════════════════════════
    private void ShowMainContent(UserStatisticsModel stats)
    {
        EmptyStateFrame.IsVisible = false;
        MainContent.IsVisible = true;

        // Özet kartları
        LblAverageScore.Text = stats.AverageScore.ToString("F0");
        LblTotalQuestions.Text = stats.TotalQuestionsSolved.ToString();
        LblTotalExams.Text = stats.TotalExams.ToString();

        // Başarı oranı rengi
        if (stats.AverageScore >= 70)
            LblAverageScore.TextColor = Color.FromArgb("#22c55e");
        else if (stats.AverageScore >= 50)
            LblAverageScore.TextColor = Color.FromArgb("#f59e0b");
        else
            LblAverageScore.TextColor = Color.FromArgb("#ef4444");

        // Başarı oranı
        LblSuccessRate.Text = $"%{stats.SuccessRate:F0}";
        LblPassedCount.Text = $"{stats.PassedExams} Geçti";
        LblFailedCount.Text = $"{stats.FailedExams} Kaldı";

        // Başarı oranı bar genişliği (animasyonlu)
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);
            double maxWidth = 280;
            double targetWidth = maxWidth * (stats.SuccessRate / 100);
            SuccessRateBar.WidthRequest = targetWidth;
            
            if (stats.SuccessRate >= 70)
                SuccessRateBar.BackgroundColor = Color.FromArgb("#22c55e");
            else if (stats.SuccessRate >= 50)
                SuccessRateBar.BackgroundColor = Color.FromArgb("#f59e0b");
            else
                SuccessRateBar.BackgroundColor = Color.FromArgb("#ef4444");
        });

        // Bar chart oluştur
        CreateBarChart(stats.RecentExamHistory);

        // Zayıf konular
        CreateWeakSubjects(stats.WeakestSubjects);

        // Son aktiviteler
        CreateRecentExams(stats.RecentExamHistory);
    }

    // ═══════════════════════════════════════════════════════════
    // NATIVE BAR CHART
    // ═══════════════════════════════════════════════════════════
    private void CreateBarChart(List<RecentExamModel> exams)
    {
        BarChartContainer.Children.Clear();

        if (exams.Count == 0)
        {
            BarChartContainer.Children.Add(new Label
            {
                Text = "Henüz sınav verisi yok",
                TextColor = Color.FromArgb("#94a3b8"),
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
            Grid.SetColumnSpan((Label)BarChartContainer.Children[0], 5);
            return;
        }

        // Son 5 sınavı al (en eski -> en yeni sırada)
        var chartExams = exams.Take(5).Reverse().ToList();
        
        for (int i = 0; i < chartExams.Count; i++)
        {
            var exam = chartExams[i];
            var barContainer = CreateBarColumn(exam, i);
            Grid.SetColumn(barContainer, i);
            BarChartContainer.Children.Add(barContainer);
        }

        // Eksik sütunları doldur
        for (int i = chartExams.Count; i < 5; i++)
        {
            var emptyBar = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(emptyBar, i);
            BarChartContainer.Children.Add(emptyBar);
        }
    }

    private View CreateBarColumn(RecentExamModel exam, int index)
    {
        var container = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 4
        };

        // Puan etiketi
        container.Children.Add(new Label
        {
            Text = $"{exam.Score:F0}",
            TextColor = Colors.White,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        });

        // Bar yüksekliği (max 100 puan = 80 piksel)
        double maxHeight = 80;
        double barHeight = Math.Max(10, maxHeight * (exam.Score / 100));

        // Bar rengi
        string barColor = exam.Score >= 70 ? "#22c55e" : (exam.Score >= 50 ? "#f59e0b" : "#ef4444");

        // Bar
        var bar = new Border
        {
            BackgroundColor = Color.FromArgb(barColor),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4, 4, 0, 0) },
            Stroke = Colors.Transparent,
            HeightRequest = 0, // Animasyon için başlangıç
            WidthRequest = 36,
            HorizontalOptions = LayoutOptions.Center
        };

        container.Children.Add(bar);

        // 70 puan çizgisi (referans)
        if (exam.Score >= 70)
        {
            container.Children.Add(new BoxView
            {
                Color = Color.FromArgb("#f59e0b"),
                HeightRequest = 2,
                WidthRequest = 36,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, -2, 0, 0)
            });
        }

        // Tarih etiketi
        container.Children.Add(new Label
        {
            Text = exam.Date.ToString("HH:mm"),
            TextColor = Color.FromArgb("#94a3b8"),
            FontSize = 9,
            HorizontalOptions = LayoutOptions.Center
        });

        // Animasyonlu bar yüksekliği
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100 + (index * 100));
            bar.HeightRequest = barHeight;
        });

        return container;
    }

    // ═══════════════════════════════════════════════════════════
    // ZAYIF KONULAR
    // ═══════════════════════════════════════════════════════════
    private void CreateWeakSubjects(List<WeakSubjectModel> weakSubjects)
    {
        WeakSubjectsContainer.Children.Clear();

        if (weakSubjects.Count == 0)
        {
            WeakSubjectsFrame.IsVisible = false;
            return;
        }

        WeakSubjectsFrame.IsVisible = true;

        foreach (var subject in weakSubjects)
        {
            var row = CreateWeakSubjectRow(subject);
            WeakSubjectsContainer.Children.Add(row);
        }
    }

    private View CreateWeakSubjectRow(WeakSubjectModel subject)
    {
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
            Text = $"{subject.StatusIcon} {subject.SubjectName}",
            TextColor = Colors.White,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold
        });

        var statsLabel = new Label
        {
            Text = $"{subject.CorrectCount}/{subject.TotalQuestions} (%{subject.SuccessRate:F0})",
            TextColor = Color.FromArgb(subject.StatusColor),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold
        };
        Grid.SetColumn(statsLabel, 1);
        headerGrid.Children.Add(statsLabel);

        container.Children.Add(headerGrid);

        // Progress bar
        var progressGrid = new Grid { HeightRequest = 8 };
        
        progressGrid.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#374151"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Stroke = Colors.Transparent
        });

        var progressFill = new Border
        {
            BackgroundColor = Color.FromArgb(subject.StatusColor),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Stroke = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = 0
        };
        progressGrid.Children.Add(progressFill);

        container.Children.Add(progressGrid);

        // Animasyon
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(150);
            double maxWidth = 270;
            progressFill.WidthRequest = maxWidth * (subject.SuccessRate / 100);
        });

        return container;
    }

    // ═══════════════════════════════════════════════════════════
    // SON AKTİVİTELER
    // ═══════════════════════════════════════════════════════════
    private void CreateRecentExams(List<RecentExamModel> exams)
    {
        RecentExamsContainer.Children.Clear();

        if (exams.Count == 0)
        {
            RecentExamsFrame.IsVisible = false;
            return;
        }

        RecentExamsFrame.IsVisible = true;

        foreach (var exam in exams)
        {
            var row = CreateRecentExamRow(exam);
            RecentExamsContainer.Children.Add(row);
        }
    }

    private View CreateRecentExamRow(RecentExamModel exam)
    {
        var container = new Border
        {
            BackgroundColor = Color.FromArgb("#374151"),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(14, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };

        // Sınav adı ve tarih
        var infoStack = new VerticalStackLayout { Spacing = 2 };
        infoStack.Children.Add(new Label
        {
            Text = exam.ExamName,
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        });
        infoStack.Children.Add(new Label
        {
            Text = exam.FormattedDate,
            TextColor = Color.FromArgb("#94a3b8"),
            FontSize = 10
        });
        grid.Children.Add(infoStack);

        // Puan
        var scoreLabel = new Label
        {
            Text = $"{exam.Score:F0}",
            TextColor = Color.FromArgb(exam.StatusColor),
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(scoreLabel, 1);
        grid.Children.Add(scoreLabel);

        // Durum
        var statusLabel = new Label
        {
            Text = exam.StatusText,
            TextColor = Color.FromArgb(exam.StatusColor),
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(statusLabel, 2);
        grid.Children.Add(statusLabel);

        container.Content = grid;
        return container;
    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnStartExamClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}
