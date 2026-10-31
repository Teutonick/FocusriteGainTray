# FocGrain

[Русский](README.ru.md)

A small independent Windows tray app for switching a supported audio input between two gain presets. Choose Analog 1 or 2, set minimum/maximum MIDI values, and left-click the tray icon to toggle. Right-click for settings, MIDI connection controls, updates and Quit.

FocGrain uses documented Focusrite Control 2 MIDI commands. It is not made, sponsored or endorsed by Focusrite. Focusrite, Scarlett and Focusrite Control 2 identify compatibility and belong to their respective owners. The original FocGrain icon is independent artwork; user-supplied FC2 screenshots illustrate setup, and rights to the depicted vendor UI/marks remain with their owners.

## First stable release — verified setup

**1.0.0 is the first accepted release.** The owner confirmed that clicking the tray icon switches the selected input gain between the configured MIN and MAX values.

| Component | Verified version |
| --- | --- |
| Interface | Scarlett 4i4 4th Gen |
| Windows | Windows 11 Pro 26H2 x64, **26300.9711** |
| Focusrite Control 2 | **1.1210.0.0** |
| Focusrite Audio Drivers | **4.150.0.432** |
| Windows MIDI Services binary | **10.0.26100.8875** |
| App-local MIDI SDK | **0.99.92-preview.11**, DLL **1.0.16.0** |
| Visual C++ x64 / bundled .NET | **14.51.36247.00** / **10.0.13** |

FC2: select **FocGrain**, channel **1 / All channels**, and turn **Enable pickup OFF**. [Full setup and acceptance record](docs/TESTED_SETUP.md).

## Install and use

1. Run **FocGrain-1.0.4-Setup-x64.exe** on Windows 11 25H2 x64 (build 26200+).
2. Setup checks Visual C++ x64, Windows MIDI Services, Virtual Device App Transport, FC2 and driver-package registration. .NET is included.
3. If required, use the wizard to download the verified official Visual C++ or FC2 installer. Vendor license/driver/restart choices remain interactive. System MIDI components come through Windows Update; the wizard links to it and rechecks readiness.
4. Use the separate [screenshot setup wizard](docs/FC2_SETUP.en.md) at the end of setup, from the tray/Settings, or its Start menu shortcut. Start FocGrain. In FC2, select **More options → Preferences → Application → MIDI Control: FocGrain**, channel 1 or All channels. **Turn OFF Enable pickup**; otherwise preset changes may be ignored.
5. Open FocGrain Settings from the tray, select the correct input and deliberately choose low test values before checking the physical gain knob.

Russian and English follow the Windows UI language. Any other language defaults to English. Setup is currently unsigned. Closing or saving Settings does not send gain commands; starting the app does not restore or send a gain value. Autostart is disabled by default and can be enabled in Settings.

The gray dot and subdued pointer identify MIN; the bright orange plus/high pointer identifies MAX. MIN is a gain preset, not a disconnected or muted input. Hover for the last command sent. The first click after connection selects the configured maximum. `Ctrl+Alt+M` toggles presets; `Ctrl+Alt+Up/Down` adjusts by the configured step. Shortcuts are configurable.

**MIDI 0–127 is not dB. Minimum gain is not mute.** There is no hardware gain readback: changing gain elsewhere does not update FocGrain. Current scope is Analog 1/2 (CC12/13, MIDI channel 1); support depends on the actual interface model and FC2 capabilities. MIN/MAX gain switching is confirmed on the setup below.

## Updates and MIDI problems

