[CmdletBinding()]
param(
    [string]$Dotnet = '',
    [string]$ISCC = '',
    [string]$Version = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!$Version) { $Version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be major.minor.patch' }
if ($env:GITHUB_REF_TYPE -eq 'tag' -and $env:GITHUB_REF_NAME -ne "v$Version") { throw 'Git tag does not match Directory.Build.props version' }
if (!$Dotnet) {
    $localSdk = Join-Path $root '.work/sdk/dotnet.exe'
    $Dotnet = if (Test-Path $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
if (!$ISCC) {
    $localCompiler = Join-Path $root '.work/inno/ISCC.exe'
    $ISCC = if (Test-Path $localCompiler) { $localCompiler } else { (Get-Command ISCC.exe -ErrorAction Stop).Source }
}
$env:DOTNET_CLI_HOME = Join-Path $root '.work/cli'
$env:TEMP = Join-Path $root '.work/temp'
$env:TMP = $env:TEMP
$env:NUGET_PACKAGES = Join-Path $root '.work/packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
New-Item -ItemType Directory -Force $env:TEMP, $env:DOTNET_CLI_HOME | Out-Null
$payload = Join-Path $root 'artifacts/publish'
$output = Join-Path $root 'artifacts/setup'
$generated = Join-Path $root '.work/setup'
New-Item -ItemType Directory -Force $payload, $output, $generated | Out-Null
# Clear only this script's fixed publish folder: stale runtime/preview DLLs must not ship.
$resolvedPayload = [IO.Path]::GetFullPath($payload)
if ($resolvedPayload -ne [IO.Path]::GetFullPath((Join-Path $root 'artifacts/publish'))) { throw 'Unexpected publish path' }
Get-ChildItem -LiteralPath $resolvedPayload -Force | Remove-Item -Recurse -Force
if (!(Test-Path (Join-Path $root '.work/feed/Windows.Devices.Midi2.0.99.92-preview.11.nupkg'))) {
    & (Join-Path $PSScriptRoot 'Prepare-MidiSdk.ps1')
}
$project = Join-Path $root 'src/FocusriteGainTray/FocusriteGainTray.csproj'
& $Dotnet publish $project -c Release -r win-x64 --self-contained true -o $payload "-p:Version=$Version" -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false -p:PublishSingleFile=false
if ($LASTEXITCODE) { throw "dotnet publish failed: $LASTEXITCODE" }
foreach ($file in @('FocGrain.exe', 'coreclr.dll', 'hostfxr.dll', 'Windows.Devices.Midi2.dll', 'Windows.Devices.Midi2.pri')) {
    if (!(Test-Path (Join-Path $payload $file))) { throw "Incomplete self-contained payload: $file" }
}
foreach ($file in @('LICENSE','README.md','README.en.md','THIRD_PARTY_NOTICES.md','CHANGELOG.md')) {
    Copy-Item (Join-Path $root $file) $payload
}
Copy-Item (Join-Path $root 'docs') (Join-Path $payload 'docs') -Recurse -Force
$artwork = Join-Path $payload 'assets'
New-Item -ItemType Directory -Force $artwork | Out-Null
Copy-Item (Join-Path $root 'assets/FocGrain.png') $artwork
Copy-Item (Join-Path $root 'assets/setup') (Join-Path $artwork 'setup') -Recurse -Force
$notices = Join-Path $payload 'third-party'
New-Item -ItemType Directory -Force $notices | Out-Null
Copy-Item (Join-Path $env:NUGET_PACKAGES 'microsoft.windows.cswinrt/2.2.0/LICENSE') (Join-Path $notices 'CsWinRT-LICENSE.txt')
Copy-Item (Join-Path $env:NUGET_PACKAGES 'microsoft.windows.cswinrt/2.2.0/NOTICE.txt') (Join-Path $notices 'CsWinRT-NOTICE.txt')
Copy-Item (Join-Path (Split-Path $ISCC -Parent) 'license.txt') (Join-Path $notices 'InnoSetup-LICENSE.txt')
# Preserve license/notice files supplied with the exact .NET runtime packages selected by publish.
foreach ($runtime in @('microsoft.netcore.app.runtime.win-x64', 'microsoft.windowsdesktop.app.runtime.win-x64')) {
    Get-ChildItem (Join-Path $env:NUGET_PACKAGES $runtime) -Recurse -File |
        Where-Object Name -Match '^(LICENSE|THIRD-PARTY-NOTICES)(\..+)?$' |
        ForEach-Object { Copy-Item $_.FullName (Join-Path $notices ($runtime + '-' + $_.Name)) }
}
$manifest = Get-Content (Join-Path $root 'installer/dependencies.json') -Raw | ConvertFrom-Json
foreach ($item in @($manifest.focusrite, $manifest.vcRuntime)) {
    if ($item.sha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Invalid prerequisite hash' }
    if ($item.version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Invalid prerequisite version' }
}
if ($manifest.focusrite.url -ne 'https://releases.focusrite.com/com.focusrite.focusrite-control/latest/Focusrite-Control-2.exe' -or
    $manifest.vcRuntime.url -ne 'https://aka.ms/vc14/vc_redist.x64.exe') { throw 'Review prerequisite source before changing its URL' }
$vc = [Version]$manifest.vcRuntime.version
$defines = @(
    "#define AppVersion `"$Version`"",
    "#define FCVersion `"$($manifest.focusrite.version)`"",
    "#define FCUrl `"$($manifest.focusrite.url)`"",
    "#define FCHash `"$($manifest.focusrite.sha256)`"",
    "#define VCVersion `"$($manifest.vcRuntime.version)`"",
    "#define VCUrl `"$($manifest.vcRuntime.url)`"",
    "#define VCHash `"$($manifest.vcRuntime.sha256)`"",
    "#define VCMinor $($vc.Minor)", "#define VCBuild $($vc.Build)"
)
$definesPath = Join-Path $generated 'Dependencies.iss'
$defines | Set-Content $definesPath -Encoding utf8BOM
& $ISCC /Qp "/DPayloadDir=$payload" "/DOutputDir=$output" "/DDependencyDefines=$definesPath" (Join-Path $root 'installer/FocGrain.iss')
if ($LASTEXITCODE) { throw "Inno Setup compile failed: $LASTEXITCODE" }
$setup = Join-Path $output "FocGrain-$Version-Setup-x64.exe"
$hash = Get-FileHash $setup -Algorithm SHA256
"$($hash.Hash)  $([IO.Path]::GetFileName($setup))" | Set-Content "$setup.sha256" -Encoding ascii
Write-Host "Built $setup (unsigned; not installed)."
