# Instructions to coding agents

Goal: official MIDI-only Focusrite Scarlett 4i4 input gain controller, single desktop tray process.

DO NOT replace hardware preamp gain commands with Core Audio Windows mic volume/mute. DO NOT add reverse-engineered Focusrite USB/network control protocol. DO NOT require Razer Synapse, AHK or loopMIDI. NO extra third-party MIDI driver.

Target: Windows 11 25H2 build 26200+, Windows MIDI Services app-hosted Virtual Device Transport, `Windows.Devices.Midi2` preview API on 2026-10-24. Update to in-box API when shipped. Use MIDI 1.0 CC12/CC13 on MIDI channel 1. Numeric 0..127 is MIDI value, NOT dB. Gain min is NOT mic mute.

First Codex task: check `dotnet restore` and `dotnet build` on Windows, fix compile errors, log toolchain and exact SDK version. Then test virtual endpoint legacy WinMM MIDI 1.0 visibility in Focusrite Control 2; use official Windows MIDI Services diagnostics. Validate CC input mapping on physical Scarlett hardware, don't declare tested until verified.

Safety: no commands on startup; don't set arbitrary CC to 127. Persist only user configured presets. Don't claim hardware readback: FC2 MIDI CC is send-only. Device should be cleaned up on quit.

Refer to README for an acceptance checklist and upstream docs. The initial source drop is intentionally uncompiled and may need compatibility fixes with the pre-release API.
