# Project contract

## Product

FocGrain: single Windows tray process for toggling user-selected minimum/maximum hardware input gain through documented FC2 MIDI. Current scope: Analog 1/2, CC12/13, MIDI channel 1, UMP group 0, MIDI 1.0 messages. No automatic hardware discovery or gain readback.

## Architecture

- `Program`: STA message loop, machine-wide device-owner mutex and legacy session mutex.
- `TrayApplicationContext`: UI commands, successful-send state and lifecycle.
- `SettingsDialog`: selected input, ordered minimum/maximum, hotkeys.
- `AppSettings`: JSON configuration, normalization and atomic file replacement.
- `GainState`: pure command state, toggle policy and CC mapping, tested without hardware.
- `WindowsMidiVirtualController`: official virtual transport and packet construction.
- `PrerequisiteProbe` / `PrerequisitePolicy`: setup readiness detection and testable install gate; no endpoints or gain commands.
- `HotkeyManager`: Windows RegisterHotKey lifecycle.
- `Fc2Startup` / `FocusriteControl2`: start the installed FC2 application once after MIDI readiness, respect opt-out and an existing FC2 process; no gain capability or extra Windows startup entry.

## Invariants

No gain commands on startup, settings save, input selection, reconnect or quit. Never restore a last-sent value across runs. Only update command state after send success. Reset state on reconnect or transport failure. Never describe MIDI values as dB or minimum as mute. Settings store presets, not hardware state. No private Focusrite protocol, Core Audio gain substitution or third-party virtual drivers.

## Working with AI assistance

Start with AGENTS.md and this contract. State scope and acceptance criteria before editing. Keep changes reviewable; record assumptions and unverified behavior. Add regression scenarios for changed safety or state logic. Run pure tests and Windows build, then report hardware acceptance separately. Do not mark a task hardware-tested based on mocks or compilation. No commits without user instruction. Update docs when behavior changes.

## Deferred

Additional inputs/models, autostart, automatic discovery, installers and signed binaries require separate design and compatibility evidence. Keep preview integration isolated until the in-box API is available.

Installer contract and distribution flow: [INSTALLER.md](INSTALLER.md). Only installer/user-chosen vendor prerequisites request elevation; normal tray operation remains a single desktop process.

## Native-call boundary and UI

All live MIDI objects are created/used/disposed through BoundedWorker on one background MTA thread. The UI awaits operations; it never synchronously waits for native RPC, SDK cleanup or a callback. One operation at a time, no queued user gain commands, no retries after a native timeout. Connect has a 10-second wait, send/disconnect 3 seconds and quit cleanup 2 seconds. A timeout cannot cancel a native operation already in progress; displayed state becomes unknown and another connection must not be spawned. The app does not automatically restart services or kill FC2. A stuck Windows service can still affect other MIDI clients independently of our UI.

Ui chooses ru from Russian UI cultures and en otherwise, including default worker-thread UI culture. AppIcons owns original embedded multi-resolution marks. Updates uses an asynchronous, bounded unauthenticated GitHub request only after a button click. Version and release behavior are described in RELEASING.md.
