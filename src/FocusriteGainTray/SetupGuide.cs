namespace FocusriteGainTray;

public sealed record SetupGuideStep(string Id, string Title, string Instructions, string Image, string Caption);

// Instructions only: no MIDI, FC2 automation, gain presets or persisted verification state.
public static class SetupGuide
{
    public static IReadOnlyList<SetupGuideStep> Steps() => new[]
    {
        new SetupGuideStep("application", Ui.T("Откройте настройки FC2", "Open FC2 settings"),
            Ui.T("Запустите FocGrain, затем Focusrite Control 2.\r\n\r\nВ главном окне FC2 нажмите «…» → Preferences → Application.\r\n\r\nСправа показано нужное окно. Названия элементов FC2 могут оставаться английскими.\r\n\r\nМастер только показывает инструкцию: он не меняет настройки FC2 и не отправляет MIDI.",
                 "Start FocGrain, then Focusrite Control 2.\r\n\r\nIn FC2, open … → Preferences → Application.\r\n\r\nThe screenshot shows the correct page. FC2 labels may remain in English.\r\n\r\nThis wizard only provides instructions. It does not change FC2 settings or send MIDI."),
            "fc2-preferences.png", "Preferences → Application"),
        new SetupGuideStep("device", Ui.T("Выберите порт FocGrain", "Select the FocGrain port"),
            Ui.T("MIDI Control → FocGrain\r\nMIDI Channel → All channels или 1\r\n\r\nFocusrite USB MIDI — аппаратные MIDI-разъёмы интерфейса. Это другой порт, не FocGrain; удалять его не нужно.\r\n\r\nЕсли FocGrain отсутствует в списке, проверьте состояние его порта в трее. При необходимости откройте FC2 заново после запуска FocGrain.",
                 "MIDI Control → FocGrain\r\nMIDI Channel → All channels or 1\r\n\r\nFocusrite USB MIDI belongs to the interface hardware MIDI connectors. It is a different port; do not remove it.\r\n\r\nIf FocGrain is missing, check its port status in the tray. If necessary, reopen FC2 after starting FocGrain."),
            "fc2-preferences.png", "MIDI Control: FocGrain · MIDI Channel: All channels / 1"),
        new SetupGuideStep("pickup", Ui.T("Обязательно снимите Enable pickup", "Turn OFF Enable pickup"),
            Ui.T("Снимите галочку Enable pickup, как на снимке.\r\n\r\nPickup игнорирует MIDI, пока значение контроллера не совпадёт с текущим значением в FC2. Поэтому переключение двух пресетов может не работать при включённом pickup.\r\n\r\nДля мгновенного MIN ↔ MAX оставьте Enable pickup выключенным. Не нужно подбирать промежуточные значения или менять MIDI-команды.",
                 "Uncheck Enable pickup, as shown.\r\n\r\nPickup ignores MIDI until the controller value matches the current FC2 value. It can prevent a two-preset toggle from taking effect.\r\n\r\nFor immediate MIN ↔ MAX switching, leave Enable pickup off. You do not need to sweep values or change MIDI commands."),
            "fc2-preferences.png", Ui.T("Enable pickup: галочка снята", "Enable pickup: unchecked")),
        new SetupGuideStep("verify", Ui.T("Проверьте нужный аудиовход", "Check the selected audio input"),
            Ui.T("В настройках FocGrain выберите Analog 1 или Analog 2 и задайте свои минимальное и максимальное значения.\r\n\r\nНачните с выбранных вами безопасных уровней. Переключите пресет левой кнопкой по значку трея и наблюдайте за КРУГЛОЙ РУЧКОЙ gain соответствующего Analogue 1/2 в FC2.\r\n\r\nЧисла над вертикальными метрами показывают уровень сигнала, а не установленный gain.\r\n\r\nMIDI 0–127 — не дБ. Минимальный gain не является mute. Значок трея отражает последнюю отправленную команду, не обратное чтение устройства.",
                 "In FocGrain Settings, select Analog 1 or Analog 2 and choose your minimum and maximum values.\r\n\r\nUse deliberately safe levels. Left-click the tray icon to toggle, and watch the ROUND GAIN KNOB for the matching Analogue 1/2 input in FC2.\r\n\r\nNumbers above the vertical meters show signal level, not the gain setting.\r\n\r\nMIDI 0–127 is not dB. Minimum gain is not mute. The tray shows the last command sent, not hardware readback."),
            "fc2-inputs.png", Ui.T("Проверяйте ручку gain Analogue 1/2, а не индикатор сигнала", "Watch the Analogue 1/2 gain knob, not the signal meter"))
    };
}
