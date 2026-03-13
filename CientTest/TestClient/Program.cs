using System.Text.Encodings.Web;
using System.Text.Json;
using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using LiteNetLib;

sealed class FlowOptions
{
    public string Username { get; init; } = "test00122";
    public string Password { get; init; } = "test@12333333";
    public string Email { get; init; } = "test00122@test.com";
    public string CharacterName { get; init; } = "HanLi";
    public int ServerId { get; init; } = 1;
    public int ModelId { get; init; } = 1;
    public string JsonOutputPath { get; init; } = Path.Combine("tmp_codex", "vertical_slice_test1_hanli_flow.json");
    public int HoldConnectionSeconds { get; init; } = 0;
    public bool SendCreateCharacterAfterLifespanExpired { get; init; }
    public string ProbeCharacterName { get; init; } = "AfterExpireProbe";
}

sealed class FlowJsonRecord
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CharacterModel[] CharacterList { get; set; } = Array.Empty<CharacterModel>();
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public List<string> Events { get; set; } = new();
}

class AuthClientListener : INetEventListener
{
    private const int LifespanExpiredReason = 1;
    private readonly object _sync = new();
    private readonly FlowOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly FlowJsonRecord _record;

    private bool _flowFinished;
    private bool _createProbeSent;
    private Guid _targetCharacterId;
    private DateTime? _holdUntilUtc;
    private NetPeer? _peer;

    public AuthClientListener(FlowOptions options)
    {
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        _record = new FlowJsonRecord
        {
            Username = options.Username,
            Email = options.Email
        };
    }

