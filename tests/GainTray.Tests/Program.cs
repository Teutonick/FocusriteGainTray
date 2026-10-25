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
Check("installer gates every missing prerequisite", () => {
    var current = PrerequisitePolicy.FocusriteBaseline;
    Equal(0,PrerequisitePolicy.Evaluate(true,0,current,true));
    Equal(20,PrerequisitePolicy.Evaluate(false,0,current,true));
    foreach (int code in new[] {21,22,23}) Equal(code,PrerequisitePolicy.Evaluate(true,code,current,true));
    Equal(24,PrerequisitePolicy.Evaluate(true,0,null,true));
    Equal(25,PrerequisitePolicy.Evaluate(true,0,new Version(1,0,0,0),true));
    Equal(26,PrerequisitePolicy.Evaluate(true,0,current,false));
    Equal(29,PrerequisitePolicy.Evaluate(true,999,current,true));
    Equal(0,PrerequisitePolicy.Evaluate(true,0,new Version(2,0,0,0),true));
});
Check("system UI language: Russian, English and unsupported fallback", () => {
    var before = System.Globalization.CultureInfo.CurrentUICulture.Name;
    try {
        Equal("ru",Ui.LanguageFor("ru-RU")); Equal("ru",Ui.LanguageFor("RU"));
        foreach (var locale in new[] {"en-US","de-DE","uk-UA","ja-JP",""}) Equal("en",Ui.LanguageFor(locale));
        Ui.Initialize("ru-RU"); Equal("ru",Ui.T("ru","en"));
        Ui.Initialize("ja-JP"); Equal("en",Ui.T("ru","en"));
    } finally { Ui.Initialize(before); }
});
Check("release parser rejects drafts, prereleases and invalid tags", () => {
    var release = ReleaseParser.Parse("{\"tag_name\":\"v1.2.3\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://evil.invalid\"}");
    Equal(new Version(1,2,3),release.Version); Equal("github.com",release.Page.Host);
    Equal("/Teutonick/FocusriteGainTray/releases/tag/v1.2.3",release.Page.AbsolutePath);
    foreach (var tag in new[] {"v1.2.3-beta","v01.2.3","1.2","../../other","v2147483649.0.0"}) {
        try { ReleaseParser.Parse(JsonSerializer.Serialize(new {tag_name=tag,draft=false,prerelease=false})); throw new Exception("Invalid tag accepted"); }
        catch (FormatException) { }
    }
    foreach (var json in new[] {"{\"tag_name\":\"v1.2.3\",\"draft\":true,\"prerelease\":false}","{\"tag_name\":\"v1.2.3\",\"draft\":false,\"prerelease\":true}"}) {
        try { ReleaseParser.Parse(json); throw new Exception("Unpublished release accepted"); } catch (FormatException) { }
    }
});
Check("GitHub update HTTP results and version comparisons", () => {
    UpdateResult Query(System.Net.HttpStatusCode status, string version, Version current) {
        using var http = new HttpClient(new FakeHttp(status,JsonSerializer.Serialize(new {tag_name=version,draft=false,prerelease=false})));
        return new UpdateClient(http).CheckAsync(current,CancellationToken.None).GetAwaiter().GetResult();
    }
    Equal(UpdateStatus.Available,Query(System.Net.HttpStatusCode.OK,"v0.3.1",new Version(0,3,0,0)).Status);
    Equal(UpdateStatus.Current,Query(System.Net.HttpStatusCode.OK,"v0.3.0",new Version(0,3,0,0)).Status);
    Equal(UpdateStatus.Current,Query(System.Net.HttpStatusCode.OK,"v0.2.0",new Version(0,3,0)).Status);
    Equal(UpdateStatus.NoRelease,Query(System.Net.HttpStatusCode.NotFound,"",new Version(0,3,0)).Status);
    Equal(UpdateStatus.RateLimited,Query(System.Net.HttpStatusCode.Forbidden,"",new Version(0,3,0)).Status);
    Equal(UpdateStatus.RateLimited,Query(System.Net.HttpStatusCode.TooManyRequests,"",new Version(0,3,0)).Status);
    try { Query(System.Net.HttpStatusCode.InternalServerError,"",new Version(0,3,0)); throw new Exception("HTTP failure hidden"); } catch (HttpRequestException) { }
});
Check("MIDI lifetime and calls stay on one non-UI MTA thread", () => {
    int owner = Environment.CurrentManagedThreadId; var backend = new FakeMidi();
    var worker = new BoundedWorker<FakeMidi>(() => { backend.Created = Environment.CurrentManagedThreadId; return backend; });
    worker.ExecuteAsync(b => { b.Calls.Add(Environment.CurrentManagedThreadId); Equal(ApartmentState.MTA,Thread.CurrentThread.GetApartmentState()); },TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    worker.ExecuteAsync(b => b.Calls.Add(Environment.CurrentManagedThreadId),TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    worker.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    if (backend.Created==owner) throw new Exception("Native object created on UI/caller thread");
    Equal(backend.Created,backend.Calls[0]); Equal(backend.Created,backend.Calls[1]); Equal(backend.Created,backend.Disposed);
});
Check("a blocked native MIDI call cannot queue changes or block shutdown", () => {
    using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
    var worker = new BoundedWorker<FakeMidi>(() => new());
    try {
        var pending = worker.ExecuteAsync(_ => { entered.Set(); release.Wait(); },TimeSpan.FromMilliseconds(200));
        if (!entered.Wait(TimeSpan.FromSeconds(2))) throw new Exception("Worker did not enter operation");
        try { worker.ExecuteAsync(_ => throw new Exception("Queued command ran"),TimeSpan.FromSeconds(1)).GetAwaiter().GetResult(); throw new Exception("Concurrent request accepted"); } catch (InvalidOperationException) { }
        try { pending.GetAwaiter().GetResult(); throw new Exception("No timeout"); } catch (TimeoutException) { }
        Equal(true,worker.Faulted);
        try { worker.ExecuteAsync(_ => throw new Exception("Replay ran"),TimeSpan.FromSeconds(1)).GetAwaiter().GetResult(); throw new Exception("Retry accepted after timeout"); } catch (InvalidOperationException) { }
        var time=System.Diagnostics.Stopwatch.StartNew();
        worker.StopAsync(TimeSpan.FromMilliseconds(50)).GetAwaiter().GetResult();
        if (time.Elapsed > TimeSpan.FromSeconds(1)) throw new Exception("Shutdown was blocked by native call");
    } finally { release.Set(); worker.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); }
});
Check("FC2 wizard includes mandatory pickup guidance in both languages", () => {
    string before = System.Globalization.CultureInfo.CurrentUICulture.Name;
    try {
        foreach (var language in new[] {"ru","en"}) {
            Ui.Initialize(language); var steps = SetupGuide.Steps();
            Equal("application,device,pickup,verify",string.Join(',',steps.Select(s=>s.Id)));
            Equal("fc2-preferences.png",steps[2].Image); Equal("fc2-inputs.png",steps[3].Image);
            if (!steps[2].Instructions.Contains(language=="ru" ? "Снимите галочку Enable pickup" : "Uncheck Enable pickup")) throw new Exception("Missing mandatory pickup-off instruction");
            if (!steps[1].Instructions.Contains("Focusrite USB MIDI")) throw new Exception("Missing port distinction");
            if (!steps[3].Instructions.Contains(language=="ru" ? "а не установленный gain" : "not the gain setting")) throw new Exception("Meters confused with gain");
        }
    } finally { Ui.Initialize(before); }
});
Console.WriteLine($"{passed} scenarios passed");

sealed class FakeMidi : IDisposable {
    public int Created, Disposed;
    public List<int> Calls = new();
    public void Dispose() => Disposed=Environment.CurrentManagedThreadId;
}
sealed class FakeHttp(System.Net.HttpStatusCode status,string json) : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
        if (request.RequestUri?.Host != "api.github.com" || request.Headers.UserAgent.Count==0) throw new Exception("Invalid GitHub request");
        return Task.FromResult(new HttpResponseMessage(status) { Content=new StringContent(json) });
    }
}
