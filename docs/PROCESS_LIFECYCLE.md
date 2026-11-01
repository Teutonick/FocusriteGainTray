# Process lifecycle

Normal operation uses one FocGrain tray process. Setup can temporarily have an
Inno Setup bootstrap/elevated process pair. Its post-install launch uses `nowait`:
closing the finished wizard must release both setup processes while the tray runs.
Previously Setup waited for the tray to quit and remained visible in Task Manager.

The MIDI worker is a background thread, not another process. Native MIDI calls
already have bounded waits; disposal gets two seconds. Tray shutdown additionally
has a ten-second process watchdog, independent of the UI thread. Its last-resort
exit code is 31 (incomplete cleanup), so upgrade/recovery logic must treat cleanup
as unconfirmed. It never terminates another application's process.

Helper process deadlines cover prerequisite/access checks (20 seconds), installed
app shutdown (30 seconds), explicit service recovery (90 seconds), recovery waiter
(130 seconds), and developer MIDI smoke (60 seconds). A deadline terminates only
the helper with failure code 29. It bypasses exit handlers to avoid another hang.
Normal paths still produce their existing reports and exit codes. A missing report
or nonzero result blocks readiness; timing out is never a successful MIDI cleanup.

Interactive third-party installers, browser/help windows, Focusrite Control 2,
and Windows services have independent lifetimes. FocGrain does not kill them on
quit or impose a destructive timeout while they install drivers. Setup waits for
the user to finish a vendor installer before checking prerequisites again.

Validation: `tests/Test-ProcessLifecycle.ps1` exercises a blocked helper deadline,
graceful and forced shutdown, installation path isolation, and single-instance
restart without opening MIDI. The real setup launch/close path still needs an
interactive Windows installation check; these tests do not verify hardware.

Build toolchain for this change: local .NET SDK 10.0.402, target
`net10.0-windows10.0.26100.0`, Windows SDK package 10.0.26100.57,
MIDI preview package 0.99.92-preview.11. Restore/build succeeded; NuGet
vulnerability metadata retrieval returned NU1900 because nuget.org was unavailable.
