# FocGrain

[Русский](README.md)

A small independent Windows tray app for switching a supported audio input between two gain presets. Choose Analog 1 or 2, set minimum/maximum MIDI values, and left-click the tray icon to toggle. Right-click for settings, MIDI connection controls, updates and Quit.

FocGrain uses documented Focusrite Control 2 MIDI commands. It is not made, sponsored or endorsed by Focusrite. Focusrite, Scarlett and Focusrite Control 2 identify compatibility and belong to their respective owners. The original FocGrain icon is independent artwork; user-supplied FC2 screenshots illustrate setup, and rights to the depicted vendor UI/marks remain with their owners.

## Install and use

1. Run **FocGrain-0.5.0-Setup-x64.exe** on Windows 11 25H2 x64 (build 26200+).
2. Setup checks Visual C++ x64, Windows MIDI Services, Virtual Device App Transport, FC2 and driver-package registration. .NET is included.
3. If required, use the wizard to download the verified official Visual C++ or FC2 installer. Vendor license/driver/restart choices remain interactive. System MIDI components come through Windows Update; the wizard links to it and rechecks readiness.
4. Use the separate [screenshot setup wizard](docs/FC2_SETUP.en.md) at the end of setup, from the tray/Settings, or its Start menu shortcut. Start FocGrain. In FC2, select **More options → Preferences → Application → MIDI Control: FocGrain**, channel 1 or All channels. **Turn OFF Enable pickup**; otherwise preset changes may be ignored.
5. Open FocGrain Settings from the tray, select the correct input and deliberately choose low test values before checking the physical gain knob.

Russian and English follow the Windows UI language. Any other language defaults to English. Setup is currently unsigned. Closing or saving Settings does not send gain commands; starting the app does not restore or send a gain value. Autostart is not enabled.

The teal minus/low pointer and amber plus/high pointer identify minimum/maximum in the tray. Hover for the last command sent. The first click after connection selects the configured maximum. `Ctrl+Alt+M` toggles presets; `Ctrl+Alt+Up/Down` adjusts by the configured step. Shortcuts are configurable.

**MIDI 0–127 is not dB. Minimum gain is not mute.** There is no hardware gain readback: changing gain elsewhere does not update FocGrain. Successful MIDI send is not proof that FC2 received it. Current scope is Analog 1/2 (CC12/13, MIDI channel 1); support depends on the actual interface model and FC2 capabilities. The user confirmed gain toggling after disabling pickup; full physical-device regression remains pending.

## Updates and MIDI problems

Use **Check for updates…** in the tray menu or Settings. FocGrain checks public published stable releases of [Teutonick/FocusriteGainTray](https://github.com/Teutonick/FocusriteGainTray/releases). It shows the current/new version and opens the release page only when requested. No background checks, credentials, automatic downloads or automatic installation. A Git tag without a published Release is not an update. A 404/offline/rate-limit result is not reported as “up to date”.

MIDI calls run on one background MTA worker with bounded waits. No delayed click/hotkey commands are queued. After a native-call timeout, state becomes unknown and retries are blocked until app restart; Quit waits at most two seconds for native cleanup. This protects FocGrain UI responsiveness but cannot repair a deadlocked Windows MIDI service or guarantee FC2 responsiveness. Update Windows and review [MIDI troubleshooting](docs/TESTING.md). Shared-service restart interrupts other MIDI clients and must be an operator decision.

Presets/log: `%LOCALAPPDATA%\FocusriteGainTray`. Uninstall leaves presets and shared FC2/drivers/Visual C++ in place. No Razer Synapse, AutoHotkey, loopMIDI, private USB protocol or Windows microphone-volume substitution.

## Build and license

.NET 10 SDK and Inno Setup 6.7.3 are required to build setup. Run `./scripts/Prepare-MidiSdk.ps1`, `dotnet run --project tests/GainTray.Tests`, then `./scripts/Build-Setup.ps1`. [Installer](docs/INSTALLER.md), [architecture](docs/PROJECT.md), [validation](docs/TESTING.md), [release/version process](docs/RELEASING.md), [audit](docs/AUDIT.md).

Project source and original icon assets are [MIT licensed](LICENSE): use, modify, redistribute and sell copies while retaining the copyright and license notice. MIT does not grant rights to third-party trademarks, FC2, drivers or Microsoft components. [Dependency and Preview redistribution terms](THIRD_PARTY_NOTICES.md) remain applicable.

Provided **AS IS**, without warranty. To the extent permitted by applicable law, authors are not liable for damage, interrupted recording, data loss or other consequences. Full warranty/liability terms are in LICENSE. Lower monitoring levels before testing gain; this is not hearing protection or a guaranteed microphone mute.

Windows startup is opt-in in Settings and disabled by default. Setup offers to close FocGrain before upgrading. Uninstall removes the current user's settings, logs and startup entry; shared Windows/Focusrite components remain installed.
