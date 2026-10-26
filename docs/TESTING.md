# Validation and release acceptance

## First stable release: 1.0.0 · 2026-10-26

The owner accepted the application and confirmed its core physical-device scenario: clicking the tray icon switches the selected Scarlett input gain between configured MIN and MAX values. FC2 uses FocGrain, channel 1 / All channels, with Enable pickup OFF. [Exact verified setup](TESTED_SETUP.md).

This acceptance replaces the provisional product status used during 0.x development. The tray now says **Ready to control gain**. Hardware readback is not part of the MIDI command interface: the tray represents the last command sent, while MIDI values 0–127 are not dB and minimum gain is not mute.

## Automated checks

- `dotnet run --project tests/GainTray.Tests`: 29 dependency-free model/HTTP/worker/guide scenarios. Uses production model source; no hardware commands. Covers MIN/MAX state, independent inputs, CC12/13, boundaries, settings normalization/JSON, locales, update responses and bounded native-call waits.
- `./scripts/Build-Setup.ps1`: self-contained Windows x64 publish and bilingual Inno Setup compilation.
- `./tests/Test-SetupPayload.ps1`: published runtime, prerequisite probe, exit codes and four-page offline wizard resources, without starting a MIDI worker.
- `./tests/Test-ProcessLifecycle.ps1`: isolated no-MIDI hosts verify graceful exit, forced fallback, exact-installation-path isolation and process termination.
- `--ui-preview ru|en <png>` and `--guide-preview ru|en <directory>`: render the actual WinForms settings and guide for visual review without sending gain.

## Regression checklist for future changes

Check selected input and ordered presets; both tray buttons; occupied hotkeys; startup without gain sends; settings persistence and corrupt JSON; duplicate launch and wizard IPC; manual/offline/rate-limited updates; scale and locale layout; opt-in Windows startup; upgrade/uninstall closure and current-user data removal; shared component preservation; endpoint lifecycle after repeated exit/start.

VM dependency-install scenarios cover download cancellation, network/hash failure, reboot requests, missing Windows MIDI components and silent-install refusal. These are maintenance checks rather than a provisional status for the accepted gain-switching workflow.

## Development diagnostics

The dated [audit log](AUDIT.md) preserves earlier 0.x toolchain and native MIDI shutdown observations. In particular, an isolated no-gain smoke test previously timed out in a native disconnect call; the application bounds shutdown waits and keeps native work on a background thread. A later owner-confirmed gain-switching acceptance does not turn that earlier diagnostic run into a successful endpoint-cleanup test.

No lifecycle or gain test should restart the shared MIDI service automatically. `--midi-smoke <report>` is an explicit developer diagnostic that creates a QA endpoint and exercises WinMM visibility without sending CC/notes. Use it deliberately, separately from the normal tray workflow.

## 1.0.1 startup regression

18 scenarios include delayed logon connection, bounded retry of returned errors, immediate stop after a native timeout, stopping after success, and cancellation during the startup wait. These tests do not create endpoints. Native restart/reinstall and service recovery are not inferred from these tests; see MIDI_RECOVERY.en.md for the user log and upstream service limitation.

## 1.0.2 ownership and visibility

21 model scenarios include second-owner exclusion, lease release, legacy-owner compatibility and WinMM name matching that rejects QA/physical ports. Test-ProcessLifecycle additionally runs real two-process exclusion/restart tests with isolated test-only lock names, no MIDI. Live read-only WinMM inventories in normal/elevated contexts showed no FocGrain input although the APPDEV software device remained present.

Live final check: one process (PID 3912), one WinMM FocGrain input, service Running, duplicate exits before MIDI. See AUDIT.md for the authorized recovery sequence.

## 1.0.3 FC2 startup

25 scenarios include startup ordering, no launch before MIDI readiness, no duplicate FC2 launch, opt-out, missing installation, launch failure retry, and migration of old settings while preserving explicit opt-out. FC2 startup is tested through injected process/registry adapters; no hardware CC commands are used. RU/EN settings previews check the extra checkbox. A fresh Windows-logon/hardware cycle is an owner check.

## 1.0.4 controlled recovery

29 scenarios include dirty/legacy/forced upgrade classification, service stop/start, bounded stop fallback and refusal to terminate shared/wrong-image/unverified processes. All service tests use fake adapters; actual native service reset requires explicit approval. Test-ProcessLifecycle expects 31 after forced termination using an explicit test PID with user recovery metadata disabled. Invalid recovery CLI arguments are exercised without service access. Installer repair prompts require interactive validation.

The no-MIDI unresponsive host now blocks its test thread indefinitely; direct verification observed a 5.29-second forced-stop path, child exit -1 and helper exit 31. This prevents a self-exiting test host from masquerading as a forced-recovery test.

Approved live 1.0.4 validation: native elevated repair succeeded (shutdown 31, repair 0, service Running, marker cleared). Normal relaunch and WinMM input creation passed. Cross-UAC completion signaling passed with an elevated wait-only stub, leaving one owner. No hardware gain commands were sent. The earlier unresponsive fixture was strengthened so forced-stop checks cannot pass on a self-exiting process; retained process handles make clean exit-code reads reliable.
