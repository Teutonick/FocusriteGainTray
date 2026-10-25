using Windows.Devices.Midi2;
using Windows.Devices.Midi2.Enumeration;
using Windows.Devices.Midi2.Transports.Virtual;
using Windows.Devices.Midi2.Utilities.Messages;

namespace FocusriteGainTray;

/// <summary>
/// Official Windows MIDI Services app-hosted virtual MIDI device.
/// Sends MIDI 1.0 Control Change packets (UMP Message Type 2), NOT MIDI 2.0 CC.
/// On Windows 11 25H2 + WMS Preview 11, a legacy MIDI 1.0 OUTPUT port
/// is intended to be enumerated by applications such as Focusrite Control 2.
/// </summary>
internal sealed class WindowsMidiVirtualController : IDisposable
{
    public const string DeviceName = "FocGrain";
    private readonly string _deviceName;
    public WindowsMidiVirtualController(string deviceName = DeviceName) => _deviceName = deviceName;
    private MidiSession? _session;
    private MidiVirtualDevice? _device;
    private MidiEndpointConnection? _connection;

    public bool IsConnected => _connection is not null;
    public string Status { get; private set; } = "Not initialized";

    public void Connect()
    {
        Disconnect();
        Log.Write($"MIDI connect started: {_deviceName}; apartment={Thread.CurrentThread.GetApartmentState()}");

        // Windows 11 24H2 lacks the required Windows.Devices.Midi2 API.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26200))
            throw new NotSupportedException(Ui.T("Нужна Windows 11 25H2 (build 26200+) и Windows MIDI Services Preview 11.", "Windows 11 25H2 (build 26200+) and Windows MIDI Services Preview 11 are required."));

        if (!MidiApi.EnsureServiceAvailable())
            throw new InvalidOperationException(Ui.T("Windows MIDI Services недоступен или выбран Legacy API Mode.", "Windows MIDI Services is unavailable, or Legacy API Mode is selected."));

        if (!MidiVirtualDeviceManager.IsTransportAvailable)
            throw new InvalidOperationException(Ui.T("Виртуальный MIDI Device transport отключён или отсутствует.", "Virtual Device App Transport is disabled or missing."));

        try
        {
            var endpointInfo = new MidiDeclaredEndpointInfo
            {
                Name = _deviceName,
                ProductInstanceId = _deviceName == DeviceName ? "FocusriteGainTray_Controller_01" : "FocGrain_QA_Controller", // no spaces
                SpecificationVersionMajor = 1,
                SpecificationVersionMinor = 1,
                SupportsMidi10Protocol = true,
                SupportsMidi20Protocol = false,
                HasStaticFunctionBlocks = true,
                SupportsReceivingJitterReductionTimestamps = false,
                SupportsSendingJitterReductionTimestamps = false
            };

            var config = new MidiVirtualDeviceCreationConfig(
                _deviceName,
                "MIDI 1.0 CC controller for official Focusrite Control 2 mappings",
                "FocGrain Community Project",
                endpointInfo)
            {
                // Critical: show a WinMM/WinRT MIDI 1.0 port to older MIDI apps.
                CreateOnlyUmpEndpoints = false
            };

            // Group 0 is a MIDI 1.0 output from the device's perspective.
            config.FunctionBlocks.Add(new MidiFunctionBlock
            {
                Number = 0,
                Name = "Gain CC output",
                IsActive = true,
                FirstGroup = new MidiGroup(0),
                GroupCount = 1,
                Direction = MidiFunctionBlockDirection.BlockOutput,
                UIHint = MidiFunctionBlockUIHint.Sender,
                RepresentsMidi10Connection = MidiFunctionBlockRepresentsMidi10Connection.YesBandwidthUnrestricted,
                MidiCIMessageVersionFormat = 0,
                MaxSystemExclusive8Streams = 0
            });

            Log.Write("MIDI connect: creating session");
            _session = MidiSession.Create(_deviceName)
                ?? throw new InvalidOperationException(Ui.T("MidiSession.Create вернул null", "MidiSession.Create returned null"));

            Log.Write("MIDI connect: creating virtual device");
            _device = MidiVirtualDeviceManager.CreateVirtualDevice(config)
                ?? throw new InvalidOperationException(Ui.T("CreateVirtualDevice вернул null", "CreateVirtualDevice returned null"));

            // Plugin MUST be added before opening the device-side connection.
            _device.SuppressHandledMessages = true;
            _connection = _session.CreateEndpointConnection(_device.DeviceEndpointDeviceId)
                ?? throw new InvalidOperationException(Ui.T("CreateEndpointConnection вернул null", "CreateEndpointConnection returned null"));

            if (_connection.AddMessageProcessingPlugin(_device) != MidiMessageProcessingPluginAddResult.Succeeded)
                throw new InvalidOperationException(Ui.T("Не удалось зарегистрировать MIDI Virtual Device plugin", "Could not register the MIDI Virtual Device plugin"));

            Log.Write("MIDI connect: opening device-side connection");
            if (!_connection.Open())
                throw new InvalidOperationException(Ui.T("Не удалось открыть MIDI endpoint", "Could not open the MIDI endpoint"));

            Status = "Ready; select 'FocGrain' in Focusrite Control 2 > Preferences > Application";
            Log.Write("Virtual MIDI 1.0 endpoint created: " + _deviceName);
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    public void SendGain(int analogInput, int value)
    {
        if (analogInput is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(analogInput));
        if (value is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(value));
        if (_connection is null) throw new InvalidOperationException(Ui.T("Виртуальный MIDI-порт ещё не подключён", "The virtual MIDI port is not connected"));

        // Focusrite Control 2 documented input gain mapping: Analog 1 = CC 12, Analog 2 = CC 13.
        byte controllerNumber = GainState.Controller(analogInput);
        var packet = MidiMessageBuilder.BuildMidi1ChannelVoiceMessage(
            0,                              // send immediately
            new MidiGroup(0),
            Midi1ChannelVoiceMessageStatus.ControlChange,
            new MidiChannel(0),             // MIDI channel 1 (zero-based)
            controllerNumber,
            (byte)value);

        if (packet is null || !MidiEndpointConnection.SendMessageSucceeded(_connection.SendSingleMessagePacket(packet)))
            throw new IOException(Ui.T($"MIDI CC {controllerNumber}={value} не отправлен", $"MIDI CC {controllerNumber}={value} was not sent"));

        Log.Write($"Sent CC{controllerNumber}={value} on MIDI channel 1 (Analog {analogInput})");
    }

    public void Disconnect()
    {
        bool hadSession = _session is not null;
        if (hadSession) Log.Write("MIDI disconnect started: " + _deviceName);
        if (_connection is not null && _session is not null)
        {
            Log.Write("MIDI disconnect: closing endpoint connection");
            try { _session.DisconnectEndpointConnection(_connection.ConnectionId); }
            catch (Exception ex) { Log.Write("DisconnectEndpointConnection: " + ex); }
            Log.Write("MIDI disconnect: endpoint close returned");
        }
        _connection = null;
        _device = null;
        if (hadSession) Log.Write("MIDI disconnect: closing session");
        _session?.Dispose();
        _session = null;
        Status = "Disconnected";
        if (hadSession) Log.Write("MIDI disconnect completed: " + _deviceName);
    }

    public void Dispose() => Disconnect();
}
