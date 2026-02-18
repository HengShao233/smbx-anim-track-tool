$ErrorActionPreference = 'Stop'

$exe = Get-ChildItem -Path './gd_engine' -Filter 'godot_*stable_mono_win64.exe' -File | Sort-Object Name | Select-Object -First 1
if (-not $exe) { throw 'Godot console exe not found in ./gd_engine' }

& $exe.FullName './gd_project/project.godot'
