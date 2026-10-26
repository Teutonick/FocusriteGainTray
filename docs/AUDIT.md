# Development audit and release acceptance

## 1.0.0 accepted · 2026-10-26

The owner confirms successful physical Scarlett gain MIN/MAX switching by tray click and accepts the first release. Exact versions are recorded in [TESTED_SETUP.md](TESTED_SETUP.md). The dated entries below describe earlier development stages, not the current product acceptance status.

## Historical review · 2026-10-25

Source tree inspected: C# files, project metadata, README, LICENSE and AGENTS.md. The supplied directory has no .git repository, so history/provenance and a Git diff cannot be audited. No commits performed.

MIT already existed with copyright 2026 Focusrite Gain Tray Contributors; retained verbatim. Added independent-project and trademark notices, dependency boundaries, free-distribution conditions and user-facing disclaimer. No copied vendor assets or binaries found among the supplied project files. Names remain compatibility-oriented identifiers; no absolute legal clearance claimed.

Environment: Windows, PowerShell 7.6, installed .NET SDK 8.0.426, SDK commit a021cd4eac; host runtime 10.0.13. Project target net10.0-windows10.0.26100.0, WindowsSDKPackageVersion 10.0.26100.57, CsWinRT 2.2.0, MIDI API 0.99.92-preview.11.

Commands attempted: dotnet restore and dotnet build --no-restore for src/FocusriteGainTray/FocusriteGainTray.csproj. With DOTNET_CLI_HOME, TEMP and TMP redirected to .work, both fail NETSDK1045: SDK 8 cannot target .NET 10. Runtime 10 does not supply SDK 10. App compile/API compatibility was outside that diagnostic run, as does full restored dependency inventory. No hardware commands were sent during this review.

Changes: selected input in settings, explicit ordered minimum/maximum validation, distinct generated tray modes with owned-icon cleanup, pure successful-send state and shared CC mapping. Reconnect/failure resets displayed state. Added model tests and Windows CI definition. Hardware/FC2/WinMM/visual acceptance was outside that diagnostic run (see TESTING.md).

Model validation: dotnet run --project tests/GainTray.Tests passed all 7 scenarios with SDK 8.0.426, including exhaustive values 0..127 on both inputs. TEMP/TMP and CLI home redirected to .work. This does not validate UI, transport or physical hardware.

## Final build result

Installed official .NET SDK 10.0.402 into ignored .work/sdk (no system SDK installation). Initial NETSDK1045 was an environment issue. Restore then exposed NU1101: Windows.Devices.Midi2 is absent from nuget.org. Downloaded the exact official Preview 11 GitHub nupkg, added scripts/Prepare-MidiSdk.ps1 and NuGet.Config with package source mapping.

With the local SDK, local feed and nuget.org, restore succeeded. Release build with --no-restore succeeded: 0 warnings, 0 errors. No MIDI API compile changes were needed. Earlier unsuccessful attempts above are retained as diagnostic history, superseded by this result.

Direct license evidence: Microsoft.Windows.CsWinRT 2.2.0 package LICENSE is MIT with Microsoft copyright; Windows.Devices.Midi2 0.99.92-preview.11 NuSpec declares MIT. Microsoft's Preview 11 release separately publishes developer go-live conditions for app-local API distribution; documented in THIRD_PARTY_NOTICES.md. No executable installer or binary release was published.

Package inventory reports only the two direct NuGet references, with no transitive package references. Inventory refresh in sandbox emitted NU1900 (vulnerability feed unreachable); no vulnerability-clearance claim is made. Reviewed MIDI release asset SHA256: 13AFC6E6DA2ADAC309AB8C174352F46F1C0EBE2D79E65475BD6274DEFB73DF74; preparation script verifies it.

## FocGrain setup implementation · 2026-10-25

Added self-contained .NET 10.0.13 publish and an Inno Setup 6.7.3 Windows x64 setup, version 0.2.0. Application branding, EXE and virtual endpoint are now FocGrain; legacy internal namespace, presets path and single-instance mutex are retained. No gain commands were sent.

Source prerequisites: official Preview 11/APP transport/minimum Windows requirements, official FC2 downloads page and upgrade/driver instructions, Microsoft VC14 redistributable and Inno documentation. The in-box Windows service/APP transport is not installed by the separate Preview Transports installer. Setup therefore offers Windows Update for these failures; it does not download an unrelated MIDI bridge or install a third-party driver.

Downloaded but did not execute the FC2/driver or VC++ installers. Authenticode verification succeeded: FC2 1.1210.0.0 signed by FOCUSRITE AUDIO ENGINEERING LIMITED, VC++ 14.51.36247.0 signed by Microsoft Corporation. SHA-256 pins are in installer/dependencies.json. The official Inno compiler installer was verified (Pyrsys B.V.) and the compiler was placed in .work/inno for building; its version/license is separate from FocGrain MIT.

