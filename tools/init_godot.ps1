$ErrorActionPreference = 'Stop'

$exe = Get-ChildItem -Path './gd_engine' -Filter 'godot_*stable_mono_win64_console.exe' -File -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -First 1
if ($exe) {
  Write-Host ("Godot console exe found: {0}" -f $exe.FullName)
  exit 0
}

New-Item -ItemType Directory -Force './gd_engine' | Out-Null
$page = Invoke-WebRequest -UseBasicParsing https://godotengine.org/download/windows/
$href = ($page.Links | Where-Object { $_.href -match 'Godot_v.*_stable_mono_win64\.zip' } | Select-Object -First 1).href
if (-not $href) { throw 'Cannot find Godot mono win64 download link.' }
if ($href -notmatch '^https?://') { $href = 'https://godotengine.org' + $href }

$zip = Join-Path $env:TEMP 'godot_mono_win64.zip'
Invoke-WebRequest -UseBasicParsing $href -OutFile $zip
Expand-Archive -Force $zip './gd_engine'
Remove-Item $zip -Force
