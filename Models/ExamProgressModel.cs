namespace SrcSinavUygulamasi.Models
{
    /// <summary>
    /// Bir deneme sınavının TEK ve GÜNCEL sonucunu temsil eder.
    /// Aynı ExamId tekrar çözülürse ESKİ kayıt overwrite edilir.
    /// </summary>
    public class ExamProgressModel
    {
        public string CategoryId { get; set; } = "";  // src1, src2, src3, src4, src5
        public string ExamId { get; set; } = "";      // "deneme_1", "deneme_2", "real_exam", "image_exam"
        public DateTime CompletedDate { get; set; } = DateTime.UtcNow;
        public int TotalQuestionCount { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int BlankCount { get; set; }
        
        /// <summary>
        /// Tüm cevaplar: QuestionId -> Kullanıcının verdiği cevap (A, B, C, D veya "")
        /// </summary>
        public Dictionary<string, string> Answers { get; set; } = new();
        
        /// <summary>
        /// Sadece yanlış cevaplar: QuestionId -> Kullanıcının verdiği yanlış cevap
        /// </summary>
        public Dictionary<string, string> WrongAnswers { get; set; } = new();
        
        /// <summary>
        /// Başarı yüzdesi
        /// </summary>
        public double SuccessRate => TotalQuestionCount > 0 
            ? (double)CorrectCount / TotalQuestionCount * 100 
            : 0;
        
        /// <summary>
        /// 70 ve üzeri geçti mi?
        /// </summary>
        public bool IsPassed => SuccessRate >= 70;
    }
}
