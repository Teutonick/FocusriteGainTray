using FocusriteGainTray;
using System.Text.Json;

int passed = 0;
void Check(string name, Action test) { test(); Console.WriteLine("PASS " + name); passed++; }
void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
void Reject(Action action) { try { action(); } catch (ArgumentOutOfRangeException) { return; } throw new Exception("Expected rejection"); }
Check("startup unknown; first click selects configured maximum", () => {
    var state = new GainState(); var c = new ChannelSettings { A = 7, B = 31 };
    Equal<int?>(null, state.LastSent(1)); Equal("?", state.Mode(1,c)); Equal(31, state.Next(1,c));
});
Check("toggle and independent inputs", () => {
    var state = new GainState(); var c = new ChannelSettings { A = 7, B = 31 };
    state.Record(1,31); Equal("MAX",state.Mode(1,c)); Equal(7,state.Next(1,c));
    Equal<int?>(null,state.LastSent(2)); state.Record(1,7); Equal("MIN",state.Mode(1,c)); Equal(31,state.Next(1,c));
    state.Record(1,20); Equal("MID",state.Mode(1,c)); Equal(31,state.Next(1,c));
    state.Reset(); Equal<int?>(null,state.LastSent(1));
});
Check("MIDI mapping and boundaries", () => {
    Equal((byte)12,GainState.Controller(1)); Equal((byte)13,GainState.Controller(2));
    var state = new GainState(); state.Record(1,0); state.Record(2,127);
    foreach (int input in new[] {-1,0,3,127}) { Reject(() => GainState.Controller(input)); Reject(() => state.Record(input,0)); }
    Reject(() => state.Record(1,-1)); Reject(() => state.Record(1,128));
});
Check("settings normalize corrupt ranges and reversed presets", () => {
    var settings = new AppSettings { SelectedInput = 99, Step = -3, Input1 = new() { A = 200, B = -1 }, Input2 = null! };
    settings.Validate(); Equal(2,settings.SelectedInput); Equal(1,settings.Step);
    Equal(0,settings.Input1.A); Equal(127,settings.Input1.B); Equal(40,settings.Input2.B);
});
Check("user settings JSON round trip", () => {
    var settings = new AppSettings { SelectedInput = 2, Input2 = new() { A = 9, B = 27 } };
    var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
    restored.Validate(); Equal(2,restored.SelectedInput); Equal(9,restored.Channel(2).A); Equal(27,restored.Channel(2).B);
});
Check("equal presets have stable display", () => {
    var state = new GainState(); var c = new ChannelSettings { A = 5, B = 5 };
    state.Record(1,5); Equal("MIN",state.Mode(1,c)); Equal(5,state.Next(1,c));
});
Check("all MIDI values preserve sent values and toggle policy", () => {
    var state = new GainState(); var c = new ChannelSettings { A = 3, B = 52 };
    for (int input = 1; input <= 2; input++)
        for (int value = 0; value <= 127; value++) {
            state.Record(input, value); Equal<int?>(value, state.LastSent(input));
            Equal(value == c.B ? c.A : c.B, state.Next(input,c));
        }
    state.Reset(); Equal<int?>(null,state.LastSent(1)); Equal<int?>(null,state.LastSent(2));
});
Console.WriteLine($"{passed} scenarios passed");
