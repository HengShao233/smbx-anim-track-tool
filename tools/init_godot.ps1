$ErrorActionPreference = 'Stop'

$exe = Get-ChildItem -Path './gd_engine' -Filter 'godot_*stable_mono_win64_console.exe' -File -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -First 1
if ($exe) {
  Write-Host ("Godot console exe found: {0}" -f $exe.FullName)
  exit 0
}

New-Item -ItemType Directory -Force './gd_engine' | Out-Null
$page = Invoke-WebRequest -UseBasicParsing https://godotengine.org/download/windows/
$href = ($page.Links | Where-Object { $_.href -match 'Godot_v.*-stable_mono_win64\.zip' } | Select-Object -First 1).href

if (-not $href) {
  $match = [regex]::Match($page.Content, 'https?://[^"\s]*&slug=mono_win64.zip[^"\s]*&platform=windows.64')
  if ($match.Success) {
    $href = $match.Value
  }
}

if (-not $href) { throw 'Cannot find Godot mono win64 download link. Please check https://godotengine.org/download/windows/ layout.' }
if ($href -notmatch '^https?://') { $href = 'https://download.godotengine.org' + $href }


$zip = Join-Path $env:TEMP 'godot_mono_win64.zip'
$extract = Join-Path $env:TEMP 'godot_mono_extract'

Invoke-WebRequest -UseBasicParsing $href -OutFile $zip
if (Test-Path $extract) {
  Remove-Item $extract -Recurse -Force
}
Expand-Archive -Force $zip $extract

$entries = Get-ChildItem -Path $extract
if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) {
  $sourceRoot = $entries[0].FullName
} else {
  $sourceRoot = $extract
}

Copy-Item -Path (Join-Path $sourceRoot '*') -Destination './gd_engine' -Recurse -Force

Remove-Item $zip -Force
Remove-Item $extract -Recurse -Force