Validation: 8 model scenarios pass; Release self-contained publish and final Inno compilation succeed. The published-payload smoke test passes: embedded .NET/Windows Desktop, report/exit-code contract, rejected unknown arguments, isolated profile without settings/log writes. An actual preflight outside sandbox returned 0 and found Windows MIDI Services, APP transport, FC2 1.1210.0.0 and registered Focusrite driver package. This is readiness evidence, not physical hardware evidence. A sandbox registry access failure was rerun outside sandbox; unrelated protected uninstall entries are skipped while missing required evidence still blocks readiness.

UI QA used a separately compiled PreviewOnly wizard with file/component installation disabled. MIT, information and component-status pages were inspected; detected prerequisites were shown and their install buttons disabled. No normal installation, driver change, Windows update, endpoint creation, gain command or hardware acceptance was performed during setup testing. That UI-only run did not exercise missing-dependency installation, reboot or VM upgrade/uninstall; physical gain acceptance was subsequently supplied by the owner for 1.0.0.

Output: artifacts/setup/FocGrain-0.2.0-Setup-x64.exe and SHA-256 sidecar; not code-signed or published to a release. Earlier build attempts hit a wrong Inno INI function signature and a preview-held output-file lock; both were resolved. No commits.

## 0.3.0 review · 2026-10-25

Git now exists and origin is https://github.com/Teutonick/FocusriteGainTray.git; previous changes were preserved. No commit/tag/release created. Added original multi-size ICO/PNG/SVG art, bilingual application/setup, central version, on-demand fixed-repository GitHub Releases check and a serialized background MTA native boundary. System UI fallback is English except Russian. The GitHub latest-release request returned 404; checker does not misreport that as up-to-date. Automated remote publishing was not added; the manual release procedure is documented.

Model regression: 13 scenarios pass. Release compilation with .NET SDK 10.0.402 succeeded with 0 warnings/errors; payload smoke and English prerequisite report passed. RU/EN settings layouts and extracted EXE icon were inspected. The user reported both FC2 and FocGrain freezing; existing logs showed prior successful CC sends but did not identify a crash cause.

Live no-gain QA found a genuine native-call stall. Before service reset, connect timed out near session/device creation. The user explicitly authorized restarting only midisrv. Normal stop remained Stop Pending after 20 seconds. Elevated recovery verified exclusive service ownership and the exact System32/MidiSrv.exe image, restarted it and reported Running. After reset, the QA endpoint was created and its WinMM input enumerated/opened/closed successfully; explicit endpoint disconnect then exceeded its 3-second wait. The bounded test exited without any gain/notes. This is not a passing full lifecycle/FC2 test and not proof of an SDK root cause without native stacks. The application guard is tested; full dual-app freeze resolution was outside that diagnostic run. Official SDK Preview 11 sources and Microsoft issue 939 were consulted; no SDK binary, private device protocol or OS feature flag was modified.

No physical gain test, unattended OS update, FC2 reinstall or driver change performed. The final setup remains unsigned. See TESTING.md and RELEASING.md for follow-up requirements.

Final validation for 0.3.0: self-contained publish and bilingual/icon-branded Inno setup compilation passed; 13 regression scenarios and published-payload smoke passed. Both final settings language renders were inspected, with the Russian note shortened to avoid clipping. The service was restored after the diagnostic session; the final admin helper reported midisrv Running. No further stateful MIDI tests were run after that restoration. Repeat-connect is disabled while an endpoint is already live, so opening the tray cannot tear down/recreate a port being selected by FC2. Full native disconnect/FC2 recovery was not established by those diagnostic runs. The failed QA receipts are local .work/midi-smoke-030.json and .work/midi-smoke-after-reset.json.

Extended diagnostic: disconnect also exceeded a 15-second QA wait, so the earlier 3-second result was not merely a short UI deadline. A public-SDK pre-cleanup/removal experiment completed plugin cleanup/removal but still stalled in DisconnectEndpointConnection. That experiment was reverted and is NOT in the shipped controller. Receipts: .work/midi-smoke-extended.json and .work/midi-smoke-cleanup-fresh.json. The SDK/Windows cleanup issue is unresolved; further native lifecycle testing should resume only after a concrete Windows/SDK change or with native diagnostic stacks. A final MIDI-only service recovery was requested after diagnostics; its result is recorded in .work/midi-service-final-reset.json.

## 0.4.0 FC2 onboarding · 2026-10-25

The user supplied screenshots showing Enable pickup enabled, then explicitly confirmed that turning it off with FocGrain selected makes gain toggle work. The later two user-supplied reference images show the correct preferences and input controls. They were copied unchanged into assets/setup/, embedded for offline wizard use and separately attributed in THIRD_PARTY_NOTICES.md. No screenshot is presented as automatic hardware readback.

Added a separate four-page RU/EN wizard, post-install --setup launch (checked by default, skipped in silent mode), tray/settings/Start-menu access and a single-instance request window so repeated wizard requests do not create extra MIDI hosts. Added mandatory pickup-off guidance, clarified Focusrite USB MIDI versus FocGrain, gain knob versus signal meters and MIDI values versus CC controller numbers. Hid the redundant connected-state retry button and clarified the port-open status. The wizard has no MIDI/FC2 automation, preset persistence or automatic gain test.

