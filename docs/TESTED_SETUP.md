# Verified release setup / Проверенный сетап

## 1.0.0 acceptance · 2026-10-26

The owner confirmed successful operation on their physical setup: clicking the tray icon switches the selected input's hardware gain between the configured MIN and MAX values. The first release meets the application's core goal.

Владелец подтвердил работу на своём устройстве: клик по значку трея переключает аппаратный gain выбранного входа между настроенными MIN и MAX. Основная цель приложения достигнута, первый релиз принят.

| Component / Компонент | Version / Версия |
| --- | --- |
| Audio interface / Звуковая карта | Focusrite Scarlett 4i4 4th Gen |
| Windows | Windows 11 Pro 26H2 x64, **26300.9711** |
| Focusrite Control 2 | **1.1210.0.0** |
| Focusrite Audio Drivers | **4.150.0.432** |
| Windows MIDI Services | In-box `midisrv`, running; `MidiSrv.exe` **10.0.26100.8875** |
| MIDI transport | Windows MIDI Services app-hosted Virtual Device Transport |
| App-local MIDI API | `Windows.Devices.Midi2` **0.99.92-preview.11**; DLL **1.0.16.0** |
| Microsoft Visual C++ x64 runtime | **14.51.36247.00** |
| Bundled .NET / Windows Desktop runtime | **10.0.13**, self-contained |
| Build tools / Инструменты сборки | .NET SDK **10.0.402**, Inno Setup **6.7.3** |

FC2 configuration: **MIDI Control = FocGrain**, **MIDI Channel = All channels or 1**, **Enable pickup = OFF**. Application mapping: MIDI channel 1, CC12 for Analog 1 and CC13 for Analog 2. Gain presets use MIDI values 0–127, not dB; MIN is not mute.

Настройка FC2: **MIDI Control = FocGrain**, **MIDI Channel = All channels или 1**, **Enable pickup выключен**. Приложение использует MIDI-канал 1, CC12 для Analog 1 и CC13 для Analog 2. Значения пресетов 0–127 — MIDI, не дБ; MIN не является mute.

Versions were read from Windows/CIM, installed Focusrite executable and driver metadata, VC runtime registration, the system MIDI service binary and the published application payload. No account names, machine IDs or device serial numbers are included. Acceptance is the owner's physical-device observation; this table identifies that setup rather than extending the result to every Focusrite model.

Версии собраны из Windows/CIM, метаданных FC2 и драйверов, регистрации VC runtime, системного MIDI-сервиса и файлов сборки. Имена пользователей, идентификаторы компьютера и серийные номера не включены. Приёмка основана на проверке владельцем физического устройства; таблица описывает именно этот сетап.
