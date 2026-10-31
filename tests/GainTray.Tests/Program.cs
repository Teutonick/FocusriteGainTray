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
Check("logon waits for MIDI and returned startup failures retry without sending gain", () => {
    var events = new List<string>(); int attempts = 0;
    MidiStartup.RunAsync(true, () => { events.Add("connect"); return Task.FromResult(++attempts == 3); },
        () => false, () => false, duration => { events.Add($"wait:{duration.TotalSeconds}"); return Task.CompletedTask; }).GetAwaiter().GetResult();
    Equal("wait:15,connect,wait:5,connect,wait:5,connect", string.Join(',',events));
});
Check("native timeout stops startup attempts immediately", () => {
    int attempts = 0, delays = 0; bool faulted = false;
    MidiStartup.RunAsync(false, () => { attempts++; faulted = true; return Task.FromResult(false); },
        () => faulted, () => false, _ => { delays++; return Task.CompletedTask; }).GetAwaiter().GetResult();
    Equal(1,attempts); Equal(0,delays);
});
Check("startup is bounded and successful connect stops retries", () => {
    foreach(bool success in new[]{false,true}) {
        int attempts = 0;
        MidiStartup.RunAsync(false, () => { attempts++; return Task.FromResult(success); },
            () => false, () => false, _ => Task.CompletedTask).GetAwaiter().GetResult();
        Equal(success ? 1 : 3,attempts);
    }
});
Check("quit during startup delay creates no port", () => {
    bool closing = false;
    MidiStartup.RunAsync(true, () => throw new Exception("Connect ran after quit"), () => false, () => closing,
        _ => { closing = true; return Task.CompletedTask; }).GetAwaiter().GetResult();
});
Check("single instance excludes a second owner and releases on quit", () => {
    string id=Guid.NewGuid().ToString("N"), machine=@"Global\FocGrain.Test."+id, legacy=@"Local\FocGrain.Test."+id;
    Equal(true,SingleInstance.TryAcquire(machine,legacy,out var first));
    try { Equal(false,SingleInstance.TryAcquire(machine,legacy,out var duplicate)); Equal<SingleInstance?>(null,duplicate); }
    finally { first!.Dispose(); }
    Equal(true,SingleInstance.TryAcquire(machine,legacy,out var next)); next!.Dispose();
});
Check("legacy process blocks new owner without stranding the machine lock", () => {
    string id=Guid.NewGuid().ToString("N"), machine=@"Global\FocGrain.Test."+id, legacy=@"Local\FocGrain.Test."+id;
    using(var oldVersion = new Mutex(true,legacy)) {
        Equal(false,SingleInstance.TryAcquire(machine,legacy,out var duplicate));
        Equal<SingleInstance?>(null,duplicate);
        oldVersion.ReleaseMutex();
    }
    Equal(true,SingleInstance.TryAcquire(machine,legacy,out var next)); next!.Dispose();
});
Check("WinMM input matching does not accept QA or unrelated ports", () => {
    Equal(true,LegacyMidiInputs.Matches("FocGrain","FocGrain"));
    Equal(true,LegacyMidiInputs.Matches("FocGrain (Virtual MIDI Device)","FocGrain"));
    Equal(false,LegacyMidiInputs.Matches("FocGrain QA","FocGrain"));
    Equal(false,LegacyMidiInputs.Matches("Focusrite USB MIDI","FocGrain"));
    Equal(false,LegacyMidiInputs.Matches("FocGrain","FocGrain QA"));
});
Check("FC2 startup waits for MIDI and handles an existing instance without relaunch", () => {
    int locates=0, launches=0;
    var start = new Fc2Startup(() => { locates++; return "FC2.exe"; }, _ => true, _ => launches++);
    Equal(Fc2StartupResult.WaitingForMidi,start.Run(true,false)); Equal(0,locates);
    Equal(Fc2StartupResult.AlreadyRunning,start.Run(true,true)); Equal(0,launches);
    Equal(Fc2StartupResult.AlreadyHandled,start.Run(true,true)); Equal(1,locates);
});
Check("FC2 startup launches only once after MIDI and respects opt out", () => {
    var events = new List<string>();
    var start = new Fc2Startup(() => { events.Add("locate"); return "installed FC2.exe"; }, path => { events.Add("check:"+path); return false; }, path => events.Add("launch:"+path));
    Equal(Fc2StartupResult.Disabled,start.Run(false,true)); Equal(0,events.Count);
    Equal(Fc2StartupResult.Started,start.Run(true,true));
    Equal("locate,check:installed FC2.exe,launch:installed FC2.exe",string.Join(',',events));
    Equal(Fc2StartupResult.AlreadyHandled,start.Run(true,true)); Equal(3,events.Count);
});
Check("missing FC2 and a launch failure remain retryable", () => {
    string? path=null; int calls=0;
    var start = new Fc2Startup(() => path, _ => false, _ => { if(++calls==1) throw new IOException("launch failed"); });
    Equal(Fc2StartupResult.NotInstalled,start.Run(true,true));
    path="FC2.exe";
    try { start.Run(true,true); throw new Exception("Launch failure was swallowed"); } catch(IOException) { }
    Equal(Fc2StartupResult.Started,start.Run(true,true)); Equal(2,calls);
});
Check("old settings gain FC2 startup while an explicit opt out survives JSON", () => {
    var old=JsonSerializer.Deserialize<AppSettings>("{\"SelectedInput\":2}")!;
    Equal(true,old.StartFocusriteControl2); Equal(2,old.SelectedInput);
    old.StartFocusriteControl2=false;
    Equal(false,JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(old))!.StartFocusriteControl2);
});
Check("upgrade distinguishes clean, dirty, forced and legacy shutdown", () => {
    Equal(false,MidiRecoveryPolicy.NeedsRecovery(new(1,0,4),0,false));
    Equal(true,MidiRecoveryPolicy.NeedsRecovery(new(1,0,3),0,false));
    Equal(true,MidiRecoveryPolicy.NeedsRecovery(new(1,0,4),31,false));
    Equal(true,MidiRecoveryPolicy.NeedsRecovery(new(1,0,4),-1,false));
    Equal(true,MidiRecoveryPolicy.NeedsRecovery(new(1,0,4),0,true));
});
Check("forced service recovery refuses shared, wrong-image and changing ownership", () => {
    var state = new MidiServiceState(3,123,true);
    string expected=Path.GetFullPath("MidiSrv.exe");
    Equal(true,MidiRecoveryPolicy.CanTerminate(state,expected,expected,new[]{"midisrv"}));
    Equal(false,MidiRecoveryPolicy.CanTerminate(state with{OwnProcess=false},expected,expected,new[]{"midisrv"}));
    Equal(false,MidiRecoveryPolicy.CanTerminate(state,expected,expected,new[]{"midisrv","other"}));
    Equal(false,MidiRecoveryPolicy.CanTerminate(state,Path.GetFullPath("other.exe"),expected,new[]{"midisrv"}));
    Equal(false,MidiRecoveryPolicy.CanTerminate(state with{State=4},expected,expected,new[]{"midisrv"}));
    Equal(false,MidiRecoveryPolicy.CanTerminate(state with{ProcessId=0},expected,expected,new[]{"midisrv"}));
});
Check("service restart uses clean stop and reaches Running", () => {
    var service=new FakeService(false);
    MidiRecoveryPolicy.Restart(service,()=>{});
    Equal("stop,start",string.Join(',',service.Calls)); Equal((uint)4,service.Query().State);
});
Check("stuck stop triggers verified fallback; unexpected state fails closed", () => {
    var service=new FakeService(true);
    MidiRecoveryPolicy.Restart(service,()=>{});
    Equal("stop,verified-stop,start",string.Join(',',service.Calls));
    var invalid=new FakeService(true){UnexpectedStop=true};
    try { MidiRecoveryPolicy.Restart(invalid,()=>{}); throw new Exception("Unexpected state was terminated"); } catch(IOException) { }
    Equal("stop",string.Join(',',invalid.Calls));
});
Check("sleep invalidates in-flight success and resume blocks gain until recovery", () => {
    var power = new PowerTransitionState();
    int beforeSleep = power.Generation;
    power.Transition(false);
    Equal(true, power.BlocksGain); Equal(false, power.AcceptsCompletion(beforeSleep));
    int resumed = power.Transition(true);
    Equal(true, power.BlocksGain); Equal(false, power.AcceptsCompletion(beforeSleep));
    Equal(true, power.AcceptsCompletion(resumed));
    power.CompleteRecovery(resumed); Equal(false, power.BlocksGain);
});
Check("another sleep or resume invalidates previous recovery completion", () => {
    var power = new PowerTransitionState();
    int firstResume = power.Transition(true);
    power.Transition(false);
    power.CompleteRecovery(firstResume);
    Equal(true, power.BlocksGain); Equal(false, power.AcceptsCompletion(firstResume));
    int nextResume = power.Transition(true);
    power.CompleteRecovery(firstResume); Equal(true, power.BlocksGain);
    power.CompleteRecovery(nextResume); Equal(false, power.BlocksGain);
});
Check("cross-account machine recovery supersedes only older valid dirty markers", () => {
    var dirty = DateTimeOffset.Parse("2026-10-28T12:32:00+03:00");
    string marker = dirty.ToString("O");
    Equal(true, RecoveryReceipt.Supersedes(marker, dirty.AddSeconds(1).UtcTicks));
    Equal(false, RecoveryReceipt.Supersedes(marker, dirty.UtcTicks));
    Equal(false, RecoveryReceipt.Supersedes(marker, dirty.AddSeconds(-1).UtcTicks));
    Equal(false, RecoveryReceipt.Supersedes("unknown", dirty.AddSeconds(1).UtcTicks));
    Equal(false, RecoveryReceipt.Supersedes(marker, 0));
    Equal(false, RecoveryReceipt.Supersedes(marker, long.MaxValue));
    Equal(true, RecoveryReceipt.Supersedes(dirty.ToUniversalTime().ToString("O"), dirty.AddSeconds(1).UtcTicks));
});
Check("logs rotate before overflow and retain at most one MiB including previous", () => {
    string dir = Path.Combine(Path.GetTempPath(), "focgrain-log-" + Guid.NewGuid().ToString("N"));
    string path = Path.Combine(dir, "application.log");
    try {
        Directory.CreateDirectory(dir);
        File.WriteAllText(path, new string('x', Log.FileLimit * 2));
        File.WriteAllText(path + ".previous", new string('y', Log.FileLimit * 2));
        Log.Append(path, "newest\n");
        Equal(true, File.ReadAllText(path).EndsWith("newest\n"));
        for (int i = 0; i < 6; i++) Log.Append(path, new string('я', Log.FileLimit));
        long total = new FileInfo(path).Length + new FileInfo(path + ".previous").Length;
        Equal(true, total <= 1024 * 1024);
        Equal(true, File.ReadAllText(path).EndsWith("[log record truncated]\n"));
        var strict = new System.Text.UTF8Encoding(false, true);
        strict.GetString(File.ReadAllBytes(path)); strict.GetString(File.ReadAllBytes(path + ".previous"));
        Equal(2, Directory.GetFiles(dir).Length);
    } finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
});
Check("resume preserves open native endpoint and only checks visibility", () => {
    var calls = new List<string>();
    MidiResumePolicy.Run(true, true, true, () => calls.Add("visibility"), () => calls.Add("create"));
    Equal("visibility", string.Join(',', calls));
});
Check("resume does not recreate stale or partially initialized native objects", () => {
    foreach (var state in new[] { (false, false), (true, false), (false, true) }) {
        try {
            MidiResumePolicy.Run(true, state.Item1, state.Item2,
                () => throw new Exception("Visibility checked on closed connection"),
                () => throw new Exception("Stale connection recreated"));
            throw new Exception("Closed native state accepted");
        } catch (IOException) { }
    }
});
Check("resume permits initial creation only when no native state exists", () => {
    int created = 0;
    MidiResumePolicy.Run(false, false, false, () => throw new Exception("No endpoint exists"), () => created++);
    Equal(1, created);
});
Check("resume visibility failure keeps endpoint and remains retryable without replay", () => {
    int checks = 0;
    void CheckVisibility() { if (++checks == 1) throw new IOException("WinMM not ready"); }
    try { MidiResumePolicy.Run(true, true, true, CheckVisibility, () => throw new Exception("Recreated")); } catch (IOException) { }
    MidiResumePolicy.Run(true, true, true, CheckVisibility, () => throw new Exception("Recreated"));
    Equal(2, checks);
});
Console.WriteLine($"{passed} scenarios passed");

sealed class FakeMidi : IDisposable {
    public int Created, Disposed;
    public List<int> Calls = new();
    public void Dispose() => Disposed=Environment.CurrentManagedThreadId;
}
sealed class FakeService(bool stuck) : IMidiServiceRecovery {
    private uint _state=4;
    public bool UnexpectedStop;
    public List<string> Calls = new();
    public MidiServiceState Query()=>new(_state,123,true);
    public void Stop(){Calls.Add("stop");_state=UnexpectedStop?2u:stuck?3u:1u;}
    public void TerminateVerifiedService(MidiServiceState state){Calls.Add("verified-stop");_state=1;}
    public void Start(){Calls.Add("start");_state=4;}
}
sealed class FakeHttp(System.Net.HttpStatusCode status,string json) : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
        if (request.RequestUri?.Host != "api.github.com" || request.Headers.UserAgent.Count==0) throw new Exception("Invalid GitHub request");
        return Task.FromResult(new HttpResponseMessage(status) { Content=new StringContent(json) });
    }
}
