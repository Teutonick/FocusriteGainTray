# FocGrain setup

## User flow

Run `artifacts/setup/FocGrain-0.4.0-Setup-x64.exe`. The normal setup requests administrator rights, shows MIT and an independent-project/preview notice, then a prerequisite page **before any application files are installed**.

| Component | Detection / action |
| --- | --- |
| Windows 11 25H2 x64, build 26200+ | Inno OS/architecture gate; older Windows and ARM64 are rejected by this x64 release |
| .NET | Published self-contained with FocGrain; no SDK or separate runtime installation on the user's machine |
| Visual C++ x64 | Installed runtime registry version plus required system DLL presence; reviewed baseline 14.51.36247; offer official Microsoft download and interactive installer |
| MIDI API / service / Virtual Device App Transport | Temporary FocGrain CLI probe calls official `MidiApi.EnsureServiceAvailable()` and `MidiVirtualDeviceManager.IsTransportAvailable`; no MIDI session, endpoint or gain message |
| Focusrite Control 2 | 32/64-bit HKLM uninstall registration plus actual executable version; reviewed baseline 1.1210.0.0; offer official vendor download if absent/older |
| Focusrite driver package | Existing Focusrite Audio Drivers registration and installation folder; offer bundled FC2 installer if absent |

The runtime and FC2 baselines are reviewed release baselines, not claims that every older version is incompatible. A registered driver package is not evidence that a physical interface is connected or working. After setup, the user must select FocGrain/channel 1 in FC2 MIDI Control and verify input compatibility.

System MIDI Services and the Virtual Device App Transport are Windows components. The separate Microsoft Preview Transports installer contains other transports and is not the remedy for a missing APP transport. Setup offers Windows Update and official MIDI documentation when these checks fail; it does not attempt to replace the service, change Legacy API Mode, alter Developer Mode or install a third-party virtual MIDI driver. Update/restart Windows, then rerun setup. API activation failure on an otherwise supported machine requires diagnosis; setup fails closed instead of pretending a tools installer will fix it.

Downloads occur only after clicking the component button and confirming. The downloaded file is not embedded in our setup. HTTPS and a pinned SHA-256 are verified before executing it. The reviewed binaries had valid Authenticode signatures: Microsoft Corporation / FOCUSRITE AUDIO ENGINEERING LIMITED. Sources, versions and hashes live in `installer/dependencies.json`. The vendor UI remains interactive for license/driver/restart decisions. Cancellation, failure, hash mismatch and network failure return to prerequisite preparation; they do not install FocGrain.

Vendor exit codes 3010/1641 or newly appearing Windows reboot markers block application installation until the user restarts Windows and reruns setup. Setup does not force a reboot. Silent mode performs the same final gate and never silently downloads/install missing prerequisites. Normal mode rechecks immediately before file installation.

Application goes to Program Files/FocGrain, with Start menu shortcuts and an Installed Apps uninstaller. Startup is opt-in at the finish page, not automatic. Autostart is not configured. The previous Local/FocusriteGainTray mutex and `%LOCALAPPDATA%/FocusriteGainTray` preset path are preserved for compatibility. The UI and virtual endpoint are named FocGrain; existing FC2 MIDI selections may need to be updated once.

Uninstall removes the application and its app-local runtime/API; it leaves presets, FC2, shared drivers and Visual C++ alone. Re-run a newer setup with the same AppId to update the app. The installer explicitly removes the old app-local MIDI DLL/PRI before installing its payload, allowing a later in-box-API version to stop shipping them. Users must obtain new releases manually; there is no background updater.

## Build

.NET 10 SDK, PowerShell 7 and Inno Setup 6.7.3 are needed on the developer machine. No compiler is downloaded automatically by the build script.

```powershell
./scripts/Prepare-MidiSdk.ps1
./scripts/Build-Setup.ps1 -ISCC 'C:/path/to/Inno Setup 6/ISCC.exe'
```

If present, `.work/sdk/dotnet.exe` and `.work/inno/ISCC.exe` are used by default. Output: self-contained `artifacts/publish`, setup and SHA-256 sidecar in `artifacts/setup`. Generated defines, official downloaded prerequisites, SDK and build tools stay in ignored `.work`; no vendor installer is checked in or bundled. Publish cleans only its fixed artifacts/publish folder to avoid stale runtime/preview files.

The package source map pins the preview SDK to the local feed; it is obtained from the official Microsoft release, not nuget.org. Included third-party notices come from the exact restored runtime and CsWinRT packages and Inno compiler license. Review the compiler's current commercial-use terms separately if distributing a paid/commercial product; the MIT license of FocGrain does not license the compiler.

When updating dependencies, download the official file separately, verify Authenticode publisher, inspect its file version, compute SHA-256 and review vendor compatibility/license changes. Only then edit dependencies.json. Both current download URLs use a vendor `latest` alias: if the upstream file changes, the old setup will deliberately refuse the new hash. Install the current official prerequisite manually and use “Проверить снова”, or release a refreshed setup. Never remove hash verification to work around an update.

## Validation and limits