Use **Check for updates…** in the tray menu or Settings. FocGrain checks public published stable releases of [Teutonick/FocusriteGainTray](https://github.com/Teutonick/FocusriteGainTray/releases). It shows the current/new version and opens the release page only when requested. No background checks, credentials, automatic downloads or automatic installation. A Git tag without a published Release is not an update. A 404/offline/rate-limit result is not reported as “up to date”.

MIDI calls run on one background MTA worker with bounded waits. No delayed click/hotkey commands are queued. After a native-call timeout, state becomes unknown and retries are blocked until app restart; Quit waits at most two seconds for native cleanup. This protects FocGrain UI responsiveness but cannot repair a deadlocked Windows MIDI service or guarantee FC2 responsiveness. Update Windows and review [MIDI troubleshooting](docs/TESTING.md). Shared-service restart interrupts other MIDI clients and must be an operator decision.

Presets/log: `%LOCALAPPDATA%\FocusriteGainTray`. Uninstall removes presets, logs and the startup entry for the current user; shared FC2/drivers/Visual C++ remain installed. No Razer Synapse, AutoHotkey, loopMIDI, private USB protocol or Windows microphone-volume substitution.

## Build and license

.NET 10 SDK and Inno Setup 6.7.3 are required to build setup. Run `./scripts/Prepare-MidiSdk.ps1`, `dotnet run --project tests/GainTray.Tests`, then `./scripts/Build-Setup.ps1`. [Installer](docs/INSTALLER.md), [architecture](docs/PROJECT.md), [validation](docs/TESTING.md), [release/version process](docs/RELEASING.md), [audit](docs/AUDIT.md).

Project source and original icon assets are [MIT licensed](LICENSE): use, modify, redistribute and sell copies while retaining the copyright and license notice. MIT does not grant rights to third-party trademarks, FC2, drivers or Microsoft components. [Dependency and Preview redistribution terms](THIRD_PARTY_NOTICES.md) remain applicable.

Provided **AS IS**, without warranty. To the extent permitted by applicable law, authors are not liable for damage, interrupted recording, data loss or other consequences. Full warranty/liability terms are in LICENSE. Lower monitoring levels before testing gain; this is not hearing protection or a guaranteed microphone mute.

Windows startup is opt-in in Settings and disabled by default. Setup offers to close FocGrain before upgrading. Uninstall removes the current user's settings, logs and startup entry; shared Windows/Focusrite components remain installed.

### Port missing after restart or upgrade

Start FocGrain before FC2, wait for readiness, then select FocGrain again if FC2 shows No device. The device identifier uses the stable ProductInstanceId; FC2 selection may need restoring if a port disappears during recreation. At logon FocGrain waits 15 seconds and retries returned startup errors; a native timeout stops retries. [MIDI recovery and Windows transport issue](docs/MIDI_RECOVERY.en.md). To refresh an older enabled startup entry, open Settings and Save once.

### FC2 must be running, not just its tray notifier

FocGrain now minimizes FC2 when launching it; an already running FC2 window is left as it is. It checks the FC2 process every two seconds. A red exclamation badge means FC2 is absent or MIDI is unavailable; the tooltip and menu identify the problem. Gain commands are blocked while FC2 is absent. Process presence does not prove MIDI reception or hardware gain changes.

The Focusrite Notifier tray utility is separate from Focusrite Control 2. Version 1.0.3 starts the installed FC2 application once after the MIDI input is ready, unless FC2 is already running. This is enabled by default and can be disabled with **Start FC2 after the MIDI port is ready** in Settings. It starts FC2 minimized; no undocumented background switches, gain commands or extra Windows startup entries are used. Quitting FocGrain does not terminate FC2. Windows startup for FocGrain remains a separate opt-in setting.

### MIDI recovery during upgrades

From 1.0.4, use **Repair MIDI without restarting Windows…** in the tray. After explicit confirmation and UAC, a one-shot helper restarts only midisrv and relaunches FocGrain with normal rights. Other MIDI connections are temporarily interrupted; save work first. Setup detects incomplete/legacy shutdown and offers the same repair before upgrading. No shared-service restart occurs silently. [Recovery details](docs/MIDI_RECOVERY.en.md).

### Sleep and hibernation

Suspend invalidates the last-sent display and blocks gain commands. On automatic resume FocGrain waits three seconds and any in-flight operation, then checks its preserved MIDI connection on the existing MTA worker. No gain is restored or replayed. A native timeout keeps the tray available for explicit MIDI repair; it never starts another worker or silently restarts midisrv. FC2 may need its FocGrain input reselected after endpoint recreation.

`application.log` records power transitions, operation type, HRESULT and full managed exception, worker/thread information, shutdown requests and ProcessExit. `active-run.txt` detects a previous tray run without completed shutdown on next launch. Forced termination, native fail-fast and power loss may prevent final logging; the marker indicates an incomplete run, not its cause. Keep `application.log` and `application.log.previous` when reporting a failure.

Manual acceptance still required: hibernate/wake with FC2 open, confirm tray remains responsive, no unsolicited CC, gain state unknown, user-triggered CC succeeds; repeat during an in-flight operation and with MIDI unavailable. On timeout confirm commands stay blocked, repair requires confirmation/UAC, and Quit remains bounded. Check logs after normal Quit and an externally terminated test process. These checks have not been run on hardware for this patch.

In 1.0.6 setup can install an update without launching it if MIDI service repair is declined or fails. The dirty marker continues to block MIDI port opening until explicit repair. Recovery helper details appear in the setup log and error dialog.

In 1.0.7 the elevated explicit-repair helper temporarily enables SeDebugPrivilege to open the verified SYSTEM-owned MidiSrv process, then restores its token. Missing privilege fails closed. A dirty marker now consistently points to Repair MIDI rather than the hidden Retry action.

Application logs are limited to 1 MiB total: `application.log` and one `.previous` file, at most 512 KiB each. Rotation discards the oldest archive. Oversized records carry a truncation notice; older oversized logs are trimmed on the next write.

From 1.0.9 resume preserves the existing native endpoint. It checks session/connection IsOpen and WinMM visibility with bounded retries; it never disconnects/recreates an existing endpoint after sleep. These checks do not prove FC2 delivery or hardware gain. Closed or unresponsive native state still needs explicit service recovery. No CC is sent for validation.
