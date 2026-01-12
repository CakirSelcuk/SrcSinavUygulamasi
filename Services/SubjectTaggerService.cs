using SrcSinavUygulamasi.Models;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Keyword bazlı otomatik konu ataması.
    /// Bu servis hem runtime'da (yeni JSON'lar için) hem de
    /// migration tool'da kullanılabilir.
    /// </summary>
    public static class SubjectTaggerService
    {
        // Konu sabitleri
        public const string SUBJECT_ARAC_TEKNIGI = "Araç Tekniği";
        public const string SUBJECT_ILKYARDIM = "İlkyardım";
        public const string SUBJECT_TRAFIK = "Trafik Mevzuatı";
        public const string SUBJECT_ULASTIRMA = "Ulaştırma Mevzuatı";
        public const string SUBJECT_GENEL = "Genel";

        /// <summary>
        /// Keyword -> Subject eşleşme haritası
        /// Öncelik sırasına göre kontrol edilir
        /// </summary>
        private static readonly Dictionary<string, string[]> _keywordMap = new()
        {
            // Araç Tekniği
            { SUBJECT_ARAC_TEKNIGI, new[] { 
                "motor", "yağ", "vites", "lastik", "fren", "akü", "şanzıman",
                "kaporta", "amortisör", "direksiyon", "debriyaj", "benzin",
                "mazot", "yakıt", "egzoz", "radyatör", "silecek", "far",
                "ampul", "jant", "balata", "hidrolik", "kayış", "periyodik bakım",
                "araç bakım", "teknik", "arıza", "soğutma", "ısınma"
            }},
            
            // İlkyardım
            { SUBJECT_ILKYARDIM, new[] { 
                "ilk yardım", "ilkyardım", "kanama", "solunum", "kalp", "nabız",
                "kaza", "yaralı", "bilinç", "kırık", "yanık", "zehirlenme",
                "boğulma", "şok", "tansiyon", "suni solunum", "kalp masajı",
                "turnike", "pansuman", "sargı", "yaralanma", "ambulans",
                "112", "acil", "kurtarma", "temel yaşam", "hayati"
            }},
            
            // Trafik Mevzuatı
            { SUBJECT_TRAFIK, new[] { 
                "levha", "işaret", "geçiş", "hız", "şerit", "sollama",
                "duraklama", "park", "kavşak", "yaya", "sinyalizasyon",
                "trafik ışığı", "kırmızı ışık", "emniyet kemeri", "kask",
                "alkol", "uyuşturucu", "ceza puanı", "sürücü belgesi",
                "ehliyet", "tescil", "plaka", "muayene", "geçiş üstünlüğü",
                "öncelik", "dönüş", "geri geri", "otoyol", "karayolu"
            }},
            
            // Ulaştırma Mevzuatı
            { SUBJECT_ULASTIRMA, new[] { 
                "hukuk", "sigorta", "belge", "yetki belgesi", "src",
                "taşımacılık", "mevzuat", "kanun", "yönetmelik", "bakanlık",
                "firma", "şirket", "taşıma", "nakliye", "kargo", "yük",
                "tonaj", "kapasite", "tır", "kamyon", "dorse", "konteyner",
                "gümrük", "uluslararası", "transit", "sefer", "ruhsat",
                "vergi", "harç", "ceza", "idari", "ticari", "AETR",
                "çalışma süresi", "dinlenme", "mola", "takograf"
            }}
        };

        /// <summary>
        /// Tek bir soruya konu ata
        /// </summary>
        public static string TagQuestion(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
                return SUBJECT_GENEL;
                
            var lowerText = questionText.ToLowerInvariant();
            
            // Her kategoriyi kontrol et
            foreach (var category in _keywordMap)
            {
                foreach (var keyword in category.Value)
                {
                    if (lowerText.Contains(keyword.ToLowerInvariant()))
                    {
                        return category.Key;
                    }
                }
            }
            
            return SUBJECT_GENEL;
        }

        /// <summary>
        /// Soru listesine toplu konu ataması
        /// </summary>
        public static void AutoTagQuestions(List<QuestionModel> questions)
        {
            foreach (var question in questions)
            {
                // Sadece "Genel" veya boş olanları güncelle
                if (string.IsNullOrEmpty(question.Subject) || question.Subject == SUBJECT_GENEL)
                {
                    question.Subject = TagQuestion(question.Soru);
                }
            }
        }

        /// <summary>
        /// Tüm mevcut konuları getir
        /// </summary>
        public static List<string> GetAllSubjects()
        {
            return new List<string>
            {
                SUBJECT_ARAC_TEKNIGI,
                SUBJECT_ILKYARDIM,
                SUBJECT_TRAFIK,
                SUBJECT_ULASTIRMA,
                SUBJECT_GENEL
            };
        }
    }
}