    public bool LoginFinished
    {
        get
        {
            lock (_sync)
            {
                return _flowFinished;
            }
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _peer = peer;
        AppendEvent("ConnectedToServer");
        SendLogin(peer);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        reader.Recycle();

        var packet = PacketSerializer.Deserialize(bytes);
        if (packet is null)
        {
            Logger.Error($"Deserialize failed. ByteLength={bytes.Length}");
            AppendEvent("DeserializeFailed");
            MarkFlowFinished();
            return;
        }

        try
        {
            switch (packet)
            {
                case RegisterResultPacket registerResult:
                    HandleRegisterResult(peer, registerResult);
                    break;
                case LoginResultPacket loginResult:
                    HandleLoginResult(peer, loginResult);
                    break;
                case GetCharacterListResultPacket listResult:
                    HandleCharacterListResult(peer, listResult);
                    break;
                case CreateCharacterResultPacket createResult:
                    HandleCreateCharacterResult(peer, createResult);
                    break;
                case GetCharacterDataResultPacket dataResult:
                    HandleCharacterDataResult(dataResult);
                    break;
                case CharacterBaseStatsChangedPacket baseStatsChanged:
                    Logger.Info($"CharacterBaseStatsChanged received: HasBaseStats={baseStatsChanged.BaseStats.HasValue}");
                    AppendEvent("CharacterBaseStatsChanged");
                    break;
                case CharacterCurrentStateChangedPacket currentStateChanged:
                    Logger.Info($"CharacterCurrentStateChanged received: HasCurrentState={currentStateChanged.CurrentState.HasValue}");
                    AppendEvent("CharacterCurrentStateChanged");
                    break;
                case CharacterStateTransitionPacket stateTransition:
                    HandleCharacterStateTransition(stateTransition);
                    break;
                default:
                    Logger.Info($"Unhandled packet type: {packet.GetType().Name}");
                    AppendEvent($"Unhandled:{packet.GetType().Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Client packet handler error: {packet.GetType().Name}");
            PacketIncidentCapture.Log(new PacketIncidentRecord
            {
                CapturedAtUtc = DateTime.UtcNow,
                Source = "Client",
                IncidentType = "ClientPacketHandleException",
                ConnectionId = peer.Id,
                RemoteEndPoint = $"{peer.Address}:{peer.Port}",
                ChannelNumber = channelNumber,
                DeliveryMethod = deliveryMethod.ToString(),
                PacketType = packet.GetType().FullName ?? packet.GetType().Name,
                PacketJson = TrySerializePacket(packet),
                PacketPayloadBase64 = Convert.ToBase64String(bytes),
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                ExceptionMessage = ex.Message,
                ExceptionStackTrace = ex.StackTrace
            });

            AppendEvent($"Error:{packet.GetType().Name}");
            PersistFlowJson();
            MarkFlowFinished();
        }
    }

    private void HandleRegisterResult(NetPeer peer, RegisterResultPacket registerResult)
    {
        Logger.Info($"Register result: Success={registerResult.Success}, Code={registerResult.Code}");
        AppendEvent($"Register:{registerResult.Code}");
        if (registerResult.Success is true || registerResult.Code == MessageCode.LoginAlreadyExists)
        {
            SendLogin(peer);
            return;
        }

        PersistFlowJson();
        MarkFlowFinished();
    }

    private void HandleLoginResult(NetPeer peer, LoginResultPacket loginResult)
    {
        Logger.Info($"Login result: Success={loginResult.Success}, Code={loginResult.Code}, AccountId={loginResult.AccountId}");
        AppendEvent($"Login:{loginResult.Code}");

        if (loginResult.Success is true)
        {
            SendGetCharacterList(peer);
            return;
        }

        PersistFlowJson();
        MarkFlowFinished();
    }

    private void HandleCharacterListResult(NetPeer peer, GetCharacterListResultPacket listResult)
    {
        Logger.Info($"GetCharacterList result: Success={listResult.Success}, Code={listResult.Code}");
        AppendEvent($"GetCharacterList:{listResult.Code}");

        if (listResult.Success is not true)
        {
            PersistFlowJson();
            MarkFlowFinished();
            return;
        }

        var characters = listResult.Characters?.ToArray() ?? Array.Empty<CharacterModel>();
        _record.CharacterList = characters;
        PersistFlowJson();

        Logger.Info($"Character list count: {characters.Length}");
        foreach (var character in characters)
        {
            Logger.Info($"CharacterList item: Id={character.CharacterId}, Name={character.Name}, Server={character.WorldServerId}, Model={character.Appearance.ModelId}");
        }

        if (characters.Length == 0)
        {
            Logger.Info("Character list is empty.");
            PersistFlowJson();
            MarkFlowFinished();
            return;
        }

        _targetCharacterId = FindTargetCharacterId(characters);
        SendGetCharacterData(peer, _targetCharacterId);
    }

    private void HandleCreateCharacterResult(NetPeer peer, CreateCharacterResultPacket createResult)
    {
        Logger.Info($"CreateCharacter result: Success={createResult.Success}, Code={createResult.Code}");
        AppendEvent($"CreateCharacter:{createResult.Code}");

        if (createResult.Success is true && createResult.Character.HasValue)
        {
            _targetCharacterId = createResult.Character.Value.CharacterId;
            _record.Character = createResult.Character.Value;
            _record.BaseStats = createResult.BaseStats;
            _record.CurrentState = createResult.CurrentState;
            PersistFlowJson();
            SendGetCharacterList(peer);
            return;
        }

        PersistFlowJson();
        MarkFlowFinished();
    }

    private void HandleCharacterDataResult(GetCharacterDataResultPacket dataResult)
    {
        Logger.Info($"GetCharacterData result: Success={dataResult.Success}, Code={dataResult.Code}");
        AppendEvent($"GetCharacterData:{dataResult.Code}");

        if (dataResult.Success is true && dataResult.Character.HasValue)
        {
            _record.Character = dataResult.Character.Value;
            _record.BaseStats = dataResult.BaseStats;
            _record.CurrentState = dataResult.CurrentState;
            PersistFlowJson();
            Logger.Info($"Character data written to JSON: {_options.JsonOutputPath}");
        }
        else
        {
            Logger.Error("Failed to load character data.");
            PersistFlowJson();
        }

        if (_options.HoldConnectionSeconds > 0)
        {
            _holdUntilUtc = DateTime.UtcNow.AddSeconds(_options.HoldConnectionSeconds);
            AppendEvent($"HoldConnection:{_options.HoldConnectionSeconds}s");
            PersistFlowJson();
            Logger.Info($"Holding connection open for {_options.HoldConnectionSeconds}s.");
            return;
        }

        MarkFlowFinished();
    }

    private void HandleCharacterStateTransition(CharacterStateTransitionPacket stateTransition)
    {
        Logger.Info($"CharacterStateTransition received: CharacterId={stateTransition.CharacterId}, Reason={stateTransition.Reason}");
        AppendEvent($"CharacterStateTransition:{stateTransition.Reason}");
        PersistFlowJson();

        if (_options.SendCreateCharacterAfterLifespanExpired &&
            !_createProbeSent &&
            stateTransition.Reason == LifespanExpiredReason &&
            _peer is not null)
        {
            _createProbeSent = true;
            AppendEvent("CreateCharacterProbe:SentAfterLifespanExpired");
            PersistFlowJson();
            SendCreateCharacter(_peer, _options.ProbeCharacterName);
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
        Logger.Info($"Sent {packet.GetType().Name} (bytes={data.Length})");
    }

    private void SendRegister(NetPeer peer)
    {
        var registerPacket = new RegisterPacket
        {
            Username = _options.Username,
            Password = _options.Password,
            Email = _options.Email
        };

        SendPacket(peer, registerPacket);
    }

    private void SendLogin(NetPeer peer)
    {
        var loginPacket = new LoginPacket
        {
            Username = _options.Username,
            Password = _options.Password
        };

        SendPacket(peer, loginPacket);
    }

    private void SendGetCharacterList(NetPeer peer)
    {
        SendPacket(peer, new GetCharacterListPacket());
    }

    private void SendCreateCharacter(NetPeer peer, string characterName)
    {
        Logger.Info($"Creating character with name: {characterName}");
        SendPacket(peer, new CreateCharacterPacket
        {
            Name = characterName,
            ServerId = _options.ServerId,
            ModelId = _options.ModelId
        });
    }

    private void SendGetCharacterData(NetPeer peer, Guid characterId)
    {
        SendPacket(peer, new GetCharacterDataPacket
        {
            CharacterId = characterId
        });
    }

    private Guid FindTargetCharacterId(IReadOnlyList<CharacterModel> characters)
    {
        foreach (var character in characters)
        {
            if (string.Equals(character.Name, _options.CharacterName, StringComparison.Ordinal))
                return character.CharacterId;
        }

        return characters[0].CharacterId;
    }

    private void PersistFlowJson()
    {
        var fullPath = Path.GetFullPath(_options.JsonOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(_record, _jsonOptions));
    }

    private void AppendEvent(string evt)
    {
        _record.Events.Add($"{DateTime.UtcNow:O} {evt}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.Info($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        AppendEvent($"Disconnected:{disconnectInfo.Reason}");
        PersistFlowJson();
        MarkFlowFinished();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"Network error: {socketError}");
        AppendEvent($"NetworkError:{socketError}");
        PersistFlowJson();
        MarkFlowFinished();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        reader.Recycle();
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject();
    }

    private static string TrySerializePacket(IPacket packet)
    {
        try
        {
            return JsonSerializer.Serialize(packet, packet.GetType());
        }
        catch (Exception ex)
        {
            return $"<serialize_failed:{ex.GetType().Name}>";
        }
    }

    private void MarkFlowFinished()
    {
        lock (_sync)
        {
            _flowFinished = true;
        }
    }

    public void Tick()
    {
        if (_holdUntilUtc.HasValue && DateTime.UtcNow >= _holdUntilUtc.Value)
        {
            AppendEvent("HoldConnectionCompleted");
            PersistFlowJson();
            _holdUntilUtc = null;
            MarkFlowFinished();
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Logger.Configure(GetLogRootPath(args));
        PacketIncidentCapture.Configure(GetBoolArg(args, "--packetIncidentCapture=", true));
        Logger.Info($"Packet incident capture enabled: {PacketIncidentCapture.Enabled}");

        if (PacketReplayTool.TryRun(args))
            return;

        if (GetBoolArg(args, "--reconnectMode=", false))
        {
            ReconnectServer.Run(args);
            return;
        }

        var options = BuildFlowOptions(args);
        Logger.Info($"Vertical slice account={options.Username}, email={options.Email}, character={options.CharacterName}, json={options.JsonOutputPath}");

        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;
        var timeoutSeconds = Math.Max(5, GetIntArg(args, "--timeoutSec=", 30));
        var waitForInput = GetBoolArg(args, "--waitForInput=", false);

        var authListener = new AuthClientListener(options);
        var netManager = new NetManager(authListener)
        {
            AutoRecycle = true
        };

        if (!netManager.Start())
        {
            Logger.Error("Failed to start LiteNetLib client.");
            return;
        }

        Logger.Info("Connecting to server...");
        netManager.Connect(serverAddress, serverPort, string.Empty);
        var startedAt = DateTime.UtcNow;

        while (!authListener.LoginFinished &&
               (DateTime.UtcNow - startedAt).TotalSeconds < timeoutSeconds)
        {
            netManager.PollEvents();
            authListener.Tick();
            Thread.Sleep(15);
        }

        if (!authListener.LoginFinished)
        {
            Logger.Error($"Client flow timed out after {timeoutSeconds}s.");
        }

        netManager.Stop();

        Logger.Info("Finished client run.");

        if (waitForInput)
            Console.ReadLine();
    }

    private static FlowOptions BuildFlowOptions(string[] args)
    {
        return new FlowOptions
        {
            Username = GetStringArg(args, "--username=", "test00122"),
            Password = GetStringArg(args, "--password=", "test@12333333"),
            Email = GetStringArg(args, "--email=", "test00122@test.com"),
            CharacterName = GetStringArg(args, "--characterName=", "HanLi"),
            ServerId = GetIntArg(args, "--serverId=", 1),
            ModelId = GetIntArg(args, "--modelId=", 1),
            JsonOutputPath = GetStringArg(args, "--jsonOutput=", Path.Combine("tmp_codex", "vertical_slice_test1_hanli_flow.json")),
            HoldConnectionSeconds = GetIntArg(args, "--holdSec=", 0),
            SendCreateCharacterAfterLifespanExpired = GetBoolArg(args, "--sendCreateAfterExpire=", false),
            ProbeCharacterName = GetStringArg(args, "--probeCharacterName=", "AfterExpireProbe")
        };
    }

    private static string? GetLogRootPath(string[] args)
    {
        const string prefix = "--logRoot=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return null;
    }

    private static bool GetBoolArg(string[] args, string prefix, bool defaultValue)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = arg[prefix.Length..];
            if (bool.TryParse(raw, out var parsed))
                return parsed;
        }

        return defaultValue;
    }

    private static int GetIntArg(string[] args, string prefix, int defaultValue)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(arg[prefix.Length..], out var parsed))
                return parsed;
        }

        return defaultValue;
    }

    private static string GetStringArg(string[] args, string prefix, string defaultValue)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return defaultValue;
    }
}
