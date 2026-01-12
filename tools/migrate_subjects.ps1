# JSON Subject Migration Tool
# Bu script Resources/Raw klasöründeki kaynak JSON dosyalarını günceller
# NOT: Bu one-time migration aracıdır, runtime'da çalışmaz

param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$RawFolder = Join-Path $ProjectRoot "Resources\Raw"

# Keyword haritası
$KeywordMap = @{
    "Araç Tekniği" = @(
        "motor", "yağ", "vites", "lastik", "fren", "akü", "şanzıman",
        "kaporta", "amortisör", "direksiyon", "debriyaj", "benzin",
        "mazot", "yakıt", "egzoz", "radyatör", "silecek", "far",
        "ampul", "jant", "balata", "hidrolik", "kayış", "periyodik bakım",
        "araç bakım", "teknik", "arıza", "soğutma", "ısınma"
    )
    "İlkyardım" = @(
        "ilk yardım", "ilkyardım", "kanama", "solunum", "kalp", "nabız",
        "kaza", "yaralı", "bilinç", "kırık", "yanık", "zehirlenme",
        "boğulma", "şok", "tansiyon", "suni solunum", "kalp masajı",
        "turnike", "pansuman", "sargı", "yaralanma", "ambulans",
        "112", "acil", "kurtarma", "temel yaşam", "hayati"
    )
    "Trafik Mevzuatı" = @(
        "levha", "işaret", "geçiş", "hız", "şerit", "sollama",
        "duraklama", "park", "kavşak", "yaya", "sinyalizasyon",
        "trafik ışığı", "kırmızı ışık", "emniyet kemeri", "kask",
        "alkol", "uyuşturucu", "ceza puanı", "sürücü belgesi",
        "ehliyet", "tescil", "plaka", "muayene", "geçiş üstünlüğü",
        "öncelik", "dönüş", "geri geri", "otoyol", "karayolu"
    )
    "Ulaştırma Mevzuatı" = @(
        "hukuk", "sigorta", "belge", "yetki belgesi", "src",
        "taşımacılık", "mevzuat", "kanun", "yönetmelik", "bakanlık",
        "firma", "şirket", "taşıma", "nakliye", "kargo", "yük",
        "tonaj", "kapasite", "tır", "kamyon", "dorse", "konteyner",
        "gümrük", "uluslararası", "transit", "sefer", "ruhsat",
        "vergi", "harç", "ceza", "idari", "ticari", "AETR",
        "çalışma süresi", "dinlenme", "mola", "takograf"
    )
}

function Get-SubjectFromText {
    param([string]$Text)
    
    $lowerText = $Text.ToLowerInvariant()
    
    foreach ($category in $KeywordMap.GetEnumerator()) {
        foreach ($keyword in $category.Value) {
            if ($lowerText.Contains($keyword.ToLowerInvariant())) {
                return $category.Key
            }
        }
    }
    
    return "Genel"
}

function Update-JsonFile {
    param([string]$FilePath)
    
    Write-Host "`n📄 Processing: $FilePath" -ForegroundColor Cyan
    
    try {
        $content = Get-Content $FilePath -Raw -Encoding UTF8
        $questions = $content | ConvertFrom-Json
        
        $stats = @{
            "Araç Tekniği" = 0
            "İlkyardım" = 0
            "Trafik Mevzuatı" = 0
            "Ulaştırma Mevzuatı" = 0
            "Genel" = 0
        }
        
        foreach ($q in $questions) {
            $subject = Get-SubjectFromText -Text $q.soru
            
            # PowerShell'de property yoksa ekle
            if (-not $q.PSObject.Properties["subject"]) {
                $q | Add-Member -NotePropertyName "subject" -NotePropertyValue $subject
            } else {
                $q.subject = $subject
            }
            
            $stats[$subject]++
        }
        
        # JSON'u UTF-8 BOM ile kaydet (orijinal format)
        $jsonOutput = $questions | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($FilePath, $jsonOutput, [System.Text.UTF8Encoding]::new($true))
        
        Write-Host "   ✅ Updated $($questions.Count) questions" -ForegroundColor Green
        Write-Host "   📊 Distribution:" -ForegroundColor Yellow
        foreach ($stat in $stats.GetEnumerator()) {
            if ($stat.Value -gt 0) {
                Write-Host "      - $($stat.Key): $($stat.Value)" -ForegroundColor Gray
            }
        }
        
        return $true
    }
    catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Ana işlem
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   SRC Subject Migration Tool" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "`nSource Folder: $RawFolder" -ForegroundColor White

if (-not (Test-Path $RawFolder)) {
    Write-Host "❌ Resources/Raw folder not found!" -ForegroundColor Red
    exit 1
}

$jsonFiles = Get-ChildItem -Path $RawFolder -Filter "sorular_*.json"
Write-Host "`nFound $($jsonFiles.Count) JSON files to process..." -ForegroundColor White

$successCount = 0
$failCount = 0

foreach ($file in $jsonFiles) {
    if (Update-JsonFile -FilePath $file.FullName) {
        $successCount++
    } else {
        $failCount++
    }
}

Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "   Migration Complete!" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "✅ Success: $successCount files" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "❌ Failed: $failCount files" -ForegroundColor Red
}
Write-Host "`n⚠️  Remember to rebuild the project after migration!" -ForegroundColor Yellow
