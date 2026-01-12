namespace SrcSinavUygulamasi.Models
{
    /// <summary>
    /// Kullanıcı istatistikleri DTO'su - Analysis Dashboard için
    /// </summary>
    public class UserStatisticsModel
    {
        // ═══════════════════════════════════════════════════════════
        // GENEL İSTATİSTİKLER
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Toplam girilen sınav sayısı
        /// </summary>
        public int TotalExams { get; set; }
        
        /// <summary>
        /// Tüm sınavların ortalama puanı
        /// </summary>
        public double AverageScore { get; set; }
        
        /// <summary>
        /// Toplam çözülen soru sayısı
        /// </summary>
        public int TotalQuestionsSolved { get; set; }
        
        /// <summary>
        /// Geçilen sınav sayısı (70+)
        /// </summary>
        public int PassedExams { get; set; }
        
        /// <summary>
        /// Kalınan sınav sayısı (<70)
        /// </summary>
        public int FailedExams { get; set; }
        
        /// <summary>
        /// Başarı oranı (%)
        /// </summary>
        public double SuccessRate => TotalExams > 0 ? (double)PassedExams / TotalExams * 100 : 0;

        // ═══════════════════════════════════════════════════════════
        // ZAYIF KONULAR
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// En çok yanlış yapılan 3 konu ve başarı yüzdeleri
        /// </summary>
        public List<WeakSubjectModel> WeakestSubjects { get; set; } = new();

        // ═══════════════════════════════════════════════════════════
        // SON AKTİVİTELER
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Son 5 sınavın geçmişi
        /// </summary>
        public List<RecentExamModel> RecentExamHistory { get; set; } = new();
        
        /// <summary>
        /// Veri var mı kontrolü
        /// </summary>
        public bool HasData => TotalExams > 0;
    }

    /// <summary>
    /// Zayıf konu modeli
    /// </summary>
    public class WeakSubjectModel
    {
        public string SubjectName { get; set; } = "";
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestions => CorrectCount + WrongCount;
        public double SuccessRate => TotalQuestions > 0 ? (double)CorrectCount / TotalQuestions * 100 : 0;
        
        /// <summary>
        /// Durum rengi: Kırmızı (<50%), Turuncu (50-70%), Yeşil (70%+)
        /// </summary>
        public string StatusColor => SuccessRate < 50 ? "#ef4444" : (SuccessRate < 70 ? "#f59e0b" : "#22c55e");
        
        /// <summary>
        /// Durum ikonu
        /// </summary>
        public string StatusIcon => SuccessRate < 50 ? "🔴" : (SuccessRate < 70 ? "🟠" : "🟢");
    }

    /// <summary>
    /// Son sınav geçmişi modeli
    /// </summary>
    public class RecentExamModel
    {
        public string ExamName { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public DateTime Date { get; set; }
        public double Score { get; set; }
        public bool IsPassed => Score >= 70;
        
        /// <summary>
        /// Tarih formatı
        /// </summary>
        public string FormattedDate => Date.ToString("dd.MM.yyyy HH:mm");
        
        /// <summary>
        /// Durum metni
        /// </summary>
        public string StatusText => IsPassed ? "✓ Geçti" : "✗ Kaldı";
        
        /// <summary>
        /// Durum rengi
        /// </summary>
        public string StatusColor => IsPassed ? "#22c55e" : "#ef4444";
    }
}