Validation: 14 model/HTTP/worker scenarios pass. .NET SDK 10.0.402 Release build passed with zero warnings/errors after the nullable font fallback fix. Four pages per language were rendered from the real wizard class; resource images loaded successfully. Russian text wrapping was reviewed and switched to Unicode RichTextBox rendering. No live MIDI ports, FC2 changes, gain sends, service restarts, commits or release publication were performed for this onboarding change. Live single-instance --setup routing and normal installer completion remain separate manual acceptance checks.

## 1.0.1 restart report · 2026-10-26

The owner provided a log where shutdown stops in DisconnectEndpointConnection and the next run times out after CreateVirtualDevice. The pinned generated Preview 11 projection confirms AssociationId has a getter only; the app already keeps Name/ProductInstanceId constant. No persistent-association fix is claimed. The local VirtualMidiTransport binary reports 1.0.15.0. Microsoft issue 1047 / the 2026-10-21 maintainer comment in issue 819 describe a similar virtual-device teardown/service hang. Implemented cancellable startup waiting, retry of returned failures only, a longer bounded connect window, identity logs and accurate recovery guidance. No service reset, live endpoint creation or gain command was performed for this patch.

## 1.0.2 live diagnostic correction · 2026-10-26

The current FocGrain PID 25656 started at 11:13:06 in session 1; midisrv was Running and a FocGrain APPDEV PnP entry was OK. Read-only WinMM inventories in both normal and elevated contexts showed only the physical Focusrite input, no FocGrain input. This establishes a missing client-facing port, rather than just FC2 cache. Source inspection of Microsoft MidiVirtualDeviceCreationConfig.cpp shows ProductInstanceId passed as uniqueIdentifier; VirtualMidiEndpointManager constructs APPDEV/APPPUB device instance ids from it. The previous inference that changing AssociationId itself caused changing Windows endpoint identity was incorrect and has been removed from current guides. Added a machine-wide guard plus legacy session lock, PID logs and WinMM readiness check. No gain commands were sent.

### Authorized live recovery result · 2026-10-26

The owner separately authorized (1) stopping only FocGrain and removing its exact APPDEV software node and (2) restarting only midisrv, including verified dedicated-process recovery if stop stalled. Exact-node removal succeeded but the next single-process CreateVirtualDevice still timed out. After the authorized service restart reported Running, FocGrain PID 3912 created/opened the device and reported WinMM MIDI input visible at 11:30:22.878. Independent WinMM enumeration then showed exactly one FocGrain input. A duplicate normal launch exited with code 0 while PID 3912 remained the sole FocGrain process. No gain commands were sent by these checks; FC2 settings and the Focusrite hardware/driver were not changed. The working repository-built process was left running to avoid another teardown. Receipts are local .work/targeted-repair-result.json, midi-service-reset-102.json and recovery-102-final.json. This confirms current recovery and single-instance behavior, not permanent elimination of the upstream transport issue.

## 1.0.3 Notifier versus FC2 · 2026-10-26

Read-only CIM data: Windows boot 11:39:20, Focusrite Notifier started 11:40:21, Focusrite Control 2 started 11:56:08 as a child of Focusrite Notifier. The owner log shows MIDI sends at 11:56:00-02 before FC2 existed. Windows startup inventory includes Notifier, not FC2. This explains why opening FC2 enabled the chain. Added opt-out automatic FC2 launch after WinMM readiness; ordinary vendor executable launch with no private switches. Existing FC2 is preserved, startup gain is never sent and FC2 launch errors do not close the MIDI endpoint. No services, device nodes or gain levels were changed for this patch.

## 1.0.4 upgrade-induced service hang

The owner's 16:55 log again stops in DisconnectEndpointConnection before a new setup-mode process times out at CreateVirtualDevice. Prior 1.0.3 returned process success even after bounded native shutdown timed out, allowing setup to immediately create a port against a wedged service. Added exit 31 + recovery marker, conservative legacy shutdown classification, interactive installer recovery gate and a UAC one-shot repair command. The normal UI process exits before service repair; an unelevated child waits for successful helper completion before acquiring MIDI ownership. No service modification is performed by tests or normal startup.

### Approved native recovery validation · 2026-10-26

The owner explicitly approved closing the stuck FocGrain and testing the compiled elevated helper. The 1.0.4 shutdown helper returned 31 for the installed legacy process; --repair-midi-service 0 returned 0, midisrv returned Running with PID 34216, and the recovery marker cleared. A normal 1.0.4 process (PID 27128) then followed --after-recovery, created its endpoint and reported WinMM input visible at 17:38:06.864. Independent enumeration confirmed exactly one FocGrain input and one owner. No gain commands were sent by validation. The completion handshake was also exercised against an elevated no-service stub: readiness signal true, waiter exit 0, original owner unchanged. Current receipts are local .work/native-recovery-104.json, recovery-104-handoff.json, recovery-104-uac-handoff.json and recovery-104-final.json. This validates the recovery backend and handoff; full interactive setup UI was not driven in this check.
