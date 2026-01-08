using System.Reflection;
using System.Text.Json;
using SrcSinavUygulamasi.Models;
using Microsoft.Maui.Storage;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Soru yükleme sonucu - FAIL FAST için hata bilgisi içerir
    /// </summary>
    public record QuestionLoadResult(
        List<QuestionModel> Questions,
        bool IsValid,
        string ErrorMessage);

    public class QuestionService
    {
        /// <summary>
        /// Pratik sorularını getir (eski uyum için)
        /// </summary>
        public async Task<List<QuestionModel>> SorulariGetir(string kategoriId)
        {
            var result = await SorulariGetirWithValidation(kategoriId);
            return result.Questions;
        }

        /// <summary>
        /// Pratik sorularını FAIL FAST validasyonu ile getir
        /// </summary>
        public async Task<QuestionLoadResult> SorulariGetirWithValidation(string kategoriId)
        {
            return await GetQuestionsFromResourceWithValidation($"sorular_{kategoriId.ToLower()}", kategoriId, "pratik");
        }

        /// <summary>
        /// Gerçek sınav sorularını getir (eski uyum için)
        /// </summary>
        public async Task<List<QuestionModel>> SinavSorulariniGetir(string kategoriId)
        {
            var result = await SinavSorulariniGetirWithValidation(kategoriId);
            return result.Questions;
        }

        /// <summary>
        /// Gerçek sınav sorularını FAIL FAST validasyonu ile getir
        /// </summary>
        public async Task<QuestionLoadResult> SinavSorulariniGetirWithValidation(string kategoriId)
        {
            return await GetQuestionsFromResourceWithValidation($"sorular_{kategoriId.ToLower()}_sinav", kategoriId, "sinav");
        }

        /// <summary>
        /// Resimli soruları getir (eski uyum için)
        /// </summary>
        public async Task<List<QuestionModel>> ResimliSorulariGetir(string kategoriId)
        {
            var result = await ResimliSorulariGetirWithValidation(kategoriId);
            return result.Questions;
        }

        /// <summary>
        /// Resimli soruları FAIL FAST validasyonu ile getir
        /// </summary>
        public async Task<QuestionLoadResult> ResimliSorulariGetirWithValidation(string kategoriId)
        {
            return await GetQuestionsFromResourceWithValidation($"sorular_{kategoriId.ToLower()}_resimli", kategoriId, "resimli");
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

#if DEBUG
            if (!isValid)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ŞIK DAĞILIMI HATASI!");
                System.Diagnostics.Debug.WriteLine($"   A: {dagilim.GetValueOrDefault("A", 0)}");
                System.Diagnostics.Debug.WriteLine($"   B: {dagilim.GetValueOrDefault("B", 0)}");
                System.Diagnostics.Debug.WriteLine($"   C: {dagilim.GetValueOrDefault("C", 0)}");
                System.Diagnostics.Debug.WriteLine($"   D: {dagilim.GetValueOrDefault("D", 0)}");
            }
#endif

            return isValid;
        }

        /// <summary>
        /// Şık dağılımı hata mesajını getir (Kullanıcıya gösterilecek Türkçe mesaj)
        /// </summary>
        public string GetSikDagilimiHataMesaji(List<QuestionModel> sorular)
        {
            if (sorular.Count != 20) return "";

            var dagilim = sorular.GroupBy(s => s.DogruCevap)
                                  .ToDictionary(g => g.Key, g => g.Count());

            return "Bu deneme şu an yayınlanamaz. Şık dağılımı 5A/5B/5C/5D değil.";
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

        /// <summary>
        /// FAIL FAST: Soru yükleme ve validasyon (ID eksik/tekrarlı kontrol)
        /// </summary>
        private async Task<QuestionLoadResult> GetQuestionsFromResourceWithValidation(string resourceName, string categoryId, string examType)
        {
            string resourcePath = $"SrcSinavUygulamasi.Resources.Raw.{resourceName}.json";

            try
            {
                if (string.IsNullOrWhiteSpace(resourceName))
                    return new QuestionLoadResult(new List<QuestionModel>(), false, "Geçersiz kaynak adı.");

                var assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(resourcePath);

                if (stream == null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"HATA: Gömülü kaynak bulunamadı -> {resourcePath}");
#endif
                    return new QuestionLoadResult(new List<QuestionModel>(), false, "Soru dosyası bulunamadı.");
                }

                using var reader = new StreamReader(stream);
                string jsonIcerik = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sorular = JsonSerializer.Deserialize<List<QuestionModel>>(jsonIcerik, options);

                if (sorular == null || sorular.Count == 0)
                    return new QuestionLoadResult(new List<QuestionModel>(), false, "Soru dosyası boş.");

                // ═══════════════════════════════════════════════════════════
                // FAIL FAST: ID Validasyonu
                // ═══════════════════════════════════════════════════════════
                // Runtime ID üretimi YASAKTIR!
                // ID yoksa veya tekrarlıysa SINAV BAŞLATILMAZ.
                // ═══════════════════════════════════════════════════════════

                var seenIds = new HashSet<string>();
                var missingIdIndexes = new List<int>();
                var duplicateIds = new List<string>();

                for (int i = 0; i < sorular.Count; i++)
                {
                    var id = sorular[i].Id;
                    
                    if (string.IsNullOrEmpty(id))
                    {
                        missingIdIndexes.Add(i + 1);
                    }
                    else if (seenIds.Contains(id))
                    {
                        duplicateIds.Add(id);
                    }
                    else
                    {
                        seenIds.Add(id);
                    }
                }

                // FAIL FAST: Eksik ID varsa sınavı ENGELLE
                if (missingIdIndexes.Count > 0)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"❌ FAIL FAST: {resourceName} - {missingIdIndexes.Count} adet ID eksik!");
#endif
                    return new QuestionLoadResult(
                        new List<QuestionModel>(),
                        false,
                        $"Sınav verileri geçersiz. {missingIdIndexes.Count} soruda ID eksik.");
                }

                // FAIL FAST: Tekrarlı ID varsa sınavı ENGELLE
                if (duplicateIds.Count > 0)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"❌ FAIL FAST: {resourceName} - Tekrarlı ID'ler: {string.Join(", ", duplicateIds)}");
#endif
                    return new QuestionLoadResult(
                        new List<QuestionModel>(),
                        false,
                        "Sınav verileri geçersiz. Tekrarlanan ID tespit edildi.");
                }

                // Validasyon başarılı
                return new QuestionLoadResult(sorular, true, "");
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"KRİTİK HATA: {ex.Message}");
#endif
                return new QuestionLoadResult(new List<QuestionModel>(), false, "Soru yüklenirken bir hata oluştu.");
            }
        }
    }
}