using SrcSinavUygulamasi.Models;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Sınav sonuç analizi ve Fear Logic mesaj üretimi.
    /// Konu bazlı başarı hesaplama + kritik uyarılar.
    /// </summary>
    public class ExamAnalysisService
    {
        private readonly QuestionService _questionService = new();

        // Fear Logic Renk Sabitleri (MVP için hardcoded)
        public const string COLOR_CRITICAL = "#B00020";  // Kan kırmızısı
        public const string COLOR_WARNING = "#FF6B00";   // Turuncu
        public const string COLOR_SUCCESS = "#22c55e";   // Yeşil
        public const string COLOR_TEXT_ON_CRITICAL = "#FFFFFF";  // Beyaz

        /// <summary>
        /// Konu bazlı analiz sonucu
        /// </summary>
        public class AnalysisResult
        {
            public List<SubjectScoreModel> SubjectScores { get; set; } = new();
            public double OverallPercentage { get; set; }
            public bool IsCriticalState { get; set; }
            public string CriticalMessage { get; set; } = "";
            public string CriticalColor { get; set; } = "";
            public string StatusTitle { get; set; } = "";
            public List<string> Recommendations { get; set; } = new();
        }

        /// <summary>
        /// Sınav sonucunu analiz et (Fear Logic dahil)
        /// </summary>
        public async Task<AnalysisResult> AnalyzeResults(
            string categoryId,
            ExamProgressModel progress,
            List<QuestionModel>? allQuestions = null)
        {
            var result = new AnalysisResult();

            // Soruları getir (verilmediyse)
            if (allQuestions == null || allQuestions.Count == 0)
            {
                allQuestions = await _questionService.SorulariGetir(categoryId);
                
                // Auto-tag uygula (JSON'da subject yoksa)
                SubjectTaggerService.AutoTagQuestions(allQuestions);
            }

            // Soru ID -> Question eşlemesi
            var questionDict = allQuestions.ToDictionary(q => q.Id, q => q);

            // Konu bazlı istatistikler
            var subjectStats = new Dictionary<string, (int Correct, int Wrong)>();
            
            foreach (var subject in SubjectTaggerService.GetAllSubjects())
            {
                subjectStats[subject] = (0, 0);
            }

            // Her cevabı analiz et
            foreach (var answer in progress.Answers)
            {
                if (!questionDict.TryGetValue(answer.Key, out var question))
                    continue;

                var subject = question.Subject ?? "Genel";
                
                // Ensure subject exists in dictionary
                if (!subjectStats.ContainsKey(subject))
                    subjectStats[subject] = (0, 0);
                
                var currentStats = subjectStats[subject];
                bool isCorrect = answer.Value == question.DogruCevap;
                
                if (isCorrect)
                    subjectStats[subject] = (currentStats.Correct + 1, currentStats.Wrong);
                else
                    subjectStats[subject] = (currentStats.Correct, currentStats.Wrong + 1);
            }

            // SubjectScoreModel listesi oluştur
            foreach (var stat in subjectStats)
            {
                if (stat.Value.Correct + stat.Value.Wrong > 0)
                {
                    result.SubjectScores.Add(new SubjectScoreModel
                    {
                        Subject = stat.Key,
                        CorrectCount = stat.Value.Correct,
                        WrongCount = stat.Value.Wrong
                    });
                }
            }

            // Genel başarı yüzdesi
            int totalCorrect = result.SubjectScores.Sum(s => s.CorrectCount);
            int totalQuestions = result.SubjectScores.Sum(s => s.TotalCount);
            result.OverallPercentage = totalQuestions > 0 
                ? Math.Round((double)totalCorrect / totalQuestions * 100, 1) 
                : 0;

            // ═══════════════════════════════════════════════════════════
            // FEAR LOGIC: Kritik durum tespiti ve mesaj üretimi
            // ═══════════════════════════════════════════════════════════
            GenerateFearLogicMessages(result);

            return result;
        }

        /// <summary>
        /// Fear Logic mesajları üret
        /// </summary>
        private void GenerateFearLogicMessages(AnalysisResult result)
        {
            var criticalMessages = new List<string>();
            var recommendations = new List<string>();

            // 1. Genel başarı kontrolü
            if (result.OverallPercentage < 70)
            {
                result.IsCriticalState = true;
                result.StatusTitle = "⛔ DURUM KRİTİK";
                criticalMessages.Add("Bugün sınav olsaydı KALIRDINIZ!");
                recommendations.Add("Tüm konuları acilen tekrar edin.");
            }
            else if (result.OverallPercentage < 85)
            {
                result.StatusTitle = "⚠️ GELİŞTİRİLMELİ";
                recommendations.Add("Performansınız sınırda, eksiklerinizi kapatın.");
            }
            else
            {
                result.StatusTitle = "✅ HAZIRSINIZ";
            }

            // 2. Konu bazlı kritik kontroller
            foreach (var score in result.SubjectScores)
            {
                if (score.Percentage < 50)
                {
                    result.IsCriticalState = true;
                    criticalMessages.Add($"{score.Subject} konusunda çok risklisiniz! (%{score.Percentage:F0})");
                    recommendations.Add($"{score.Subject} konusunu öncelikli çalışın.");
                }
                else if (score.Percentage < 70)
                {
                    recommendations.Add($"{score.Subject} konusunu pekiştirin.");
                }
            }

            // 3. Özel kontroller
            var trafikScore = result.SubjectScores.FirstOrDefault(s => s.Subject == SubjectTaggerService.SUBJECT_TRAFIK);
            if (trafikScore != null && trafikScore.Percentage < 60)
            {
                criticalMessages.Add("Trafik mevzuatı sınavın ana konusu - burada zayıfsınız!");
            }

            var ilkyardimScore = result.SubjectScores.FirstOrDefault(s => s.Subject == SubjectTaggerService.SUBJECT_ILKYARDIM);
            if (ilkyardimScore != null && ilkyardimScore.Percentage < 50)
            {
                criticalMessages.Add("İlkyardım soruları genellikle kolay puandır - kaybetmeyin!");
            }

            // Mesajları birleştir
            if (criticalMessages.Count > 0)
            {
                result.CriticalMessage = string.Join("\n", criticalMessages);
                result.CriticalColor = COLOR_CRITICAL;
            }
            else if (result.OverallPercentage < 85)
            {
                result.CriticalMessage = "Eksiklerinizi kapatmak için süreniz var.";
                result.CriticalColor = COLOR_WARNING;
            }

            result.Recommendations = recommendations;
        }

        /// <summary>
        /// Hızlı özet analiz (Sonuç sayfası için)
        /// </summary>
        public (bool IsCritical, string Message, string Color) GetQuickAnalysis(int correctCount, int totalQuestions)
        {
            double percentage = totalQuestions > 0 
                ? (double)correctCount / totalQuestions * 100 
                : 0;

            if (percentage < 70)
            {
                return (true, 
                    $"⛔ KALDINIZ! (%" + percentage.ToString("F0") + ")\nSınava hazır değilsiniz. Eksiklerinizi hemen kapatın!", 
                    COLOR_CRITICAL);
            }
            else if (percentage < 85)
            {
                return (false, 
                    $"⚠️ GEÇTİNİZ ama risklisiniz (%" + percentage.ToString("F0") + ")\nKonuları pekiştirmeniz gerekiyor.", 
                    COLOR_WARNING);
            }
            else
            {
                return (false, 
                    $"✅ BAŞARILI! (%" + percentage.ToString("F0") + ")\nSınava hazırsınız.", 
                    COLOR_SUCCESS);
            }
        }
    }
}
