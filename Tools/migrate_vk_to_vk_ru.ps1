<#
PowerShell script to replace vk.com -> vk.ru across repository.
Run from repository root (Src folder level recommended):
    powershell -ExecutionPolicy Bypass -File .\Src\tools\migrate_vk_to_vk_ru.ps1

Behavior:
- Scans text files (common source/text extensions) recursively excluding "bin","obj",".git","packages","Src/tools" directories.
- Applies ordered regex replacements (specific hostnames first, then general vk.com -> vk.ru).
- Creates a .bak backup for each modified file (original preserved).
- Produces CSV report at Src/Doc/Migration_changes.csv with changed files and applied patterns.

WARNING: This is a destructive repo-wide replace (backups saved as .bak). Review the CSV and backups before committing.
#>

Param(
    [string]$Root = (Get-Location).Path,
    [string]$ReportPath = "Src/Doc/Migration_changes.csv"
)

Write-Output "Starting VK domain migration in: $Root"

# File extensions to process
$exts = @('.cs','.xaml','.html','.htm','.js','.json','.xml','.config','.txt','.css','.csproj','.sln','.resx','.aspx','.vb','.ps1')

# Directories (or path segments) to exclude
$excludeSegments = @('\\bin\\','\\obj\\','\\.git\\','\\packages\\','\\tools\\')

# Ordered replacements (regex pattern -> replacement)
$replacements = @(
    @{pat = '(?i)https?://oauth\.vk\.com'; rep = 'https://oauth.vk.ru'},
    @{pat = '(?i)https?://api\.vk\.com'; rep = 'https://api.vk.ru'},
    @{pat = '(?i)https?://login\.vk\.com'; rep = 'https://login.vk.ru'},
    @{pat = '(?i)https?://m\.vk\.com'; rep = 'https://m.vk.ru'},
    @{pat = '(?i)//oauth\.vk\.com'; rep = '//oauth.vk.ru'},
    @{pat = '(?i)//api\.vk\.com'; rep = '//api.vk.ru'},
    @{pat = '(?i)//login\.vk\.com'; rep = '//login.vk.ru'},
    @{pat = '(?i)//m\.vk\.com'; rep = '//m.vk.ru'},
    @{pat = '(?i)oauth\.vk\.com'; rep = 'oauth.vk.ru'},
    @{pat = '(?i)api\.vk\.com'; rep = 'api.vk.ru'},
    @{pat = '(?i)login\.vk\.com'; rep = 'login.vk.ru'},
    @{pat = '(?i)m\.vk\.com'; rep = 'm.vk.ru'},
    # General host replacement (last)
    @{pat = '(?i)vk\.com'; rep = 'vk.ru'}
)

# Prepare report
$report = @()

Write-Output "Enumerating files..."

# Find files with desired extensions, excluding specified directories and the script itself
$files = Get-ChildItem -Path $Root -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $exts -contains $_.Extension.ToLower()
} | Where-Object {
    $full = $_.FullName
    $lower = $full.ToLower()

    # Exclude any path that contains any of the exclude segments
    foreach ($seg in $excludeSegments) {
        if ($lower -like "*${seg.ToLower()}*") { return $false }
    }

    # Also avoid modifying this tool script itself
    if ($full -ieq (Join-Path $PSScriptRoot (Split-Path -Leaf $MyInvocation.MyCommand.Path))) { return $false }

    return $true
}

Write-Output ("Files found: {0}" -f $files.Count)

foreach ($file in $files)
{
    try
    {
        $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
    }
    catch
    {
        Write-Output "Skipping binary/unreadable file: $($file.FullName)"
        continue
    }

    if ($null -eq $text) { continue }

    $original = $text
    $applied = @()

    foreach ($r in $replacements)
    {
        $pattern = $r.pat
        $rep = $r.rep
        try {
            $newText = [regex]::Replace($text, $pattern, $rep)
        }
        catch {
            Write-Output "Regex replace failed for pattern $pattern on file $($file.FullName): $($_.Exception.Message)"
            continue
        }

        if ($newText -ne $text)
        {
            $applied += $pattern
            $text = $newText
        }
    }

    if ($text -ne $original)
    {
        # backup
        $bak = $file.FullName + '.bak'
        try { Copy-Item -LiteralPath $file.FullName -Destination $bak -Force -ErrorAction Stop } catch { Write-Output "Warning: failed to create backup for $($file.FullName): $($_.Exception.Message)" }
        try { Set-Content -LiteralPath $file.FullName -Value $text -Encoding UTF8 }
        catch { Write-Output "Failed to write file: $($file.FullName): $($_.Exception.Message)"; continue }

        $report += [PSCustomObject]@{
            File = $file.FullName.Replace('\','/')
            Patterns = ($applied -join ';')
            Backup = $bak.Replace('\','/')
            Timestamp = (Get-Date).ToString('o')
        }
        Write-Output "Patched: $($file.FullName) - Patterns: $(($applied) -join ',')"
    }
}

# Write CSV report
$reportDir = Split-Path -Path $ReportPath -Parent
if (!(Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
$report | Export-Csv -Path $ReportPath -NoTypeInformation -Encoding UTF8

Write-Output "Migration complete. Report saved to: $ReportPath"
Write-Output "Backups with .bak suffix were created next to modified files. Review before commit."

