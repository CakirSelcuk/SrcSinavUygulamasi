using System.Text.Json;
using SrcSinavUygulamasi.Models;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Sınav ilerleme ve sonuç takibi servisi.
    /// Tüm veriler Preferences ile kalıcı olarak saklanır.
    /// Her SRC kategorisi kendi verisini izole tutar.
    /// </summary>
    public class ExamProgressService
    {
        private const string EXAM_PROGRESS_PREFIX = "exam_progress_";
        private const string QUESTION_PROGRESS_PREFIX = "question_progress_";

        #region Exam Progress

        /// <summary>
        /// Sınav sonucunu kaydet. Aynı examId varsa overwrite eder.
        /// </summary>
        public void SaveExamAttempt(
            string categoryId,
            string examId,
            Dictionary<string, string> answersMap,
            int totalQuestions,
            int correctCount,
            int wrongCount,
            int blankCount)
        {
            var progress = new ExamProgressModel
            {
                CategoryId = categoryId.ToLower(),
                ExamId = examId,
                CompletedDate = DateTime.UtcNow,
                TotalQuestionCount = totalQuestions,
                CorrectCount = correctCount,
                WrongCount = wrongCount,
                BlankCount = blankCount,
                Answers = answersMap,
                WrongAnswers = answersMap
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                    .Where(kvp => {
                        // Burada doğru cevabı kontrol edemiyoruz çünkü sadece cevap var
                        // WrongAnswers dışarıdan set edilecek
                        return true;
                    })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var allProgress = GetAllExamProgress(categoryId);
            
            // Aynı examId varsa üzerine yaz
            var existing = allProgress.FirstOrDefault(p => p.ExamId == examId);
            if (existing != null)
            {
                allProgress.Remove(existing);
            }
            allProgress.Add(progress);

            SaveAllExamProgress(categoryId, allProgress);
        }

        /// <summary>
        /// Sınav sonucunu kaydet (hazır model ile)
        /// </summary>
        public void SaveExamProgress(ExamProgressModel progress)
        {
            var allProgress = GetAllExamProgress(progress.CategoryId);
            
            var existing = allProgress.FirstOrDefault(p => p.ExamId == progress.ExamId);
            if (existing != null)
            {
                allProgress.Remove(existing);
            }
            allProgress.Add(progress);

            SaveAllExamProgress(progress.CategoryId, allProgress);
        }

        /// <summary>
        /// Tamamlanan deneme ID'lerini getir
        /// </summary>
        public List<string> GetCompletedExamIds(string categoryId)
        {
            return GetAllExamProgress(categoryId)
                .Select(p => p.ExamId)
                .ToList();
        }

        /// <summary>
        /// Tüm denemeler tamamlandı mı?
        /// </summary>
        public bool AreAllExamsCompleted(string categoryId, List<string> expectedExamIds)
        {
            var completedIds = GetCompletedExamIds(categoryId);
            return expectedExamIds.All(id => completedIds.Contains(id));
        }

        /// <summary>
        /// Belirli bir kategorideki TÜM yanlış soruları getir (tekrarsız)
        /// </summary>
        public Dictionary<string, string> GetAllWrongQuestionsUnique(string categoryId)
        {
            var allProgress = GetAllExamProgress(categoryId);
            var uniqueWrongs = new Dictionary<string, string>();

            foreach (var exam in allProgress)
            {
                foreach (var wrong in exam.WrongAnswers)
                {
                    if (!uniqueWrongs.ContainsKey(wrong.Key))
                    {
                        uniqueWrongs[wrong.Key] = wrong.Value;
                    }
                }
            }

            return uniqueWrongs;
        }

        /// <summary>
        /// Belirli bir sınavın sonucunu getir
        /// </summary>
        public ExamProgressModel? GetExamProgress(string categoryId, string examId)
        {
            return GetAllExamProgress(categoryId)
                .FirstOrDefault(p => p.ExamId == examId);
        }

        private List<ExamProgressModel> GetAllExamProgress(string categoryId)
        {
            var key = EXAM_PROGRESS_PREFIX + categoryId.ToLower();
            var json = Preferences.Get(key, "[]");
            
            try
            {
                return JsonSerializer.Deserialize<List<ExamProgressModel>>(json) ?? new List<ExamProgressModel>();
            }
            catch
            {
                return new List<ExamProgressModel>();
            }
        }

        private void SaveAllExamProgress(string categoryId, List<ExamProgressModel> progress)
        {
            var key = EXAM_PROGRESS_PREFIX + categoryId.ToLower();
            var json = JsonSerializer.Serialize(progress);
            Preferences.Set(key, json);
        }

        #endregion

        #region Question Progress

        /// <summary>
        /// Soru ilerleme bilgisini getir
        /// </summary>
        public QuestionProgressModel GetQuestionProgress(string categoryId, string questionId)
        {
            var allProgress = GetAllQuestionProgress(categoryId);
            return allProgress.FirstOrDefault(p => p.QuestionId == questionId) 
                ?? new QuestionProgressModel { QuestionId = questionId };
        }

        /// <summary>
        /// Soru doğru cevaplandığında çağrılır
        /// </summary>
        public void MarkQuestionCorrect(string categoryId, string questionId)
        {
            var allProgress = GetAllQuestionProgress(categoryId);
            var progress = allProgress.FirstOrDefault(p => p.QuestionId == questionId);
            
            if (progress == null)
            {
                progress = new QuestionProgressModel { QuestionId = questionId };
                allProgress.Add(progress);
            }

            progress.CorrectStreakCount++;
            progress.LastCorrectDate = DateTime.UtcNow;

            SaveAllQuestionProgress(categoryId, allProgress);
        }

        /// <summary>
        /// Soru yanlış cevaplandığında çağrılır
        /// </summary>
        public void MarkQuestionWrong(string categoryId, string questionId)
        {
            var allProgress = GetAllQuestionProgress(categoryId);
            var progress = allProgress.FirstOrDefault(p => p.QuestionId == questionId);
            
            if (progress == null)
            {
                progress = new QuestionProgressModel { QuestionId = questionId };
                allProgress.Add(progress);
            }

            progress.CorrectStreakCount = 0; // Streak sıfırlanır
            progress.TotalWrongCount++;
            progress.LastWrongDate = DateTime.UtcNow;

            SaveAllQuestionProgress(categoryId, allProgress);
        }

        /// <summary>
        /// Soruyu mini sınavlardan gizle
        /// </summary>
        public void MarkQuestionAsHiddenForMini(string categoryId, string questionId)
        {
            var allProgress = GetAllQuestionProgress(categoryId);
            var progress = allProgress.FirstOrDefault(p => p.QuestionId == questionId);
            
            if (progress == null)
            {
                progress = new QuestionProgressModel { QuestionId = questionId };
                allProgress.Add(progress);
            }

            progress.IsHiddenForMiniExams = true;

            SaveAllQuestionProgress(categoryId, allProgress);
        }

        /// <summary>
        /// Mini sınav için soru listesi oluştur
        /// </summary>
        public List<string> BuildMiniExamQuestionIds(string categoryId, int maxQuestions = 15)
        {
            var wrongQuestions = GetAllWrongQuestionsUnique(categoryId);
            var questionProgresses = GetAllQuestionProgress(categoryId);

            // Gizlenen ve öğrenilen soruları filtrele
            var eligibleQuestions = wrongQuestions.Keys
                .Where(qId => {
                    var progress = questionProgresses.FirstOrDefault(p => p.QuestionId == qId);
                    if (progress == null) return true;
                    return !progress.IsHiddenForMiniExams && !progress.IsLearned;
                })
                .ToList();

            // En fazla maxQuestions kadar al
            if (eligibleQuestions.Count <= maxQuestions)
            {
                return eligibleQuestions;
            }

            // Rastgele seç
            var random = new Random();
            return eligibleQuestions
                .OrderBy(x => random.Next())
                .Take(maxQuestions)
                .ToList();
        }

        /// <summary>
        /// Hazırlık durumu hesapla
        /// </summary>
        public (string Label, string Color) GetReadinessStatus(string categoryId)
        {
            var allProgress = GetAllExamProgress(categoryId);
            if (allProgress.Count == 0)
            {
                return ("Henüz Sınav Yok", "#64748b");
            }

            var avgSuccess = allProgress.Average(p => p.SuccessRate);

            if (avgSuccess >= 85)
                return ("Hazır ✓", "#22c55e");
            else if (avgSuccess >= 60)
                return ("Geliştirilmeli", "#f59e0b");
            else
                return ("Tekrar Gerekli", "#ef4444");
        }

        private List<QuestionProgressModel> GetAllQuestionProgress(string categoryId)
        {
            var key = QUESTION_PROGRESS_PREFIX + categoryId.ToLower();
            var json = Preferences.Get(key, "[]");
            
            try
            {
                return JsonSerializer.Deserialize<List<QuestionProgressModel>>(json) ?? new List<QuestionProgressModel>();
            }
            catch
            {
                return new List<QuestionProgressModel>();
            }
        }

        private void SaveAllQuestionProgress(string categoryId, List<QuestionProgressModel> progress)
        {
            var key = QUESTION_PROGRESS_PREFIX + categoryId.ToLower();
            var json = JsonSerializer.Serialize(progress);
            Preferences.Set(key, json);
        }

        #endregion

        #region Mini Exam (Laundry) Logic

        /// <summary>
        /// Mini sınavda doğru cevaplanan soruları TÜM sınavların WrongAnswers listesinden sil.
        /// Bu "çamaşır" mantığıdır - doğru cevaplanan sorular artık yanlış değildir.
        /// </summary>
        /// <param name="categoryId">Kategori ID</param>
        /// <param name="clearedQuestionIds">Doğru cevaplanan soru ID'leri</param>
        /// <returns>Toplam temizlenen soru sayısı</returns>
        public int ClearCorrectAnswersFromWrongList(string categoryId, List<string> clearedQuestionIds)
        {
            if (clearedQuestionIds == null || clearedQuestionIds.Count == 0)
                return 0;

            var allProgress = GetAllExamProgress(categoryId);
            int totalCleared = 0;

            foreach (var exam in allProgress)
            {
                int before = exam.WrongAnswers.Count;
                
                // Doğru cevaplanan soruları WrongAnswers'dan kaldır
                foreach (var questionId in clearedQuestionIds)
                {
                    if (exam.WrongAnswers.ContainsKey(questionId))
                    {
                        exam.WrongAnswers.Remove(questionId);
                    }
                }
                
                totalCleared += (before - exam.WrongAnswers.Count);
            }

            // Güncellenmiş verileri kaydet
            SaveAllExamProgress(categoryId, allProgress);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"🧺 Laundry: {clearedQuestionIds.Count} ID temizlendi, toplam {totalCleared} kayıt silindi");
#endif

            return totalCleared;
        }

        /// <summary>
        /// Kategorideki toplam yanlış soru sayısını getir (tekrarsız)
        /// </summary>
        public int GetTotalWrongCount(string categoryId)
        {
            return GetAllWrongQuestionsUnique(categoryId).Count;
        }
        #endregion
    }
}