- Model tests cover all prerequisite policy exit codes and unknown failure handling.
- Publish/build and installer compilation are automated; an unsigned setup is expected until a signing certificate/workflow is configured.
- `FocGrain.exe --check-prerequisites <absolute-report-path>` writes UTF-16 INI and returns the same code. This mode initializes no tray/hotkeys, writes no presets and creates no endpoint. It may ask the Windows service to become available.
- An internal `PreviewOnly` compiler define disables all dependency installer execution and all app file installation, and uses lowest privilege solely for UI QA. That binary is generated under .work, not delivered as setup.
- Fresh-machine dependency installation, network failure/cancel/hash-change, required reboot, actual upgrade/uninstall should be exercised on a disposable Windows 25H2 VM or target test system. A successful probe on an already prepared PC is not that regression.

## Upstream

- [MIDI requirements](https://microsoft.github.io/MIDI/kb/minimum-requirements/)
- [APP transport](https://microsoft.github.io/MIDI/kb/virtual-device-app/)
- [Preview 11 and developer go-live agreement](https://github.com/microsoft/MIDI/releases/tag/inbox-preview-11)
- [FC2 / bundled driver instructions](https://support.focusrite.com/hc/en-gb/articles/36903735706258-Upgrading-from-Focusrite-Control-to-Focusrite-Control-2)
- [Official supported-interface downloads](https://downloads.focusrite.com/focusrite/scarlett-4th-gen/scarlett-4i4-4th-gen)
- [Inno Setup](https://jrsoftware.org/)

## 0.3.0 appearance, locale and version

Original FocGrain artwork is embedded in the EXE and six tray states, settings/update windows, setup/uninstaller metadata, wizard small/large images and Start menu shortcuts. Multi-resolution ICO files cover 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels. Source artwork and deterministic Windows generation script are in assets/ and scripts/Generate-Icons.ps1. No Focusrite artwork is used.

Setup lists English first, detects the Windows UI language and uses Russian only for Russian; unsupported languages use English. It does not persist a previous setup language. Native Inno strings, custom preparation/download messages and notices are bilingual. The setup passes its selected language into the temporary CLI probe. The guide shortcut selects README.md/README.ru.md. The tray application uses the same ru/en fallback rule through CurrentUICulture, not number/date-format locale.

The prerequisite memo scrolls instead of cutting off long diagnostics. Directory.Build.props supplies the default build version, UI/EXE metadata and setup name. Check for updates is an explicit application action against GitHub Releases; setup does not auto-publish a release or silently fetch a newer FocGrain version. See RELEASING.md.

## 0.4.0 post-install FC2 wizard

The finish action now defaults to launching `FocGrain.exe --setup`, as explicitly requested for post-install guidance. The user can uncheck it; silent setup skips it. Normal Start menu launch still uses no arguments. A separate localized wizard shortcut uses --setup; the tray and Settings offer the same guide. When FocGrain is already running, --setup posts a fixed registered Windows message to its hidden request window, then exits without constructing another tray/MIDI worker. This change adds no gain commands or remembered wizard/hardware state.

The four-page guide loads the two original user-supplied screenshots from embedded resources and works offline. It explains Application settings, FocGrain/channel selection, mandatory Enable pickup OFF, and a manual gain-knob check. It never changes FC2 settings or sends gain/scan/ramp commands. Image enlargement and resizable/scrollable content are provided. Offline Markdown versions and screenshots are installed in docs/ and assets/setup/. See FC2_SETUP.md and its English version.

The disabled connected-state Connect MIDI action was removed. The header now says the FocGrain port is open without asserting FC2 receipt. Retry appears only if the port is not open and no native timeout has locked the worker. Presets are labeled MIDI values, not misleading CC numbers.

## Версия 0.5.0: запуск и удаление

В настройках появился пункт «Запускать при входе в Windows». По умолчанию он выключен; включение добавляет одну запись FocGrain в HKCU\Software\Microsoft\Windows\CurrentVersion\Run. Снятие галочки удаляет её. Запуск не отправляет команды gain.

При обновлении setup предлагает закрыть экземпляры из выбранной папки установки. Сначала отправляется запрос обычного выхода; через 5 секунд старый или зависший процесс завершается принудительно. Другие папки установки, Focusrite Control 2 и MIDI Services не затрагиваются. При удалении используется тот же механизм, удаляются файлы установки, ярлыки, запись автозапуска, настройки и логи текущего пользователя в %LOCALAPPDATA%\FocusriteGainTray. Настройки других учётных записей не удаляются. Общие компоненты Focusrite, Visual C++ и Windows остаются установленными.

Выход скрывает и освобождает значок трея, освобождает горячие клавиши и окна, даёт MIDI SDK две секунды на закрытие. Зависший native-вызов выполняется на фоновом потоке и не должен удерживать процесс. Виртуальные порты обслуживаются Windows MIDI Services; установщик не перезапускает общую MIDI-службу. История диагностики native cleanup сохранена в AUDIT.md. Лог ограничен текущим файлом около 1 МБ и одной предыдущей копией.

## 1.0.4 MIDI recovery gate

Shutdown helper code 31 means incomplete/legacy cleanup or an existing recovery marker. Interactive upgrade offers an explicitly confirmed restart of midisrv before writing files/launching. Declining blocks upgrade; silent upgrade never restarts the shared service and refuses this case. Uninstall may proceed without recovery if declined. The temporary probe runs --repair-midi-service 0 only after approval; recovery owns the same global MIDI lock, verifies the exact dedicated system service process and never changes drivers/FC2/gain.
