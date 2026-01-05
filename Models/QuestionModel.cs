namespace SrcSinavUygulamasi.Models
{
    public class QuestionModel
    {
        public string Soru { get; set; } = "";

        // KRİTİK NOKTA: Listeyi burada oluşturuyoruz (new List). 
        // Böylece veri gelmese bile liste "null" olmaz, uygulama çökmez.
        public List<string> Siklar { get; set; } = new List<string>() { "", "", "", "" };

        public string DogruCevap { get; set; } = "";
        
        // Resimli sorular için görsel yolu (örn: "src1_1.png")
        public string ResimYolu { get; set; } = "";
        
        // Görsel var mı kontrolü
        public bool HasImage => !string.IsNullOrEmpty(ResimYolu);
    }
}