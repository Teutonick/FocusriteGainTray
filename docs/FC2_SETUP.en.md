# Focusrite Control 2 setup

[Русский](FC2_SETUP.md)

At the end of installation, **Start FocGrain and open the FC2 setup wizard** is selected by default. The wizard is also available in the tray menu, Settings and its own Start menu shortcut. It does not automate FC2 or send gain commands.

1. Start FocGrain, then Focusrite Control 2. Open **… → Preferences → Application** in FC2.
2. Select **MIDI Control → FocGrain**, **MIDI Channel → All channels** (or **1**).
3. **Uncheck Enable pickup**, as shown. Leave it off for immediate MIN ↔ MAX toggling.

![Correct FC2 settings: FocGrain, All channels, Enable pickup unchecked](../assets/setup/fc2-preferences.png)

Pickup ignores MIDI until the incoming controller value matches the current FC2 value. This prevents jumps with a physical control but can block a two-preset toggle. The user confirmed that gain switching worked after turning pickup off.

**Focusrite USB MIDI** is the interface hardware MIDI port, not FocGrain; do not remove it. Select exactly **FocGrain**, not QA/test ports. FC2 may cache old entries after diagnostics: if needed, reopen FC2 with FocGrain running instead of deleting Windows devices indiscriminately.

4. In FocGrain Settings, choose **Analog 1 / Analog 2** and your minimum/maximum values. Left-click its tray icon to toggle. Watch the **round gain knob for Analogue 1/2**, not the vertical signal meter.

![FC2 inputs: round gain controls for Analogue 1 and 2](../assets/setup/fc2-inputs.png)

Numbers above the vertical meters show input signal level, not the gain setting. **MIDI values 0–127 are not dB. Minimum gain is not mute.** Use deliberately safe monitoring levels for the manual check.

The tray status **FocGrain port open** confirms only the application's own virtual port, not FC2 receipt or hardware gain. Retry opening the port is offered only when it is not open; no connection button is needed during normal operation.

The screenshots were supplied by the user for this guide. The depicted interface and Focusrite marks remain the property of their respective owners. FocGrain is an independent project, not a Focusrite product.

[Official Focusrite MIDI documentation](https://support.focusrite.com/hc/en-gb/articles/38789425032594-Controlling-Focusrite-Control-2-via-MIDI).
