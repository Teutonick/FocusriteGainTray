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
