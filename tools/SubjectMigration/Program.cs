using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// Subject Migration Tool for SRC Exam JSON files
// This tool adds "Subject" field to questions based on keyword matching

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("========================================");
Console.WriteLine("   SRC Subject Migration Tool (C#)");
Console.WriteLine("========================================");

// Navigate to Resources/Raw folder
string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string rawFolder = Path.Combine(projectRoot, "Resources", "Raw");

Console.WriteLine($"\nProject Root: {projectRoot}");
Console.WriteLine($"Raw Folder: {rawFolder}");

if (!Directory.Exists(rawFolder))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: Resources/Raw folder not found!");
    Console.WriteLine($"Trying alternative path...");
    Console.ResetColor();
    
    // Alternative - direct path
    rawFolder = @"C:\Users\Ceyda\.gemini\antigravity\scratch\SrcSinavUygulamasi\Resources\Raw";
    if (!Directory.Exists(rawFolder))
    {
        Console.WriteLine("Still not found. Exiting.");
        return;
    }
}

// Keyword mapping
var keywordMap = new Dictionary<string, List<string>>
{
    ["Araç Tekniği"] = new List<string> {
        "motor", "yağ", "vites", "lastik", "fren", "akü", "şanzıman",
        "kaporta", "amortisör", "direksiyon", "debriyaj", "benzin",
        "mazot", "yakıt", "egzoz", "radyatör", "silecek", "far",
        "ampul", "jant", "balata", "hidrolik", "kayış", "periyodik bakım",
        "araç bakım", "teknik", "arıza", "soğutma", "ısınma"
    },
    ["İlkyardım"] = new List<string> {
        "ilk yardım", "ilkyardım", "kanama", "solunum", "kalp", "nabız",
        "kaza", "yaralı", "bilinç", "kırık", "yanık", "zehirlenme",
        "boğulma", "şok", "tansiyon", "suni solunum", "kalp masajı",
        "turnike", "pansuman", "sargı", "yaralanma", "ambulans",
        "112", "acil", "kurtarma", "temel yaşam", "hayati"
    },
    ["Trafik Mevzuatı"] = new List<string> {
        "levha", "işaret", "geçiş", "hız", "şerit", "sollama",
        "duraklama", "park", "kavşak", "yaya", "sinyalizasyon",
        "trafik ışığı", "kırmızı ışık", "emniyet kemeri", "kask",
        "alkol", "uyuşturucu", "ceza puanı", "sürücü belgesi",
        "ehliyet", "tescil", "plaka", "muayene", "geçiş üstünlüğü",
        "öncelik", "dönüş", "geri geri", "otoyol", "karayolu"
    },
    ["Ulaştırma Mevzuatı"] = new List<string> {
        "hukuk", "sigorta", "belge", "yetki belgesi", "src",
        "taşımacılık", "mevzuat", "kanun", "yönetmelik", "bakanlık",
        "firma", "şirket", "taşıma", "nakliye", "kargo", "yük",
        "tonaj", "kapasite", "tır", "kamyon", "dorse", "konteyner",
        "gümrük", "uluslararası", "transit", "sefer", "ruhsat",
        "vergi", "harç", "ceza", "idari", "ticari", "aetr",
        "çalışma süresi", "dinlenme", "mola", "takograf"
    }
};

string GetSubject(string questionText)
{
    var lowerText = questionText.ToLowerInvariant();
    
    foreach (var category in keywordMap)
    {
        foreach (var keyword in category.Value)
        {
            if (lowerText.Contains(keyword.ToLowerInvariant()))
            {
                return category.Key;
            }
        }
    }
    
    return "Genel";
}

// Process all sorular_*.json files
var jsonFiles = Directory.GetFiles(rawFolder, "sorular_*.json");
Console.WriteLine($"\nFound {jsonFiles.Length} JSON files to process...\n");

int totalProcessed = 0;
int totalFailed = 0;

foreach (var filePath in jsonFiles)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"📄 Processing: {Path.GetFileName(filePath)}");
    Console.ResetColor();
    
    try
    {
        // Read JSON with UTF-8 BOM support
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var jsonArray = JsonNode.Parse(content)?.AsArray();
        
        if (jsonArray == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   ❌ Failed to parse JSON");
            Console.ResetColor();
            totalFailed++;
            continue;
        }
        
        var stats = new Dictionary<string, int>
        {
            ["Araç Tekniği"] = 0,
            ["İlkyardım"] = 0,
            ["Trafik Mevzuatı"] = 0,
            ["Ulaştırma Mevzuatı"] = 0,
            ["Genel"] = 0
        };
        
        foreach (var item in jsonArray)
        {
            if (item == null) continue;
            
            // Get question text (soru field)
            var questionText = item["soru"]?.GetValue<string>() ?? 
                               item["Soru"]?.GetValue<string>() ?? "";
            
            var subject = GetSubject(questionText);
            
            // Add or update Subject field
            item["Subject"] = subject;
            stats[subject]++;
        }
        
        // Write back with UTF-8 BOM
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var outputJson = jsonArray.ToJsonString(options);
        
        // Write with UTF-8 BOM
        File.WriteAllText(filePath, outputJson, new UTF8Encoding(true));
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✅ Updated {jsonArray.Count} questions");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("   📊 Distribution:");
        Console.ForegroundColor = ConsoleColor.Gray;
        foreach (var stat in stats.Where(s => s.Value > 0))
        {
            Console.WriteLine($"      - {stat.Key}: {stat.Value}");
        }
        Console.ResetColor();
        
        totalProcessed++;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"   ❌ Error: {ex.Message}");
        Console.ResetColor();
        totalFailed++;
    }
}

Console.WriteLine("\n========================================");
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("   Migration Complete!");
Console.ResetColor();
Console.WriteLine("========================================");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"✅ Success: {totalProcessed} files");
Console.ResetColor();
if (totalFailed > 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Failed: {totalFailed} files");
    Console.ResetColor();
}
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n⚠️  Remember to rebuild the project after migration!");
Console.ResetColor();
