$ErrorActionPreference = 'SilentlyContinue'
Get-ChildItem -Path .\LunaVK -Recurse -Filter *.xaml | ForEach-Object {
  $p = $_.FullName
  $bytes = [System.IO.File]::ReadAllBytes($p)
  if ($bytes.Length -eq 0) { return }

  # Пропускаем BOM (UTF‑8, UTF‑16 LE/BE)
  $i = 0
  if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $i = 3 }
  elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) { $i = 2 }
  elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) { $i = 2 }

  # Пропускаем пробелы/таб/CR/LF
  while ($i -lt $bytes.Length -and ($bytes[$i] -eq 0x20 -or $bytes[$i] -eq 0x09 -or $bytes[$i] -eq 0x0D -or $bytes[$i] -eq 0x0A)) { $i++ }

  # Проверяем первый значимый байт
  if ($i -lt $bytes.Length -and [char]$bytes[$i] -ne '<') {
    $head = [System.Text.Encoding]::UTF8.GetString($bytes, 0, [Math]::Min(100, $bytes.Length)).Replace("`r",' ').Replace("`n",' ')
    "BAD|$p|FirstNonWsByte=0x{0:X2}|HEAD={1}" -f $bytes[$i], $head
  }
}