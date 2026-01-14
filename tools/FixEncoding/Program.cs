using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        // Correct replacements based on hex analysis
        // The broken chars appear as: Å (U+00C5) + Ÿ (U+0178) = should be ş
        var replacements = new (string from, string to)[]
        {
            ("\u00C5\u0178", "\u015F"),  // ÅŸ -> ş
            ("\u00C4\u0178", "\u011F"),  // ÄŸ -> ğ (guessing pattern)
            ("\u00C5\u017D", "\u015E"),  // ÅŽ -> Ş (guessing)
            ("\u00C4\u017D", "\u011E"),  // ÄŽ -> Ğ (guessing)
        };

        string baseDir = @"..\..\";
        string[] files = {
            "Resources/Raw/sorular_src1.json",
            "Resources/Raw/sorular_src1_sinav.json",
            "Resources/Raw/sorular_src1_resimli.json",
            "Resources/Raw/sorular_src2.json",
            "Resources/Raw/sorular_src2_sinav.json",
            "Resources/Raw/sorular_src2_resimli.json",
            "Resources/Raw/sorular_src3.json",
            "Resources/Raw/sorular_src3_sinav.json",
            "Resources/Raw/sorular_src3_resimli.json",
            "Resources/Raw/sorular_src4.json",
            "Resources/Raw/sorular_src4_sinav.json",
            "Resources/Raw/sorular_src4_resimli.json",
            "Resources/Raw/sorular_src5.json",
            "Resources/Raw/sorular_src5_sinav.json",
            "Resources/Raw/sorular_src5_resimli.json",
        };

        foreach (var file in files)
        {
            var fullPath = Path.Combine(baseDir, file);
            if (File.Exists(fullPath))
            {
                var content = File.ReadAllText(fullPath, Encoding.UTF8);
                var original = content;

                foreach (var (from, to) in replacements)
                {
                    content = content.Replace(from, to);
                }

                if (content != original)
                {
                    File.WriteAllText(fullPath, content, new UTF8Encoding(false));
                    Console.WriteLine($"Fixed: {file}");
                }
                else
                {
                    Console.WriteLine($"No changes: {file}");
                }
            }
            else
            {
                Console.WriteLine($"Not found: {fullPath}");
            }
        }

        Console.WriteLine("Done!");
    }
}
