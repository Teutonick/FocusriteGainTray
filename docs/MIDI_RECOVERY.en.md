# Repair MIDI without restarting Windows

[Русский](MIDI_RECOVERY.md)

## FocGrain 1.0.4+

Save work in MIDI applications, then choose **Repair MIDI without restarting Windows…** in the tray. Confirm the shared MIDI disconnection warning and UAC. Cancelling UAC changes no service.

The old FocGrain process exits first. A one-shot elevated helper acquires exclusive MIDI ownership and restarts only **midisrv**. If normal stop stalls, it verifies Stop Pending, the exact **System32\MidiSrv.exe** image and exclusive service ownership before terminating that process. Shared, changed or unverified processes are refused.

A normal, unelevated FocGrain process waits for successful recovery, then creates the port and checks WinMM. No gain is sent. Reopen FC2's MIDI list and select FocGrain if needed, channel 1 / All channels, Enable pickup OFF.

The helper installs no permanent service and does not terminate FC2, Focusrite drivers or unrelated services. It accepts no arbitrary service name, kill PID or executable path. Recovery is explicit, never an automatic startup action.

## Upgrade and uninstall

Version 1.0.4 returns **31** and records a current-user recovery marker after incomplete MIDI shutdown. The marker clears on successful recovery or a healthy new MIDI input. Older versions are treated conservatively because they did not report native cleanup status.

Interactive setup asks to recover the MIDI service before continuing an upgrade after dirty/legacy shutdown. Declining stops the upgrade before replacing files/launching the new app. Silent upgrade refuses this case and never resets a shared service. Uninstall can proceed without recovery if the user declines; the shared service remains untouched.

This is a controlled workaround for a Windows MIDI service hang, not a patch to Windows itself. Review application.log on refusal or recovery failure. A full Windows restart remains a fallback rather than the primary instruction.

## Evidence

On 26 October, an OK FocGrain APPDEV node had no matching WinMM input. Exact software-node removal alone did not help. An authorized midisrv restart restored exactly one FocGrain input; see [audit](AUDIT.md) and [tested setup](TESTED_SETUP.md). ProductInstanceId identifies the Windows device; changing AssociationId alone does not establish changing device identity.

Microsoft tracks a similar transport issue in [#1047](https://github.com/microsoft/MIDI/issues/1047) and [#819](https://github.com/microsoft/MIDI/issues/819). FocGrain does not replace Windows components or uninstall security updates.

The compiled 1.0.4 helper was approved and exercised on this PC on 26 October: legacy shutdown returned 31, recovery returned 0, service Running and marker cleared. A normal process then created exactly one FocGrain WinMM input. No Windows reboot or gain commands were used.
