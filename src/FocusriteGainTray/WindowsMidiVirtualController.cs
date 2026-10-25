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
    public const string DeviceName = "Focusrite Gain Tray";
    private MidiSession? _session;
    private MidiVirtualDevice? _device;
    private MidiEndpointConnection? _connection;

    public bool IsConnected => _connection is not null;
    public string Status { get; private set; } = "Not initialized";

    public void Connect()
    {
        Disconnect();

        // Windows 11 24H2 lacks the required Windows.Devices.Midi2 API.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26200))
            throw new NotSupportedException("Нужна Windows 11 25H2 (build 26200+) и Windows MIDI Services Preview 11.");

        if (!MidiApi.EnsureServiceAvailable())
            throw new InvalidOperationException("Windows MIDI Services недоступен или выбран Legacy API Mode.");

        if (!MidiVirtualDeviceManager.IsTransportAvailable)
            throw new InvalidOperationException("Виртуальный MIDI Device transport отключён или отсутствует.");

        try
        {
            var endpointInfo = new MidiDeclaredEndpointInfo
            {
                Name = DeviceName,
                ProductInstanceId = "FocusriteGainTray_Controller_01", // no spaces; known limitation
                SpecificationVersionMajor = 1,
                SpecificationVersionMinor = 1,
                SupportsMidi10Protocol = true,
                SupportsMidi20Protocol = false,
                HasStaticFunctionBlocks = true,
                SupportsReceivingJitterReductionTimestamps = false,
                SupportsSendingJitterReductionTimestamps = false
            };

            var config = new MidiVirtualDeviceCreationConfig(
                DeviceName,
                "MIDI 1.0 CC controller for official Focusrite Control 2 mappings",
                "Focusrite Gain Tray Community Project",
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

            _session = MidiSession.Create(DeviceName)
                ?? throw new InvalidOperationException("MidiSession.Create вернул null");

            _device = MidiVirtualDeviceManager.CreateVirtualDevice(config)
                ?? throw new InvalidOperationException("CreateVirtualDevice вернул null");

            // Plugin MUST be added before opening the device-side connection.
            _device.SuppressHandledMessages = true;
            _connection = _session.CreateEndpointConnection(_device.DeviceEndpointDeviceId)
                ?? throw new InvalidOperationException("CreateEndpointConnection вернул null");

            if (_connection.AddMessageProcessingPlugin(_device) != MidiMessageProcessingPluginAddResult.Succeeded)
                throw new InvalidOperationException("Не удалось зарегистрировать MIDI Virtual Device plugin");

            if (!_connection.Open())
                throw new InvalidOperationException("Не удалось открыть MIDI endpoint");

            Status = "Ready; select 'Focusrite Gain Tray' in Focusrite Control 2 > Preferences > Application";
            Log.Write("Virtual MIDI 1.0 endpoint created: " + DeviceName);
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
        if (_connection is null) throw new InvalidOperationException("Виртуальный MIDI-порт ещё не подключён");

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
            throw new IOException($"MIDI CC {controllerNumber}={value} не отправлен");

        Log.Write($"Sent CC{controllerNumber}={value} on MIDI channel 1 (Analog {analogInput})");
    }

    public void Disconnect()
    {
        if (_connection is not null && _session is not null)
        {
            try { _session.DisconnectEndpointConnection(_connection.ConnectionId); }
            catch (Exception ex) { Log.Write("DisconnectEndpointConnection: " + ex); }
        }
        _connection = null;
        _device = null;
        _session?.Dispose();
        _session = null;
        Status = "Disconnected";
    }

    public void Dispose() => Disconnect();
}
