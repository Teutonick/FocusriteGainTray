# Changelog

## 1.0.8 · 2026-10-28

- Fix recovery across different UAC accounts: the original user clears its own dirty marker after verified helper success. An elevated machine receipt supersedes older per-user markers after installer recovery, while newer or malformed dirty markers remain blocked.
- Confirmed from both account logs that 1.0.7 service restarts succeeded but the original user marker survived; previous access-only validation did not cover this gate.


## 1.0.7 · 2026-10-28

- Explicit elevated MIDI recovery temporarily enables SeDebugPrivilege for access to the verified dedicated SYSTEM service process, restores token state, and names failing native calls. Retain image, sole-service ownership, PID and Stop Pending checks.
- Fix the click/hotkey hint when a dirty MIDI marker hides the retry action. Add a read-only recovery-access diagnostic that never stops or starts the service.


## 1.0.6 · 2026-10-28

- Permit an explicit upgrade without launch when MIDI repair fails or is declined; retain the dirty marker and block port opening until explicit repair. Record helper error details in the setup log and error dialog. No automatic shared-service reset.


## 1.0.5 · 2026-10-28

- Handle Windows suspend/automatic resume on the UI thread, invalidate old MIDI completions and rebuild the connection after wake without replaying gain. Keep native timeout quarantine and explicit service-repair consent.
- Log fatal managed exceptions, late MIDI failures, cleanup errors, session shutdown messages, process exit and incomplete previous tray runs; serialize log rotation/writes.
- Validation: .NET SDK 10.0.402 restore/build, Windows SDK 10.0.26100.57, MIDI SDK 0.99.92-preview.11; 31 model/worker scenarios. NuGet vulnerability metadata unavailable (NU1900). Actual hibernation and hardware recovery remain unverified.


## 1.0.0 · 2026-10-26

- First accepted stable release: the owner confirms hardware gain MIN/MAX switching by tray click on Scarlett 4i4 4th Gen.
- Record exact Windows, FC2, Focusrite driver, MIDI and runtime versions in both READMEs and TESTED_SETUP.md.
- Replace the provisional port/FC2 status with Ready to control gain in RU/EN; retain actual MIDI error handling and send-only state semantics.


## 0.4.0 · 2026-10-25

- Add a separate four-step RU/EN FC2 setup wizard using the user's two real screenshots, with enlarge view and explicit pickup-off instructions.
- Open the wizard from setup's default-selected finish action, tray, Settings or Start menu. Repeated --setup requests are routed to the existing tray process instead of creating a second MIDI host.
- Clarify port-open status versus FC2 receipt, hide the redundant connected-state retry action and label presets as MIDI values rather than CC numbers.
- Add offline screenshot guides and image attribution; retain original screenshots without alterations.
- Record user confirmation that gain toggling works after disabling Enable pickup. Native cleanup failure remains a separate unresolved diagnostic result.

## 0.3.0 · 2026-10-25

- Add original multi-resolution gain-dial artwork for application, tray states, windows, setup, wizard and shortcuts.
- Select Russian/English from the Windows UI language; unsupported languages fall back to English. Localize setup and prerequisite reports too.
- Move native MIDI lifetime and operations to one background MTA worker, reject concurrent/delayed commands, bound operation/shutdown waits and block retries after a timeout.
- Add connect-phase diagnostics and an explicit no-gain WinMM smoke mode. The current PC reproduced a system-call timeout; physical FC2 recovery remains unverified.
- Centralize version 0.3.0 in Directory.Build.props and add an on-demand GitHub Releases checker to tray/settings. No automatic downloads, installation or release publication.
- Expand regression tests to 13 scenarios, including blocked native calls, locale fallback and mocked GitHub responses.

## 0.2.0 · 2026-10-25

- Name application/executable and virtual endpoint FocGrain; preserve existing settings and single-instance identity.
- Add a Windows x64 setup with MIT/preview notice, prerequisite page, official downloads with pinned SHA-256, interactive vendor installers, reboot/cancellation handling and a final dependency gate.
- Publish self-contained .NET; check Visual C++, MIDI API/service/APP transport, FC2 executable version and driver package registration before installation.
- Offer Windows Update for system MIDI components; do not install unrelated Preview Transports.
- Add side-effect-limited prerequisite CLI, policy regression cases and repeatable setup build.

## Initial review · 2026-10-25

- Clarify independent-project identity, MIT permissions, trademark boundaries and third-party distribution terms.
- Replace the handoff README with Russian user setup, tray behavior, limitations and troubleshooting.
- Add project contract, contributor guidance, acceptance checklist and audit record.
- Add selected-input settings and ordered min/max validation.
- Show MIN/MAX/MID/unknown/error tray modes; clear stale state on reconnect or send failure.
- Add dependency-free model regression scenarios and Windows build CI.

Application compilation and physical-interface acceptance status are recorded in docs/AUDIT.md and docs/TESTING.md.

## 0.5.0

- Исправлены размеры и выравнивание кнопок настроек; добавлен автозапуск Windows, выключенный по умолчанию.
- Setup/удаление закрывают процессы только из выбранной папки: обычный выход и ограниченный принудительный fallback.
- Удаление очищает автозапуск и данные текущего пользователя; размер лога ограничен.

## 0.5.1

- MIN: серая точка и приглушённые акценты. MAX: более яркий оранжевый цвет.
- Основной README на английском, русский доступен по ссылке; ссылки установщика обновлены.

## 1.0.1 · 2026-10-26

- Add cancellable logon delay, bounded startup retries for returned failures and a 30-second connect deadline. Never retry a still-running native call or send startup gain.
- Log generated association/device identity; document the read-only Preview 11 association and FC2 reselection.
- Replace restart-app-only timeout advice with Windows MIDI recovery guidance and the upstream transport defect.
- Add four meaningful startup regression scenarios (18 total).

## 1.0.2 · 2026-10-26

- Enforce one machine-wide MIDI owner before starting tray or MIDI smoke mode; retain the legacy session mutex to block older versions. Duplicate launch activates the existing instance.
- Confirm WinMM input visibility before showing Ready; recheck an already opened endpoint without recreation.
- Add PID/session logging and a read-only WinMM inventory script.
- Correct the earlier association-id explanation: ProductInstanceId identifies the Windows device.

## 1.0.3 · 2026-10-26

- Start the installed Focusrite Control 2 once after MIDI readiness, unless already running. Enabled by default with a separate opt-out checkbox.
- Distinguish Focusrite Notifier from the actual MIDI-receiving FC2 application; keep Windows startup opt-in and send no startup gain.
- Add four FC2-startup/settings scenarios (25 total) and extend the RU/EN settings layout.

## 1.0.4 · 2026-10-26

- Add explicit UAC MIDI-service recovery without reboot; close the old owner, verify and restart only midisrv, then relaunch with normal rights and no gain sends.
- Record incomplete shutdown (exit 31 + marker). Setup gates dirty/legacy upgrades with a separate service-reset consent; silent installs never reset shared MIDI.
- Verify Stop Pending, own-process service type, exact System32/MidiSrv.exe image and exclusive service ownership before forced fallback.
- Add four recovery/upgrade policy scenarios (29 total), dirty-exit process testing and strict recovery CLI validation.
