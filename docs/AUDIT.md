# Review record · 2026-10-25

Source tree inspected: C# files, project metadata, README, LICENSE and AGENTS.md. The supplied directory has no .git repository, so history/provenance and a Git diff cannot be audited. No commits performed.

MIT already existed with copyright 2026 Focusrite Gain Tray Contributors; retained verbatim. Added independent-project and trademark notices, dependency boundaries, free-distribution conditions and user-facing disclaimer. No copied vendor assets or binaries found among the supplied project files. Names remain compatibility-oriented identifiers; no absolute legal clearance claimed.

Environment: Windows, PowerShell 7.6, installed .NET SDK 8.0.426, SDK commit a021cd4eac; host runtime 10.0.13. Project target net10.0-windows10.0.26100.0, WindowsSDKPackageVersion 10.0.26100.57, CsWinRT 2.2.0, MIDI API 0.99.92-preview.11.

Commands attempted: dotnet restore and dotnet build --no-restore for src/FocusriteGainTray/FocusriteGainTray.csproj. With DOTNET_CLI_HOME, TEMP and TMP redirected to .work, both fail NETSDK1045: SDK 8 cannot target .NET 10. Runtime 10 does not supply SDK 10. App compile/API compatibility remains unverified, as does full restored dependency inventory. No hardware commands were sent during this review.

Changes: selected input in settings, explicit ordered minimum/maximum validation, distinct generated tray modes with owned-icon cleanup, pure successful-send state and shared CC mapping. Reconnect/failure resets displayed state. Added model tests and Windows CI definition. Hardware/FC2/WinMM/visual acceptance remains pending (see TESTING.md).

Model validation: dotnet run --project tests/GainTray.Tests passed all 7 scenarios with SDK 8.0.426, including exhaustive values 0..127 on both inputs. TEMP/TMP and CLI home redirected to .work. This does not validate UI, transport or physical hardware.

## Final build result

Installed official .NET SDK 10.0.402 into ignored .work/sdk (no system SDK installation). Initial NETSDK1045 was an environment issue. Restore then exposed NU1101: Windows.Devices.Midi2 is absent from nuget.org. Downloaded the exact official Preview 11 GitHub nupkg, added scripts/Prepare-MidiSdk.ps1 and NuGet.Config with package source mapping.

With the local SDK, local feed and nuget.org, restore succeeded. Release build with --no-restore succeeded: 0 warnings, 0 errors. No MIDI API compile changes were needed. Earlier unsuccessful attempts above are retained as diagnostic history, superseded by this result.

Direct license evidence: Microsoft.Windows.CsWinRT 2.2.0 package LICENSE is MIT with Microsoft copyright; Windows.Devices.Midi2 0.99.92-preview.11 NuSpec declares MIT. Microsoft's Preview 11 release separately publishes developer go-live conditions for app-local API distribution; documented in THIRD_PARTY_NOTICES.md. No executable installer or binary release was published.

Package inventory reports only the two direct NuGet references, with no transitive package references. Inventory refresh in sandbox emitted NU1900 (vulnerability feed unreachable); no vulnerability-clearance claim is made. Reviewed MIDI release asset SHA256: 13AFC6E6DA2ADAC309AB8C174352F46F1C0EBE2D79E65475BD6274DEFB73DF74; preparation script verifies it.
