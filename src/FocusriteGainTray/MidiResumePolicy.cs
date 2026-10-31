namespace FocusriteGainTray;

public static class MidiResumePolicy
{
    // Never tear down an existing native object after sleep: cleanup itself can deadlock.
    public static void Run(bool hasNativeState, bool sessionOpen, bool connectionOpen,
        Action checkVisibility, Action initialConnect)
    {
        if (!hasNativeState) { initialConnect(); return; }
        if (!sessionOpen || !connectionOpen)
            throw new IOException(Ui.T("После сна MIDI-соединение закрыто. Используйте «Восстановить MIDI без перезагрузки»; старый порт автоматически не пересоздаётся.",
                "After sleep the MIDI connection is closed. Use Repair MIDI without restarting Windows; the old port is not automatically recreated."));
        checkVisibility();
    }
}
