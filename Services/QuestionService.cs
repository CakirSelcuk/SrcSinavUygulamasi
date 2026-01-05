using System.Reflection;
using System.Text.Json;
using SrcSinavUygulamasi.Models;
using Microsoft.Maui.Storage; // Dosya okuma işlemi için gerekli

namespace SrcSinavUygulamasi.Services
{
    public class QuestionService
    {
        public async Task<List<QuestionModel>> SorulariGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}");
        }

        // Gerçek sınav sorularını getir
        public async Task<List<QuestionModel>> SinavSorulariniGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}_sinav");
        }

        // Resimli soruları getir
        public async Task<List<QuestionModel>> ResimliSorulariGetir(string kategoriId)
        {
            return await GetQuestionsFromResource($"sorular_{kategoriId.ToLower()}_resimli");
        }

        private async Task<List<QuestionModel>> GetQuestionsFromResource(string resourceName)
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

                return sorular ?? new List<QuestionModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KRİTİK HATA: {ex.Message}");
                return new List<QuestionModel>();
            }
        }
    }
}