param([string]$BaseDir = ".")

function Repair-XamlFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "Skipping (not found): $FilePath"
        return
    }

    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
    $original = $content

    

    $content = [regex]::Replace($content,
        '(<iconPacks:PackIconMaterial\b(?:(?!VerticalAlignment)[^>])*)(/>)',
        '$1 VerticalAlignment="Center"$2')

        
        
    $content = [regex]::Replace($content,
        '(<StackPanel Orientation="Horizontal")(?![^>]*VerticalAlignment)([^>]*>)',
        '$1 VerticalAlignment="Center"$2')

    # TextBlock tags that are DIRECT children in icon+text rows    
    $content = [regex]::Replace($content,
        '([ \t]*<iconPacks:PackIconMaterial[^\n]+\n[ \t]*)(<TextBlock Text="[^"]*"(?:\s+(?:Foreground|FontSize|FontWeight|FontFamily|Margin)="[^"]*")*\s*/>)',
        {
            param($m)
            $iconLine = $m.Groups[1].Value
            $tbTag = $m.Groups[2].Value
            if ($tbTag -match 'VerticalAlignment') {
                return $m.Value
            }
            $fixed = [regex]::Replace($tbTag, '(\s*/>)$', ' VerticalAlignment="Center"$1')
            return $iconLine + $fixed
        })

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($FilePath, $content, [System.Text.Encoding]::UTF8)
        Write-Host "Fixed: $(Split-Path $FilePath -Leaf)"
    } else {
        Write-Host "No changes needed: $(Split-Path $FilePath -Leaf)"
    }
}

$viewsDir = Join-Path $BaseDir "Cobra\UI\Views"
$xamlFiles = Get-ChildItem -Path $viewsDir -Filter "*.xaml" -File

foreach ($f in $xamlFiles) {
    Repair-XamlFile -FilePath $f.FullName
}

Write-Host "`nAll done."
