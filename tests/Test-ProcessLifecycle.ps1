$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$payload = Join-Path $root 'artifacts/publish'
$sandbox = Join-Path $root ('.work/lifecycle-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $sandbox | Out-Null
Copy-Item "$payload/*" $sandbox -Recurse
$exe = Join-Path $sandbox 'FocGrain.exe'
$children = @()
try {
    foreach ($mode in @('host','unresponsive')) {
        $marker = Join-Path $sandbox "$mode.ready"
        $hostProcess = Start-Process $exe -ArgumentList "--lifecycle-test-$mode", "`"$marker`"" -PassThru -WindowStyle Hidden
        $children += $hostProcess
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (!(Test-Path $marker) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
        if (!(Test-Path $marker)) { throw 'Lifecycle host did not become ready' }
        # Wrong installation path must leave this process untouched.
        $wrong = Start-Process $exe -ArgumentList '--shutdown-installation', "`"$sandbox/other/FocGrain.exe`"" -PassThru -WindowStyle Hidden
        $children += $wrong
        if (!$wrong.WaitForExit(15000) -or $wrong.ExitCode -ne 0 -or $hostProcess.HasExited) { throw 'Installation isolation failed' }
        $closer = Start-Process $exe -ArgumentList '--shutdown-installation', "`"$exe`"" -PassThru -WindowStyle Hidden
        $children += $closer
        if (!$closer.WaitForExit(15000) -or $closer.ExitCode -ne 0 -or !$hostProcess.WaitForExit(1000)) { throw 'Shutdown left a process running' }
        if ($mode -eq 'host' -and !(Test-Path "$marker.closed")) { throw 'Graceful shutdown was not delivered' }
        if ($mode -eq 'unresponsive' -and (Test-Path "$marker.closed")) { throw 'Forced shutdown test did not exercise fallback' }
        Write-Host "PASS $mode shutdown and exact installation path isolation (no MIDI)"
    }
    foreach ($round in @('first','restart')) {
        $marker = Join-Path $sandbox "$round.instance"
        $first = Start-Process $exe -ArgumentList '--instance-test-host', "`"$marker`"" -PassThru -WindowStyle Hidden
        $children += $first
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (!(Test-Path $marker) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
        if (!(Test-Path $marker)) { throw 'Instance owner did not become ready' }
        $duplicateMarker = Join-Path $sandbox "$round.duplicate"
        $duplicate = Start-Process $exe -ArgumentList '--instance-test-host', "`"$duplicateMarker`"" -PassThru -WindowStyle Hidden
        $children += $duplicate
        if (!$duplicate.WaitForExit(10000) -or $duplicate.ExitCode -ne 73 -or (Test-Path $duplicateMarker) -or $first.HasExited) { throw 'Second instance entered the application' }
        $closer = Start-Process $exe -ArgumentList '--shutdown-installation', "`"$exe`"" -PassThru -WindowStyle Hidden
        $children += $closer
        if (!$closer.WaitForExit(15000) -or $closer.ExitCode -ne 0 -or !$first.WaitForExit(1000)) { throw 'Instance owner failed to quit' }
        Write-Host "PASS $round two-process single-instance exclusion (no MIDI)"
    }
} finally {
    foreach ($child in $children) { if (!$child.HasExited) { $child.Kill(); $child.WaitForExit() } }
    # Unique test-owned folder, verified inside the workspace before removal.
    if ([IO.Path]::GetFullPath($sandbox).StartsWith([IO.Path]::GetFullPath((Join-Path $root '.work')) + [IO.Path]::DirectorySeparatorChar)) {
        Remove-Item -LiteralPath $sandbox -Recurse -Force
    }
}
