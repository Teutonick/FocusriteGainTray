# Project contract

## Product

Single Windows tray process for toggling user-selected minimum/maximum hardware input gain through documented FC2 MIDI. Current scope: Analog 1/2, CC12/13, MIDI channel 1, UMP group 0, MIDI 1.0 messages. No automatic hardware discovery or gain readback.

## Architecture

- `Program`: STA message loop and single-instance mutex.
- `TrayApplicationContext`: UI commands, successful-send state and lifecycle.
- `SettingsDialog`: selected input, ordered minimum/maximum, hotkeys.
- `AppSettings`: JSON configuration, normalization and atomic file replacement.
- `GainState`: pure command state, toggle policy and CC mapping, tested without hardware.
- `WindowsMidiVirtualController`: official virtual transport and packet construction.
- `HotkeyManager`: Windows RegisterHotKey lifecycle.

## Invariants

No gain commands on startup, settings save, input selection, reconnect or quit. Never restore a last-sent value across runs. Only update command state after send success. Reset state on reconnect or transport failure. Never describe MIDI values as dB or minimum as mute. Settings store presets, not hardware state. No private Focusrite protocol, Core Audio gain substitution or third-party virtual drivers.

## Working with AI assistance

Start with AGENTS.md and this contract. State scope and acceptance criteria before editing. Keep changes reviewable; record assumptions and unverified behavior. Add regression scenarios for changed safety or state logic. Run pure tests and Windows build, then report hardware acceptance separately. Do not mark a task hardware-tested based on mocks or compilation. No commits without user instruction. Update docs when behavior changes.

## Deferred

Additional inputs/models, autostart, automatic discovery, installers and signed binaries require separate design and compatibility evidence. Keep preview integration isolated until the in-box API is available.
