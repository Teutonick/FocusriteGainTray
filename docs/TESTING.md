# Validation and acceptance

## Automated

Run `dotnet run --project tests/GainTray.Tests` (.NET 8 SDK or later). This is a dependency-free assertion runner, not a `dotnet test` adapter. Any failed scenario throws and fails the process. It links production model files. Tests cover initial unknown state, first maximum, min/max/mid, channel independence, reset, equal presets, invalid channels/values, CC12/13, JSON round trip, missing channels and reversed/out-of-range presets.

First run `./scripts/Prepare-MidiSdk.ps1` to download the official pinned release NuGet into the local feed. Run restore and Release build for the Windows application with .NET 10 SDK. CI defines both checks; a workflow file is not evidence of a successful CI run. No percentage coverage claim is made. UI, hotkey registration, transport failure paths, packet bytes and device cleanup still require integration checks.

## Manual target-PC checklist

- [ ] Windows 25H2 build 26200+, exact MIDI Services/FC2 versions recorded.
- [ ] Official Microsoft diagnostics show service and virtual transport available.
- [ ] One tray process; second launch does not create another endpoint.
- [ ] Start/save/select/reconnect/quit send no gain commands (observe with official MIDI monitor).
- [ ] Legacy WinMM endpoint appears in FC2; document enumeration diagnostics.
- [ ] Confirm MIDI 1.0 CC12/13, channel 1, group 0 and selected values using official monitor.
- [ ] On physical Scarlett, low test values change Inputs hardware gain independently for inputs 1/2.
- [ ] Select input in settings; cancel does not change settings. Reject minimum above maximum.
- [ ] Left click cycles MAX/MIN, right click opens settings/menu. Unknown/error/mid modes are readable at tray scale and high DPI.
- [ ] Hotkeys work in another focused app; occupied binding produces a visible error.
- [ ] Failed send never displays a new successful command; reconnect returns unknown state.
- [ ] External hardware/FC2 changes do not falsely claim readback.
- [ ] Settings survive restart; corrupt JSON falls back safely and logs an error.
- [ ] Quit removes virtual endpoint; repeated connect/quit leaves no stale endpoints or icon resources.

Physical-device tests and audible checks must be performed by an operator with the actual interface. Record model/generation, versions and observations; do not pre-check these boxes.

## Setup regression (0.2.0)

Run `./scripts/Build-Setup.ps1`, then `./tests/Test-SetupPayload.ps1`. The payload test executes the published self-contained EXE with a deliberately absent global DOTNET_ROOT and isolated LOCALAPPDATA, checks INI/exit-code agreement and unknown-option handling, and verifies that the probe does not create settings/log. It does not install anything. The normal model runner additionally covers missing/old FC2, missing registered driver, unsupported OS, API/service/transport failure and unknown probe errors. `.github/workflows/setup.yml` defines a manually triggered setup build and payload smoke test; its remote execution is not claimed here.

Before public release, use a disposable Windows VM to verify: missing VC -> download/interactive install -> recheck; missing/old FC2 -> vendor installer -> recheck; network/cancel/hash mismatch -> no app installation; missing MIDI -> Windows Update guidance -> no app installation; reboot requested -> stop before app install -> rerun after reboot; clean app install; occupied mutex; update using the same AppId; uninstall preserving shared components and presets; silent missing-prerequisite refusal. The developer PreviewOnly build has irreversible installation steps disabled and is only for layout/flow QA.

## 0.3.0 regression evidence and exclusions

13 model/HTTP/worker scenarios pass: all previous gain/settings cases plus culture fallback, stable release parsing, numeric version comparisons and GitHub failure responses, one-thread MTA ownership, and a deliberately blocked fake native call proving no queued commands, a timeout, rejected retry and bounded shutdown. Network is mocked for update tests.

Settings windows were rendered from the actual WinForms classes in RU and EN, without loading/persisting user settings or starting MIDI. Review their layout separately from full interactive/high-DPI acceptance. Original multi-resolution icons and the EXE's extracted icon were inspected. Final self-contained publish, bilingual/icon-branded Inno compile and payload smoke passed. Full native lifecycle/FC2 acceptance did not pass.

Manual, explicit `FocGrain.exe --midi-smoke report.json` publishes only a FocGrain QA endpoint, enumerates/opens/closes its WinMM MIDI input and disconnects, with bounded waits. It does not call SendGain or send MIDI notes/CC. The first run timed out during connection. With user approval, midisrv was restarted: normal stop became stuck in Stop Pending; an elevated recovery verified the process was exclusively System32/MidiSrv.exe before restarting it. The second QA run created/opened the endpoint and completed WinMM enumeration/open/close, then timed out during endpoint disconnect. This is a FAILED full lifecycle test. Do not label FC2 freeze recovery, cleanup or physical Scarlett operation as passed. Follow-up requires Windows MIDI servicing/diagnostics and a fresh operator test; transport availability alone is insufficient.

Developer-only `--ui-preview en|ru [png-path]` creates a settings preview using defaults without MIDI or user persistence. Normal startup has none of these test effects. Retest actual FC2 preference selection, disconnect/reconnect and quit on a prepared target machine before declaring the reported dual-app freeze resolved.

Extended diagnostic: disconnect also exceeded a 15-second QA wait, so the earlier 3-second result was not merely a short UI deadline. A public-SDK pre-cleanup/removal experiment completed plugin cleanup/removal but still stalled in DisconnectEndpointConnection. That experiment was reverted and is NOT in the shipped controller. Receipts: .work/midi-smoke-extended.json and .work/midi-smoke-cleanup-fresh.json. The SDK/Windows cleanup issue is unresolved; further native lifecycle testing should resume only after a concrete Windows/SDK change or with native diagnostic stacks. A final MIDI-only service recovery was requested after diagnostics; its result is recorded in .work/midi-service-final-reset.json.

## 0.4.0 FC2 setup wizard

The user confirmed that selecting FocGrain and turning OFF Enable pickup makes gain switching work on their setup. This is user-reported manual evidence for preset switching, separate from the earlier native teardown diagnostics; it does not establish all-input/all-model coverage or clean disconnect.

14 unit/model/HTTP/worker scenarios pass, including both wizard locales, mandatory pickup-off wording, port distinction and avoiding confusion between signal meters and gain knobs. `--guide-preview ru|en <directory>` renders all four pages from the actual WinForms wizard without constructing a tray or MIDI worker or loading/writing presets. Both resource images must load, and all eight output pages should be inspected for layout. This preview uses no MIDI endpoint and creates no diagnostic controller.

Before release, manually check --setup on first launch, repeating --setup while the new tray is already running (same process/endpoint, existing wizard activated), opening from Settings/tray/Start menu, screenshot enlargement, Back/Next/Finish/Close, setup's checked finish option and silent-mode skip. CLI render and compilation do not prove live IPC/installer interaction; do not call that tested without running it. Existing production FC2 settings and gain were not modified by the wizard implementation.

0.5.0: 14 model scenarios passed. Test-ProcessLifecycle.ps1 runs isolated no-MIDI hosts, verifies graceful exit, forced fallback, exact-path isolation and process termination. Published payload prerequisite/wizard checks passed. Actual elevated upgrade/uninstall and MIDI-service endpoint cleanup remain manual checks; they were not run against the user's working installation.
