using SrcSinavUygulamasi.Constants;

namespace SrcSinavUygulamasi.Models
{
    public class ExamModel
    {
        public int Id { get; set; }              // Deneme numarası (1, 2, 3...)
        public string Title { get; set; }        // "SRC3 - Deneme 1"
        public int QuestionCount { get; set; }   // Bu denemede kaç soru (güncel formatta 40)
        public string CategoryId { get; set; }   // "src3"
        public int StartIndex { get; set; }      // Soru listesindeki başlangıç indexi
        public string Color { get; set; }        // Kategori rengi
        
        // Özel görsel alanı
        public string IconText { get; set; }     // Sol taraftaki metin (numara veya "S", "R" gibi)
        
        // Sınav tipi özellikleri
        public bool IsRealExam { get; set; }     // Gerçek sınav simülasyonu mu?
        public bool IsImageExam { get; set; }    // Resimli sorular mı?
        public double PointsPerQuestion { get; set; } = ExamRules.PointsPerQuestion;  // Her soru kaç puan
        public string Subtitle { get; set; }     // Alt başlık
    }
}
