[CmdletBinding()]
param([string]$Payload = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/publish'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root '.work/payload-smoke'
New-Item -ItemType Directory -Force $scratch | Out-Null
foreach ($name in @('FocGrain.exe','FocGrain.runtimeconfig.json','coreclr.dll','Windows.Devices.Midi2.dll','Windows.Devices.Midi2.pri')) {
    if (!(Test-Path (Join-Path $Payload $name))) { throw "Missing payload file: $name" }
}
$config = Get-Content (Join-Path $Payload 'FocGrain.runtimeconfig.json') -Raw | ConvertFrom-Json
if ($config.runtimeOptions.includedFrameworks.Count -ne 2) { throw 'Payload is not self-contained with both .NET and Windows Desktop' }
$report = Join-Path $scratch ('probe-' + [Guid]::NewGuid().ToString('N') + '.ini')
$oldRoot = $env:DOTNET_ROOT
$oldLocalAppData = $env:LOCALAPPDATA
try {
    $env:DOTNET_ROOT = Join-Path $scratch 'no-global-runtime'
    $env:LOCALAPPDATA = Join-Path $scratch ('profile-' + [Guid]::NewGuid().ToString('N'))
    $process = Start-Process -FilePath (Join-Path $Payload 'FocGrain.exe') -ArgumentList @('--check-prerequisites', ('"' + $report + '"')) -Wait -PassThru -WindowStyle Hidden
    if (!(Test-Path $report)) { throw 'CLI probe did not write a report' }
    $body = Get-Content $report -Raw
    $match = [Regex]::Match($body, '(?m)^Code=(\d+)\r?$')
    if (!$match.Success -or [int]$match.Groups[1].Value -ne $process.ExitCode) { throw 'Report/exit code mismatch' }
    if ($process.ExitCode -notin @(0,20,21,22,23,24,25,26)) { throw "Probe failed unexpectedly: $($process.ExitCode)" }
    foreach ($key in @('MidiCode','Midi','Focusrite','Driver')) {
        if ($body -notmatch "(?m)^$key=.+") { throw "Missing report field: $key" }
    }
    $invalid = Start-Process -FilePath (Join-Path $Payload 'FocGrain.exe') -ArgumentList '--unknown-setup-option' -Wait -PassThru -WindowStyle Hidden
    if ($invalid.ExitCode -ne 64) { throw 'Unknown CLI arguments must not start the tray' }
    foreach($mode in @('--repair-midi-service','--after-recovery')) {
        $invalidRecovery = Start-Process -FilePath (Join-Path $Payload 'FocGrain.exe') -ArgumentList @($mode,'invalid') -Wait -PassThru -WindowStyle Hidden
        if($invalidRecovery.ExitCode -ne 64){throw 'Invalid recovery CLI must not access the service'}
    }
    $guideOutput = Join-Path $scratch ('guide-' + [Guid]::NewGuid().ToString('N'))
    $guide = Start-Process -FilePath (Join-Path $Payload 'FocGrain.exe') -ArgumentList @('--guide-preview','en',('"' + $guideOutput + '"')) -PassThru -WindowStyle Hidden
    if (!$guide.WaitForExit(15000)) { $guide.Kill(); throw 'Published guide render timed out' }
    if ($guide.ExitCode -ne 0 -or @(Get-ChildItem $guideOutput -Filter 'step-*.png').Count -ne 4) { throw 'Published wizard pages or embedded screenshots are incomplete' }
    Write-Host 'PASS published offline FC2 wizard (four pages/resources; no MIDI worker)'
    if (Test-Path (Join-Path $env:LOCALAPPDATA 'FocusriteGainTray')) { throw 'Setup probe unexpectedly wrote user settings/log' }
    Write-Host "PASS self-contained payload and prerequisite CLI contract (readiness code $($process.ExitCode); not a hardware test)"
} finally {
    $env:DOTNET_ROOT = $oldRoot
    $env:LOCALAPPDATA = $oldLocalAppData
}
