# PowerShell script to fix encoding issues in JSON files
# Fixes mojibake characters and saves as UTF-8 without BOM

$rawPath = "C:\Users\Ceyda\.gemini\antigravity\scratch\SrcSinavUygulamasi\Resources\Raw"
$jsonFiles = Get-ChildItem -Path $rawPath -Filter "sorular*.json"

$utf8NoBom = New-Object System.Text.UTF8Encoding $false

foreach ($file in $jsonFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan
    
    # Read as UTF-8
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    
    $originalContent = $content
    
    # Fix the specific mojibake pattern (en-dash displayed as garbage)
    $content = $content -replace [char]0xE2 + [char]0x80 + [char]0x93, '-'
    $content = $content -replace 'â€"', '-'
    $content = $content -replace 'â€™', "'"
    $content = $content -replace 'â€œ', '"'
    $content = $content -replace 'â€', '"'
    
    if ($content -ne $originalContent) {
        Write-Host "  - Fixed mojibake characters" -ForegroundColor Yellow
    }
    
    # Save as UTF-8 without BOM
    [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
    Write-Host "  - Saved as UTF-8 (no BOM)" -ForegroundColor Green
}

Write-Host "`nAll files processed successfully!" -ForegroundColor Green
