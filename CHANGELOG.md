# Changelog

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
