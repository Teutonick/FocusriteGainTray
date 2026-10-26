# Explicit, administrator-only repair. Never called by application or installer.
# Removes exactly FocGrain's own APPDEV software node; no driver/service/hardware changes.
[CmdletBinding(SupportsShouldProcess, ConfirmImpact='High')]
param()
$ErrorActionPreference = 'Stop'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this explicit repair as Administrator.' }
if(Get-Process -Name FocGrain -ErrorAction SilentlyContinue) { throw 'Close every FocGrain process before repairing its software device.' }
$target='SWD\MIDISRV\MIDIU_APPDEV_FOCUSRITEGAINTRAY_CONTROLLER_01'
$device=Get-PnpDevice -InstanceId $target -ErrorAction SilentlyContinue
if(!$device) { Write-Output 'FocGrain APPDEV node is already absent; no changes.'; return }
if($device.InstanceId -ne $target -or $device.FriendlyName -notlike 'FocGrain*') { throw 'Unexpected device identity. Nothing removed.' }
if($PSCmdlet.ShouldProcess($target,'Remove only the FocGrain software device (not Focusrite hardware)')) {
    & "$env:SystemRoot/System32/pnputil.exe" /remove-device $target
    if($LASTEXITCODE -notin @(0,3010)) { throw "PnP removal failed with code $LASTEXITCODE" }
    Write-Output "FocGrain software-node removal requested. Exit code: $LASTEXITCODE. MIDI Services and FC2 were not restarted."
}
