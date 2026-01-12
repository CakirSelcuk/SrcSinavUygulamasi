using SrcSinavUygulamasi.Models;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Dengeli sınav oluşturma sonucu
    /// </summary>
    public record BalancedExamResult(
        List<QuestionModel> Questions,
        bool IsValid,
        string ErrorMessage);

    /// <summary>
    /// Balanced Exam Builder - Soru havuzundan 5A/5B/5C/5D dağılımıyla deneme oluşturur
    /// </summary>
    public class BalancedExamBuilder
    {
        private const int QUESTIONS_PER_CHOICE = 5;
        private const int TOTAL_QUESTIONS = 20;

        /// <summary>
        /// Soru havuzundan dengeli bir deneme oluşturur.
        /// Seed ile deterministik seçim yapar - aynı seed = aynı sorular.
        /// </summary>
        /// <param name="questionPool">Tüm soru havuzu</param>
        /// <param name="categoryId">Kategori ID (seed için)</param>
        /// <param name="examIndex">Deneme indeksi (seed için)</param>
        /// <returns>BalancedExamResult - sorular veya hata</returns>
        public BalancedExamResult BuildExam(List<QuestionModel> questionPool, string categoryId, int examIndex)
        {
            if (questionPool == null || questionPool.Count == 0)
            {
                return new BalancedExamResult(
                    new List<QuestionModel>(),
                    false,
                    "Soru havuzu boş.");
            }

            // Soruları doğru cevaba göre grupla
            var groupA = questionPool.Where(q => q.DogruCevap == "A").ToList();
            var groupB = questionPool.Where(q => q.DogruCevap == "B").ToList();
            var groupC = questionPool.Where(q => q.DogruCevap == "C").ToList();
            var groupD = questionPool.Where(q => q.DogruCevap == "D").ToList();

            // FAIL FAST: Her gruptan en az 5 soru olmalı
            var validationResult = ValidatePool(groupA.Count, groupB.Count, groupC.Count, groupD.Count);
            if (!validationResult.IsValid)
            {
                return new BalancedExamResult(
                    new List<QuestionModel>(),
                    false,
                    validationResult.ErrorMessage);
            }

            // Deterministik seed oluştur
            string seedString = $"{categoryId.ToLower()}_{examIndex}";
            int seed = GetDeterministicSeed(seedString);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"📊 BalancedExamBuilder: {categoryId} Deneme {examIndex + 1}");
            System.Diagnostics.Debug.WriteLine($"   Havuz: A={groupA.Count}, B={groupB.Count}, C={groupC.Count}, D={groupD.Count}");
            System.Diagnostics.Debug.WriteLine($"   Seed: {seedString} → {seed}");
#endif

            // Her gruptan 5 soru seç (deterministik)
            var selectedQuestions = new List<QuestionModel>();
            
            selectedQuestions.AddRange(SelectDeterministic(groupA, QUESTIONS_PER_CHOICE, seed, 0));
            selectedQuestions.AddRange(SelectDeterministic(groupB, QUESTIONS_PER_CHOICE, seed, 1));
            selectedQuestions.AddRange(SelectDeterministic(groupC, QUESTIONS_PER_CHOICE, seed, 2));
            selectedQuestions.AddRange(SelectDeterministic(groupD, QUESTIONS_PER_CHOICE, seed, 3));

            // Soruları karıştır (ama deterministik olarak)
            var shuffledQuestions = ShuffleDeterministic(selectedQuestions, seed);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"   Seçilen sorular: {shuffledQuestions.Count} adet");
            var distribution = shuffledQuestions.GroupBy(q => q.DogruCevap)
                                                 .ToDictionary(g => g.Key, g => g.Count());
            System.Diagnostics.Debug.WriteLine($"   Dağılım: A={distribution.GetValueOrDefault("A", 0)}, B={distribution.GetValueOrDefault("B", 0)}, C={distribution.GetValueOrDefault("C", 0)}, D={distribution.GetValueOrDefault("D", 0)}");
#endif

            return new BalancedExamResult(shuffledQuestions, true, "");
        }

        /// <summary>
        /// Havuzun yeterli soru içerip içermediğini kontrol et
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidatePool(int countA, int countB, int countC, int countD)
        {
            var insufficient = new List<string>();

            if (countA < QUESTIONS_PER_CHOICE)
                insufficient.Add($"A={countA}");
            if (countB < QUESTIONS_PER_CHOICE)
                insufficient.Add($"B={countB}");
            if (countC < QUESTIONS_PER_CHOICE)
                insufficient.Add($"C={countC}");
            if (countD < QUESTIONS_PER_CHOICE)
                insufficient.Add($"D={countD}");

            if (insufficient.Count > 0)
            {
                string errorMsg = $"Soru havuzunda yetersiz şık dağılımı tespit edildi.\n" +
                                  $"Eksik: {string.Join(", ", insufficient)}\n" +
                                  $"Her şıktan en az {QUESTIONS_PER_CHOICE} soru gereklidir.";
                return (false, errorMsg);
            }

            return (true, "");
        }

        /// <summary>
        /// Soru havuzundan dengeli kaç deneme oluşturulabileceğini hesapla
        /// </summary>
        public int CalculateMaxExams(List<QuestionModel> questionPool)
        {
            if (questionPool == null || questionPool.Count == 0)
                return 0;

            var groupA = questionPool.Count(q => q.DogruCevap == "A");
            var groupB = questionPool.Count(q => q.DogruCevap == "B");
            var groupC = questionPool.Count(q => q.DogruCevap == "C");
            var groupD = questionPool.Count(q => q.DogruCevap == "D");

            // En az soru sayısına sahip gruba göre max deneme sayısı
            int minGroup = Math.Min(Math.Min(groupA, groupB), Math.Min(groupC, groupD));
            return minGroup / QUESTIONS_PER_CHOICE;
        }

        /// <summary>
        /// String'den deterministik seed üret
        /// </summary>
        private int GetDeterministicSeed(string input)
        {
            // Simple but stable hash
            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }

        /// <summary>
        /// Listeden deterministik olarak N eleman seç
        /// </summary>
        private List<QuestionModel> SelectDeterministic(List<QuestionModel> source, int count, int baseSeed, int groupOffset)
        {
            if (source.Count <= count)
                return source.ToList();

            // Her grup için farklı ama deterministik seed
            var random = new Random(baseSeed + groupOffset * 1000);
            
            // Fisher-Yates shuffle ile deterministik sıralama, sonra ilk N'i al
            var shuffled = source.ToList();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled.Take(count).ToList();
        }

        /// <summary>
        /// Listeyi deterministik olarak karıştır
        /// </summary>
        private List<QuestionModel> ShuffleDeterministic(List<QuestionModel> source, int seed)
        {
            var random = new Random(seed + 9999); // Farklı seed ile shuffle
            var result = source.ToList();
            
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}
