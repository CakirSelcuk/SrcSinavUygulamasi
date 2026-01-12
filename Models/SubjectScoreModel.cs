namespace SrcSinavUygulamasi.Models
{
    /// <summary>
    /// Konu bazlı başarı skoru modeli
    /// Fear Logic UI için kullanılır
    /// </summary>
    public class SubjectScoreModel
    {
        public string Subject { get; set; } = "";
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalCount => CorrectCount + WrongCount;
        
        /// <summary>
        /// Başarı yüzdesi (0-100)
        /// </summary>
        public double Percentage => TotalCount > 0 
            ? Math.Round((double)CorrectCount / TotalCount * 100, 1) 
            : 0;
        
        /// <summary>
        /// Fear Logic renk kodu
        /// Kırmızı < 50%, Turuncu 50-70%, Yeşil > 70%
        /// </summary>
        public string StatusColor => Percentage switch
        {
            < 50 => "#B00020",   // Kan kırmızısı - KRİTİK
            < 70 => "#FF6B00",   // Turuncu - RİSKLİ
            _ => "#22c55e"       // Yeşil - GÜVENLI
        };
        
        /// <summary>
        /// Progress bar için normalize edilmiş değer (0.0 - 1.0)
        /// </summary>
        public double ProgressValue => Percentage / 100.0;
        
        /// <summary>
        /// Durum ikonu
        /// </summary>
        public string StatusIcon => Percentage switch
        {
            < 50 => "⚠️",
            < 70 => "⚡",
            _ => "✓"
        };
    }
}
