using Microsoft.Maui.Graphics;

namespace SrcSinavUygulamasi.Models
{
    public class QuizResultModel
    {
        public double Score { get; set; }        // Puan
        public int CorrectCount { get; set; }    // Doğru Sayısı
        public int WrongCount { get; set; }      // Yanlış Sayısı
        public int EmptyCount { get; set; }      // Boş Sayısı
        public Color ThemeColor { get; set; } = Colors.Gray;   // Sınavın Rengi
        public string CategoryTitle { get; set; } = ""; // Hangi Sınavdı?
        
        // Deneme Sınavı için yeni alanlar
        public int ExamIndex { get; set; }       // Şu anki deneme indexi (0, 1, 2...)
        public int TotalExams { get; set; }      // Toplam deneme sayısı
        public string CategoryId { get; set; } = "";   // Kategori ID ("src3")
        public bool IsPassed => Score >= 70;     // 70+ geçti mi?
        public bool IsRealExam { get; set; }     // Gerçek sınav simülasyonu mu?
    }
}
