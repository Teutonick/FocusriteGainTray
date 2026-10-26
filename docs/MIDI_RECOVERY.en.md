# Missing FocGrain port after startup or upgrade

[Русский](MIDI_RECOVERY.md)

The virtual MIDI port exists while FocGrain is running. FC2 may reset its selection to **No device** when the port is recreated. Start FocGrain, wait for **Ready to control gain**, then open FC2 and select **FocGrain**, channel **1 / All channels**, with **Enable pickup OFF**. Reopen FC2 if its device list has not refreshed.

## Native creation/disconnect timeout

If the log stops at `CreateVirtualDevice` or `DisconnectEndpointConnection`, this is a system-port creation/teardown problem rather than FC2 pickup configuration.

1. Save work in MIDI applications; quit FocGrain and FC2.
2. Use **Start → Power → Restart**. Restart Windows, not just FocGrain; shutdown with Fast Startup is a different diagnostic step.
3. Start FocGrain first, wait for readiness, then reopen FC2 and select FocGrain.
4. If it recurs, check **Settings → Windows Update**. Do not randomly uninstall security updates or MIDI devices. Preserve application.log phases and Windows/MIDI versions for support.

Microsoft tracks a [similar virtual transport defect in #1047](https://github.com/microsoft/MIDI/issues/1047). A Microsoft maintainer stated on [21 October 2026 in #819](https://github.com/microsoft/MIDI/issues/819) that its fix was awaiting delivery. Similar symptoms are not proof of an identical root cause on this PC. Its previously collected VirtualMidiTransport DLL version is **1.0.15.0**, also present in that report.

Version 1.0.1 waits 15 seconds at logon/early Windows startup, allows up to 30 seconds to connect, and retries at most three returned failures. It never retries a hung native call, restarts the shared service, starts replacement native workers or sends gain during recovery.

## Device identity and WinMM visibility

Correction in 1.0.2: AssociationId links the virtual device sides; the Windows device identifier is built from ProductInstanceId, which FocGrain already keeps stable (FocusriteGainTray_Controller_01). A changing AssociationId alone does not prove changing device identity. This follows Microsoft MidiVirtualDeviceCreationConfig.cpp and Midi2.VirtualMidiEndpointManager.cpp.

Live inspection on 26 October found the FocGrain APPDEV software device with status OK, but no FocGrain WinMM input, both with normal and elevated permissions. FC2 therefore has no input to select. A Device Manager entry alone is insufficient.

Version 1.0.2 waits for WinMM input visibility before reporting readiness, rechecks an already opened endpoint without recreating it, blocks a second process with machine-wide and legacy-session mutexes, and records PID/session in logs. Read-only enumeration is available with scripts/Inspect-MidiPorts.ps1; it does not open ports or send messages. Devices are not automatically removed.

## Observed recovery

On 26 October, exact APPDEV removal succeeded but did not restore the port alone. After a separately authorized midisrv restart, one FocGrain 1.0.2 process created the device and independent WinMM enumeration showed exactly one FocGrain input. A duplicate launch exited without a second owner. No gain commands or FC2/driver changes were made. This is a concrete recovery result, not an automatic service-restart feature or a permanent transport fix.
