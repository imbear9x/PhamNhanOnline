using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using LiteNetLib;
using System.Text.Json;

class AuthClientListener : INetEventListener
{
    private const string TestUsername = "khoivu";
    private const string PreferredPassword = "hihi@admin";
    private const string FallbackPassword = "t##AAAAadmin";
    private const string RegisterEmail = "khoivu@example.com";
    private const int DefaultServerId = 1;
    private const int DefaultModelId = 1;

    private readonly object _sync = new();
    private bool _flowFinished;
    private string _currentPassword = PreferredPassword;
    private bool _fallbackTried;
    private bool _registerTried;
    private int _createAttempts;
    private Guid _targetCharacterId;

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
        Logger.Info("Connected to server.");
        SendLogin(peer, _currentPassword);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        reader.Recycle();

        var packet = PacketSerializer.Deserialize(bytes);
        if (packet is null)
        {
            Logger.Error($"Deserialize failed. ByteLength={bytes.Length}");
            return;
        }

        try
        {
            switch (packet)
            {
                case RegisterResultPacket registerResult:
                {
                    Logger.Info($"Register result: Success={registerResult.Success}, Code={registerResult.Code}");
                    if (registerResult.Success is true || registerResult.Code == MessageCode.LoginAlreadyExists)
                    {
                        SendLogin(peer, _currentPassword);
                    }
                    else
                    {
                        MarkFlowFinished();
                    }

                    break;
                }
                case LoginResultPacket loginResult:
                {
                    Logger.Info($"Login result: Success={loginResult.Success}, Code={loginResult.Code}, AccountId={loginResult.AccountId}");

                    if (loginResult.Success is true)
                    {
                        SendGetCharacterList(peer);
                    }
                    else if (loginResult.Code == MessageCode.InvalidCredentials && !_fallbackTried)
                    {
                        _fallbackTried = true;
                        _currentPassword = FallbackPassword;
                        Logger.Info("Preferred password login failed. Retrying fallback password.");
                        SendLogin(peer, _currentPassword);
                    }
                    else if (loginResult.Code == MessageCode.InvalidCredentials && !_registerTried)
                    {
                        _registerTried = true;
                        _currentPassword = PreferredPassword;
                        Logger.Info("Login failed on both passwords. Trying register then login.");
                        SendRegister(peer, _currentPassword);
                    }
                    else
                    {
                        MarkFlowFinished();
                    }

                    break;
                }
                case GetCharacterListResultPacket listResult:
                {
                    Logger.Info($"GetCharacterList result: Success={listResult.Success}, Code={listResult.Code}");

                    if (listResult.Success is not true)
                    {
                        MarkFlowFinished();
                        break;
                    }

                    var characters = listResult.Characters ?? new List<CharacterModel>();
                    Logger.Info($"Character list count: {characters.Count}");

                    for (var i = 0; i < characters.Count; i++)
                    {
                        var c = characters[i];
                        Logger.Info(
                            $"Character[{i}]: Id={c.CharacterId}, Name={c.Name}, Server={c.WorldServerId}, Model={c.Appearance.ModelId}, CreatedUnixMs={c.CreatedUnixMs}");
                    }

                    if (characters.Count == 0)
                    {
                        SendCreateCharacter(peer, BuildNextCharacterName());
                    }
                    else
                    {
                        _targetCharacterId = characters[0].CharacterId;
                        SendGetCharacterData(peer, _targetCharacterId);
                    }

                    break;
                }
                case CreateCharacterResultPacket createResult:
                {
                    Logger.Info($"CreateCharacter result: Success={createResult.Success}, Code={createResult.Code}");

                    if (createResult.Success is true && createResult.Character.HasValue)
                    {
                        _targetCharacterId = createResult.Character.Value.CharacterId;
                        Logger.Info($"Created character id: {_targetCharacterId}");
                        SendGetCharacterData(peer, _targetCharacterId);
                        break;
                    }

                    if (createResult.Code == MessageCode.CharacterNameAlreadyExists && _createAttempts < 5)
                    {
                        Logger.Info("Character name already exists, retrying with another name.");
                        SendCreateCharacter(peer, BuildNextCharacterName());
                        break;
                    }

                    MarkFlowFinished();
                    break;
                }
                case GetCharacterDataResultPacket dataResult:
                {
                    Logger.Info($"GetCharacterData result: Success={dataResult.Success}, Code={dataResult.Code}");

                    if (dataResult.Success is true && dataResult.Character.HasValue)
                    {
                        LogCharacterData(dataResult.Character.Value, dataResult.BaseStats, dataResult.CurrentState);
                        Logger.Info("Character data loaded successfully.");
                    }
                    else
                    {
                        Logger.Error("Failed to load character data.");
                    }

                    MarkFlowFinished();
                    break;
                }
                case CharacterBaseStatsChangedPacket baseStatsChanged:
                {
                    Logger.Info($"CharacterBaseStatsChanged received: HasBaseStats={baseStatsChanged.BaseStats.HasValue}");
                    break;
                }
                case CharacterCurrentStateChangedPacket currentStateChanged:
                {
                    Logger.Info($"CharacterCurrentStateChanged received: HasCurrentState={currentStateChanged.CurrentState.HasValue}");
                    break;
                }
                default:
                {
                    Logger.Info($"Unhandled packet type: {packet.GetType().Name}");
                    break;
                }
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

            MarkFlowFinished();
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
        Logger.Info($"Sent {packet.GetType().Name} (bytes={data.Length})");
    }

    private void SendLogin(NetPeer peer, string password)
    {
        var loginPacket = new LoginPacket
        {
            Username = TestUsername,
            Password = password
        };
        SendPacket(peer, loginPacket);
    }

    private void SendRegister(NetPeer peer, string password)
    {
        var registerPacket = new RegisterPacket
        {
            Username = TestUsername,
            Password = password,
            Email = RegisterEmail
        };

        SendPacket(peer, registerPacket);
    }

    private void SendGetCharacterList(NetPeer peer)
    {
        SendPacket(peer, new GetCharacterListPacket());
    }

    private void SendCreateCharacter(NetPeer peer, string name)
    {
        var packet = new CreateCharacterPacket
        {
            Name = name,
            ServerId = DefaultServerId,
            ModelId = DefaultModelId
        };

        Logger.Info($"Creating character with name: {name}");
        SendPacket(peer, packet);
    }

    private void SendGetCharacterData(NetPeer peer, Guid characterId)
    {
        SendPacket(peer, new GetCharacterDataPacket
        {
            CharacterId = characterId
        });
    }

    private string BuildNextCharacterName()
    {
        _createAttempts++;
        return $"khoivu_{Random.Shared.Next(1000, 9999)}";
    }

    private static void LogCharacterData(
        CharacterModel character,
        CharacterBaseStatsModel? baseStats,
        CharacterCurrentStateModel? currentState)
    {
        Logger.Info(
            "Character data: " +
            $"Id={character.CharacterId}, Owner={character.OwnerAccountId}, Name={character.Name}, " +
            $"Server={character.WorldServerId}, Model={character.Appearance.ModelId}, Gender={character.Appearance.Gender}, " +
            $"Hair={character.Appearance.HairColor}, Eye={character.Appearance.EyeColor}, Face={character.Appearance.FaceId}, " +
            $"CreatedUnixMs={character.CreatedUnixMs}");

        if (baseStats.HasValue)
        {
            var s = baseStats.Value;
            Logger.Info(
                "Character base stats: " +
                $"CharacterId={s.CharacterId}, Realm={s.RealmTemplateId}, Cultivation={s.Cultivation}, " +
                $"BaseHp={s.BaseHp}, BaseMp={s.BaseMp}, BasePhysique={s.BasePhysique}, BaseAttack={s.BaseAttack}, BaseSpeed={s.BaseSpeed}, " +
                $"BaseSpiritualSense={s.BaseSpiritualSense}, BaseStamina={s.BaseStamina}, LifespanBonus={s.LifespanBonus}, " +
                $"BaseFortune={s.BaseFortune}, BasePotential={s.BasePotential}");
        }
        else
        {
            Logger.Info("Character base stats: <null>");
        }

        if (currentState.HasValue)
        {
            var s = currentState.Value;
            Logger.Info(
                "Character current state: " +
                $"CharacterId={s.CharacterId}, CurrentHp={s.CurrentHp}, CurrentMp={s.CurrentMp}, CurrentStamina={s.CurrentStamina}, RemainingLifespan={s.RemainingLifespan}, CurrentMapId={s.CurrentMapId}, " +
                $"CurrentPosX={s.CurrentPosX}, CurrentPosY={s.CurrentPosY}, IsDead={s.IsDead}, CurrentState={s.CurrentState}, " +
                $"LastSavedUnixMs={s.LastSavedUnixMs}");
        }
        else
        {
            Logger.Info("Character current state: <null>");
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.Info($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        MarkFlowFinished();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"Network error: {socketError}");
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

        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;
        var timeoutSeconds = Math.Max(5, GetIntArg(args, "--timeoutSec=", 20));
        var waitForInput = GetBoolArg(args, "--waitForInput=", false);

        var authListener = new AuthClientListener();
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
}
