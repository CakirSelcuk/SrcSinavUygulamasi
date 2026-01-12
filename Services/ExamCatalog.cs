using SrcSinavUygulamasi.Models;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// Central exam registry - manages exam IDs per category.
    /// Single source of truth for exam identification.
    /// </summary>
    public static class ExamCatalog
    {
        // Kategori başına soru sayıları (soru havuzlarından bağımsız config)
        // Bu değerler JSON yüklendiğinde güncellenebilir
        private static Dictionary<string, List<string>> _categoryExamIds = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Prefixes for key generation
        /// </summary>
        public static class Keys
        {
            public const string COMPLETED_EXAMS = "completed_exams_";
            public const string EXAM_RESULT = "exam_result_";
            public const string WRONG_QUESTIONS = "wrong_";
            public const string EXAM_PROGRESS = "exam_progress_";
            public const string QUESTION_PROGRESS = "question_progress_";

            /// <summary>Completed exams key</summary>
            public static string CompletedExams(string categoryId) => $"{COMPLETED_EXAMS}{categoryId.ToLower()}";
            
            /// <summary>Exam result key</summary>
            public static string ExamResult(string categoryId, string examId) => $"{EXAM_RESULT}{categoryId.ToLower()}_{examId}";
            
            /// <summary>Wrong questions key</summary>
            public static string WrongQuestions(string categoryId, string examId) => $"{WRONG_QUESTIONS}{categoryId.ToLower()}_{examId}";
        }

        /// <summary>
        /// Standard exam ID format for practice exams
        /// </summary>
        public static string GetPracticeExamId(int index) => $"deneme_{index + 1}";

        /// <summary>
        /// Standard exam IDs for special exams
        /// </summary>
        public const string REAL_EXAM_ID = "real_exam";
        public const string IMAGE_EXAM_ID = "image_exam";
        public const string MINI_EXAM_ID = "mini_exam";

        /// <summary>
        /// Register exams for a category (called when loading exam list)
        /// </summary>
        public static void RegisterCategoryExams(string categoryId, int practiceExamCount, bool hasImageExam, bool hasRealExam)
        {
            lock (_lock)
            {
                var examIds = new List<string>();

                // Practice exams
                for (int i = 0; i < practiceExamCount; i++)
                {
                    examIds.Add(GetPracticeExamId(i));
                }

                // Image exam
                if (hasImageExam)
                {
                    examIds.Add(IMAGE_EXAM_ID);
                }

                // Real exam
                if (hasRealExam)
                {
                    examIds.Add(REAL_EXAM_ID);
                }

                _categoryExamIds[categoryId.ToLower()] = examIds;

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"📋 ExamCatalog: {categoryId} registered with {examIds.Count} exams:");
                foreach (var id in examIds)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {id}");
                }
#endif
            }
        }

        /// <summary>
        /// Get all exam IDs for a category (excluding mini exam)
        /// </summary>
        public static List<string> GetExamIds(string categoryId)
        {
            lock (_lock)
            {
                if (_categoryExamIds.TryGetValue(categoryId.ToLower(), out var ids))
                {
                    return ids.ToList();
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Get only practice exam IDs (for 5/5/5/5 rule)
        /// </summary>
        public static List<string> GetPracticeExamIds(string categoryId)
        {
            return GetExamIds(categoryId)
                .Where(id => id.StartsWith("deneme_"))
                .ToList();
        }

        /// <summary>
        /// Get the next exam ID in the same category, or null if no more exams
        /// </summary>
        public static string? GetNextExamId(string categoryId, string currentExamId)
        {
            var allIds = GetExamIds(categoryId);
            int currentIndex = allIds.IndexOf(currentExamId);

            if (currentIndex < 0 || currentIndex >= allIds.Count - 1)
            {
                return null; // Not found or last exam
            }

            return allIds[currentIndex + 1];
        }

        /// <summary>
        /// Check if an exam ID is valid for a category
        /// </summary>
        public static bool IsValidExamId(string categoryId, string examId)
        {
            return GetExamIds(categoryId).Contains(examId);
        }

        /// <summary>
        /// Category display names
        /// </summary>
        private static readonly Dictionary<string, string> _categoryDisplayNames = new()
        {
            { "src1", "SRC1" },
            { "src2", "SRC2" },
            { "src3", "SRC3" },
            { "src4", "SRC4" },
            { "src5", "SRC5" }
        };

        /// <summary>
        /// Get display name for a category (e.g., "SRC1")
        /// </summary>
        public static string GetCategoryDisplayName(string categoryId)
        {
            return _categoryDisplayNames.TryGetValue(categoryId.ToLower(), out var name) 
                ? name 
                : categoryId.ToUpper();
        }

        /// <summary>
        /// Get display title for an exam (e.g., "SRC1 - Deneme Sınavı 3")
        /// </summary>
        public static string GetDisplayTitle(string categoryId, string examId)
        {
            var categoryName = GetCategoryDisplayName(categoryId);

            // Deneme sınavları
            if (examId.StartsWith("deneme_"))
            {
                if (int.TryParse(examId.Replace("deneme_", ""), out int num))
                {
                    return $"{categoryName} - Deneme Sınavı {num}";
                }
            }

            // Özel sınavlar
            return examId switch
            {
                REAL_EXAM_ID => $"{categoryName} - Gerçek Sınav Simülasyonu",
                IMAGE_EXAM_ID => $"{categoryName} - Resimli Sorular",
                MINI_EXAM_ID => $"{categoryName} - Mini Sınav",
                _ => $"{categoryName} - {examId}"
            };
        }

        /// <summary>
        /// Get the next PRACTICE exam ID in the same category (excludes real/image exams).
        /// Returns null if current is last practice exam or not a practice exam.
        /// </summary>
        public static string? GetNextPracticeExamId(string categoryId, string currentExamId)
        {
            // Sadece deneme sınavları için sonraki hesapla
            if (!currentExamId.StartsWith("deneme_"))
            {
                return null; // Resimli veya gerçek sınav için next yok
            }

            var practiceIds = GetPracticeExamIds(categoryId);
            int currentIndex = practiceIds.IndexOf(currentExamId);

            if (currentIndex < 0 || currentIndex >= practiceIds.Count - 1)
            {
                return null; // Not found or last practice exam
            }

            return practiceIds[currentIndex + 1];
        }

        /// <summary>
        /// Check if ALL PRACTICE exams are completed (for analysis unlock)
        /// </summary>
        public static bool AreAllPracticeExamsCompleted(string categoryId, List<string> completedExamIds)
        {
            var practiceIds = GetPracticeExamIds(categoryId);
            if (practiceIds.Count == 0) return false;
            
            return practiceIds.All(id => completedExamIds.Contains(id));
        }

        /// <summary>
        /// Get practice exam count for a category
        /// </summary>
        public static int GetPracticeExamCount(string categoryId)
        {
            return GetPracticeExamIds(categoryId).Count;
        }

        /// <summary>
        /// Check if this is the last practice exam in the category
        /// </summary>
        public static bool IsLastPracticeExam(string categoryId, string examId)
        {
            var practiceIds = GetPracticeExamIds(categoryId);
            return practiceIds.Count > 0 && practiceIds[^1] == examId;
        }

        /// <summary>
        /// Check if this is the last exam in the category
        /// </summary>
        public static bool IsLastExam(string categoryId, string examId)
        {
            var allIds = GetExamIds(categoryId);
            return allIds.Count > 0 && allIds[^1] == examId;
        }

        /// <summary>
        /// Get exam index (0-based) for UI display
        /// </summary>
        public static int GetExamIndex(string categoryId, string examId)
        {
            return GetExamIds(categoryId).IndexOf(examId);
        }

        /// <summary>
        /// Debug: Print completion status
        /// </summary>
#if DEBUG
        public static void DebugPrintCompletionStatus(string categoryId, List<string> completedIds)
        {
            var expectedIds = GetExamIds(categoryId);
            System.Diagnostics.Debug.WriteLine($"🔍 Completion Check for {categoryId}:");
            System.Diagnostics.Debug.WriteLine($"   Expected: [{string.Join(", ", expectedIds)}]");
            System.Diagnostics.Debug.WriteLine($"   Completed: [{string.Join(", ", completedIds)}]");
            
            var missing = expectedIds.Except(completedIds).ToList();
            if (missing.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"   ❌ Missing: [{string.Join(", ", missing)}]");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"   ✅ All exams completed!");
            }
        }
#endif
    }
}
