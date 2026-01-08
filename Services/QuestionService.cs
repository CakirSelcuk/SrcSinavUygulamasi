using System.Reflection;
using System.Text.Json;
using SrcSinavUygulamasi.Models;
using Microsoft.Maui.Storage;

namespace SrcSinavUygulamasi.Services
{
    public class QuestionService
    {
        /// <summary>
        /// Pratik sorularını getir
        /// </summary>
        public async Task<List<QuestionModel>> SorulariGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}", kategoriId, "pratik");
        }

        /// <summary>
        /// Gerçek sınav sorularını getir
        /// </summary>
        public async Task<List<QuestionModel>> SinavSorulariniGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}_sinav", kategoriId, "sinav");
        }

        /// <summary>
        /// Resimli soruları getir
        /// </summary>
        public async Task<List<QuestionModel>> ResimliSorulariGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}_resimli", kategoriId, "resimli");
        }

        /// <summary>
        /// 20 soruluk deneme için şık dağılımını kontrol et
        /// Her şık (A,B,C,D) tam 5 kez olmalı
        /// </summary>
        public bool ValidateSikDagilimi(List<QuestionModel> sorular)
        {
            if (sorular.Count != 20) return true; // Sadece 20 soruluk denemeler için kontrol

            var dagilim = sorular.GroupBy(s => s.DogruCevap)
                                  .ToDictionary(g => g.Key, g => g.Count());

            bool isValid = dagilim.GetValueOrDefault("A", 0) == 5 &&
                          dagilim.GetValueOrDefault("B", 0) == 5 &&
                          dagilim.GetValueOrDefault("C", 0) == 5 &&
                          dagilim.GetValueOrDefault("D", 0) == 5;

            if (!isValid)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ŞIK DAĞILIMI HATASI!");
                System.Diagnostics.Debug.WriteLine($"   A: {dagilim.GetValueOrDefault("A", 0)}");
                System.Diagnostics.Debug.WriteLine($"   B: {dagilim.GetValueOrDefault("B", 0)}");
                System.Diagnostics.Debug.WriteLine($"   C: {dagilim.GetValueOrDefault("C", 0)}");
                System.Diagnostics.Debug.WriteLine($"   D: {dagilim.GetValueOrDefault("D", 0)}");
            }

            return isValid;
        }

        /// <summary>
        /// Şık dağılımı hata mesajını getir
        /// </summary>
        public string GetSikDagilimiHataMesaji(List<QuestionModel> sorular)
        {
            if (sorular.Count != 20) return "";

            var dagilim = sorular.GroupBy(s => s.DogruCevap)
                                  .ToDictionary(g => g.Key, g => g.Count());

            return $"Bu deneme şu anda yayınlanamaz.\n" +
                   $"Şık dağılımı 5A/5B/5C/5D kuralını sağlamıyor.\n\n" +
                   $"Mevcut dağılım:\n" +
                   $"A: {dagilim.GetValueOrDefault("A", 0)} (beklenen: 5)\n" +
                   $"B: {dagilim.GetValueOrDefault("B", 0)} (beklenen: 5)\n" +
                   $"C: {dagilim.GetValueOrDefault("C", 0)} (beklenen: 5)\n" +
                   $"D: {dagilim.GetValueOrDefault("D", 0)} (beklenen: 5)";
        }

        /// <summary>
        /// ID eksik soru var mı kontrol et
        /// </summary>
        public (bool HasMissingIds, int MissingCount, List<int> MissingIndexes) ValidateQuestionIds(List<QuestionModel> sorular)
        {
            var missingIndexes = new List<int>();
            
            for (int i = 0; i < sorular.Count; i++)
            {
                if (string.IsNullOrEmpty(sorular[i].Id))
                {
                    missingIndexes.Add(i + 1);
                }
            }

            return (missingIndexes.Count > 0, missingIndexes.Count, missingIndexes);
        }

        private async Task<List<QuestionModel>> GetQuestionsFromResource(string resourceName, string categoryId, string examType)
        {
            string resourcePath = $"SrcSinavUygulamasi.Resources.Raw.{resourceName}.json";

            try
            {
                if (string.IsNullOrWhiteSpace(resourceName))
                    return new List<QuestionModel>();

                var assembly = Assembly.GetExecutingAssembly();

                using Stream stream = assembly.GetManifestResourceStream(resourcePath);

                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"HATA: Gömülü kaynak bulunamadı -> {resourcePath}");
                    return new List<QuestionModel>();
                }

                using var reader = new StreamReader(stream);
                string jsonIcerik = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sorular = JsonSerializer.Deserialize<List<QuestionModel>>(jsonIcerik, options);

                if (sorular == null)
                    return new List<QuestionModel>();

                // ═══════════════════════════════════════════════════════════
                // FAIL FAST: ID Validasyonu
                // ═══════════════════════════════════════════════════════════
                // Runtime ID üretimi YASAKTIR!
                // ID yoksa soru yüklenmez.
                // ═══════════════════════════════════════════════════════════
                
                var validSorular = new List<QuestionModel>();
                int skippedCount = 0;

                for (int i = 0; i < sorular.Count; i++)
                {
                    if (string.IsNullOrEmpty(sorular[i].Id))
                    {
                        // FAIL FAST: ID yoksa bu soruyu ATLA
                        skippedCount++;
                        System.Diagnostics.Debug.WriteLine($"❌ FAIL FAST: {resourceName} soru #{i + 1} - ID EKSİK! Soru yüklenmedi.");
                    }
                    else
                    {
                        validSorular.Add(sorular[i]);
                    }
                }

                if (skippedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ UYARI: {resourceName} dosyasında {skippedCount} soru ID eksikliği nedeniyle YÜKLENMEDI!");
                    System.Diagnostics.Debug.WriteLine($"   Lütfen tools/assign_question_ids aracını çalıştırın.");
                }

                return validSorular;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KRİTİK HATA: {ex.Message}");
                return new List<QuestionModel>();
            }
        }
    }
}