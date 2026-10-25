$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$feed = Join-Path $root '.work/feed'
New-Item -ItemType Directory -Force $feed | Out-Null
$name = 'Windows.Devices.Midi2.0.99.92-preview.11.nupkg'
$target = Join-Path $feed $name
# Official Microsoft release asset; never use the DLL bundled with MIDI tools.
Invoke-WebRequest "https://github.com/microsoft/MIDI/releases/download/inbox-preview-11/$name" -OutFile $target
$hash = Get-FileHash $target -Algorithm SHA256
if ($hash.Hash -ne '13AFC6E6DA2ADAC309AB8C174352F46F1C0EBE2D79E65475BD6274DEFB73DF74') {
    throw 'MIDI SDK SHA256 differs from the reviewed Preview 11 asset. Do not restore; review the upstream release.'
}
$hash
Write-Host 'Official preview package downloaded. Review its license and THIRD_PARTY_NOTICES.md before redistribution.'
