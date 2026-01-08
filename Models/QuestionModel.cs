namespace SrcSinavUygulamasi.Models
{
    public class QuestionModel
    {
        /// <summary>
        /// Benzersiz soru kimliği - JSON'dan gelir, stabil olmalı
        /// Örnek: "src4_deneme2_q17"
        /// </summary>
        public string Id { get; set; } = "";
        
        public string Soru { get; set; } = "";

        // KRİTİK NOKTA: Listeyi burada oluşturuyoruz (new List). 
        // Böylece veri gelmese bile liste "null" olmaz, uygulama çökmez.
        public List<string> Siklar { get; set; } = new List<string>() { "", "", "", "" };

        public string DogruCevap { get; set; } = "";
        
        // Resimli sorular için görsel yolu (örn: "src1_1.png")
        public string ResimYolu { get; set; } = "";
        
        // Görsel var mı kontrolü
        public bool HasImage => !string.IsNullOrEmpty(ResimYolu);
        
        /// <summary>
        /// Kullanıcının verdiği cevap (runtime only, persist edilmez)
        /// A, B, C, D veya "" (boş)
        /// </summary>
        public string UserAnswer { get; set; } = "";
        
        /// <summary>
        /// Kullanıcı doğru mu cevapladı (runtime hesaplama)
        /// </summary>
        public bool IsCorrect => !string.IsNullOrEmpty(UserAnswer) && UserAnswer == DogruCevap;
    }
}