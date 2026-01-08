namespace SrcSinavUygulamasi.Models
{
    /// <summary>
    /// Soru bazlı öğrenme ve tekrar kontrolü için kullanılır.
    /// Mini sınavlarda 3 kez üst üste doğru yapılan soru "öğrenildi" sayılır.
    /// </summary>
    public class QuestionProgressModel
    {
        public string QuestionId { get; set; } = "";
        
        /// <summary>
        /// Bu soru toplam kaç kez yanlış yapıldı
        /// </summary>
        public int TotalWrongCount { get; set; }
        
        /// <summary>
        /// Üst üste doğru yapma sayısı (yanlış yapılınca sıfırlanır)
        /// </summary>
        public int CorrectStreakCount { get; set; }
        
        /// <summary>
        /// 3 kez üst üste doğru = öğrenildi
        /// </summary>
        public bool IsLearned => CorrectStreakCount >= 3;
        
        /// <summary>
        /// Kullanıcı bu soruyu mini sınavlardan gizledi mi
        /// </summary>
        public bool IsHiddenForMiniExams { get; set; }
        
        /// <summary>
        /// Son doğru cevaplama tarihi
        /// </summary>
        public DateTime? LastCorrectDate { get; set; }
        
        /// <summary>
        /// Son yanlış cevaplama tarihi
        /// </summary>
        public DateTime? LastWrongDate { get; set; }
    }
}
