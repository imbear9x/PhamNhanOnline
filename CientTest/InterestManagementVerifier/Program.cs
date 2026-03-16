using System.Text.Json;
using GameServer.DTO;
using GameServer.Network;
using GameServer.Network.Interface;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

var runner = new InterestManagementVerifier();
var report = runner.RunAll();

var outputPath = Path.GetFullPath(Path.Combine("tmp_codex", "interest_management_verifier_report.json"));
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true
}));

Console.WriteLine($"Interest management verifier: {(report.Success ? "PASSED" : "FAILED")}");
Console.WriteLine($"Scenarios: {report.Scenarios.Count}, Passed: {report.Scenarios.Count(x => x.Passed)}, Failed: {report.Scenarios.Count(x => !x.Passed)}");
foreach (var scenario in report.Scenarios)
{
    Console.WriteLine($"- [{(scenario.Passed ? "PASS" : "FAIL")}] {scenario.Name}: {scenario.Summary}");
}

Console.WriteLine($"Report: {outputPath}");
return report.Success ? 0 : 1;

internal sealed class InterestManagementVerifier
{
    private readonly GameTimeService _gameTimeService = new(new GameTimeConfig());

    public VerificationReport RunAll()
    {
        var scenarios = new List<ScenarioReport>
        {
            RunScenario("Private home map keeps each player isolated in zone 0", VerifyPrivateHomeIsolation),
            RunScenario("Join map publishes zone-aware map snapshot and nearby observers in public map", VerifyNearbySpawnOnJoin),
            RunScenario("Far players do not spawn until they enter interest radius", VerifyFarPlayersStayHiddenUntilNear),
            RunScenario("Move and state-change packets only reach visible observers", VerifyMoveAndStateReplication)
        };

        return new VerificationReport(
            scenarios.All(x => x.Passed),
            DateTime.UtcNow,
            scenarios);
    }

    private ScenarioReport RunScenario(string name, Action<ScenarioContext> scenario)
    {
        var context = new ScenarioContext(_gameTimeService);
        try
        {
            scenario(context);
            return new ScenarioReport(name, true, "All assertions passed.", context.Assertions);
        }
        catch (Exception ex)
        {
            context.Assertions.Add($"FAILED: {ex.Message}");
            return new ScenarioReport(name, false, ex.Message, context.Assertions);
        }
    }

    private static void VerifyPrivateHomeIsolation(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, mapId: MapCatalog.HomeMapId, zoneIndex: 0, x: 64, y: 64);
        var bob = context.CreatePlayer("Bob", 2, mapId: MapCatalog.HomeMapId, zoneIndex: 0, x: 64, y: 64);

        context.EnterWorld(alice);
        context.EnterWorld(bob);

        context.ExpectMapJoined(alice.ConnectionId, MapCatalog.HomeMapId, 0, "Alice should enter private home zone 0.");
        context.ExpectMapJoined(bob.ConnectionId, MapCatalog.HomeMapId, 0, "Bob should enter private home zone 0.");
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(alice.ConnectionId, 0, "Alice should not see Bob inside her private home map.");
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(bob.ConnectionId, 0, "Bob should not see Alice inside his private home map.");
    }

    private static void VerifyNearbySpawnOnJoin(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 100, y: 100);
        var bob = context.CreatePlayer("Bob", 2, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 120, y: 120);

        context.EnterWorld(alice);
        context.ClearNetworkLog();
        context.EnterWorld(bob);

        context.ExpectMapJoined(bob.ConnectionId, MapCatalog.StarterFarmMapId, 1, "Bob should receive farm map zone 1 on enter.");
        context.ExpectSpawn(bob.ConnectionId, alice.PlayerId, "Bob should observe Alice when entering nearby public zone.");
        context.ExpectSpawn(alice.ConnectionId, bob.PlayerId, "Alice should observe Bob when he enters nearby public zone.");
    }

    private static void VerifyFarPlayersStayHiddenUntilNear(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 100, y: 100);
        var charlie = context.CreatePlayer("Charlie", 3, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 700, y: 700);

        context.EnterWorld(alice);
        context.ClearNetworkLog();
        context.EnterWorld(charlie);

        context.ExpectPacket<ObservedCharacterSpawnedPacket>(alice.ConnectionId, 0, "Alice should not see Charlie when out of range in public zone.");
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(charlie.ConnectionId, 0, "Charlie should not see Alice when out of range in public zone.");

        context.ClearNetworkLog();
        context.MovePlayer(charlie, 110, 110);

        context.ExpectSpawn(alice.ConnectionId, charlie.PlayerId, "Alice should see Charlie after he moves into interest radius.");
        context.ExpectSpawn(charlie.ConnectionId, alice.PlayerId, "Charlie should see Alice after he moves into interest radius.");
    }

    private static void VerifyMoveAndStateReplication(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 100, y: 100);
        var bob = context.CreatePlayer("Bob", 2, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 120, y: 120);
        var charlie = context.CreatePlayer("Charlie", 3, mapId: MapCatalog.StarterFarmMapId, zoneIndex: 1, x: 700, y: 700);

        context.EnterWorld(alice);
        context.EnterWorld(bob);
        context.EnterWorld(charlie);
        context.ClearNetworkLog();

        context.MovePlayer(bob, 130, 140);
        context.ExpectMove(alice.ConnectionId, bob.PlayerId, 1, "Alice should receive Bob's move while he is visible in zone 1.");
        context.ExpectPacket<ObservedCharacterMovedPacket>(charlie.ConnectionId, 0, "Charlie should not receive Bob's move while far away.");

        context.ClearNetworkLog();
        context.ChangeState(bob, hp: 77, isDead: false, stateCode: 9);
        context.ExpectState(alice.ConnectionId, bob.PlayerId, 1, "Alice should receive Bob's state change while visible.");
        context.ExpectPacket<ObservedCharacterCurrentStateChangedPacket>(charlie.ConnectionId, 0, "Charlie should not receive Bob's state change while far away.");

        context.ClearNetworkLog();
        context.MovePlayer(bob, 700, 700);
        context.ExpectDespawn(alice.ConnectionId, bob.PlayerId, 1, "Alice should receive despawn when Bob leaves her interest radius.");

        context.ClearNetworkLog();
        context.ChangeState(bob, hp: 55, isDead: false, stateCode: 10);
        context.ExpectPacket<ObservedCharacterCurrentStateChangedPacket>(alice.ConnectionId, 0, "Alice should not receive Bob's state after Bob is despawned.");
    }
}

internal sealed class ScenarioContext
{
    private readonly WorldManager _worldManager;
    private readonly WorldInterestService _interestService;
    private readonly CapturingNetworkSender _network;
    private readonly GameTimeService _gameTimeService;
    private int _nextConnectionId = 1000;

    public ScenarioContext(GameTimeService gameTimeService)
    {
        _gameTimeService = gameTimeService;
        _network = new CapturingNetworkSender();
        var mapCatalog = CreateMapCatalog();
        var mapManager = new MapManager(mapCatalog);
        _worldManager = new WorldManager(mapManager);
        _interestService = new WorldInterestService(_worldManager, _network, _gameTimeService);
    }

    public List<string> Assertions { get; } = new();

    public PlayerSession CreatePlayer(string name, int ordinal, int mapId, int zoneIndex, float x, float y)
    {
        var playerId = Guid.Parse($"00000000-0000-0000-0000-{ordinal.ToString("D12")}");
        var character = new CharacterDto(
            playerId,
            Guid.Parse($"10000000-0000-0000-0000-{ordinal.ToString("D12")}"),
            1,
            name,
            new CharacterAppearanceDto(1, 1, 1, 1, 1),
            DateTime.UtcNow);
        var baseStats = new CharacterBaseStatsDto(
            playerId,
            RealmTemplateId: 1,
            RealmLifespan: 100,
            Cultivation: 0,
            BaseHp: 100,
            BaseMp: 80,
            BasePhysique: 10,
            BaseAttack: 10,
            BaseSpeed: 10,
            BaseSpiritualSense: 10,
            BaseStamina: 100,
            LifespanBonus: 0,
            BaseFortune: 0,
            BasePotential: 0,
            UnallocatedPotential: 0,
            CultivationProgress: 0m);
        var currentState = new CharacterCurrentStateDto(
            playerId,
            CurrentHp: 100,
            CurrentMp: 80,
            CurrentStamina: 100,
            LifespanEndGameMinute: 999999,
            CurrentMapId: mapId,
            CurrentZoneIndex: zoneIndex,
            CurrentPosX: x,
            CurrentPosY: y,
            IsDead: false,
            CurrentState: 0,
            CultivationStartedAtUtc: null,
            LastCultivationRewardedAtUtc: null,
            LastSavedAt: DateTime.UtcNow);

        return _worldManager.AddOrUpdatePlayer(
            playerId,
            Interlocked.Increment(ref _nextConnectionId),
            character,
            baseStats,
            currentState);
    }

    public void EnterWorld(PlayerSession player)
    {
        _interestService.EnsurePlayerInWorld(player);
        _interestService.PublishWorldSnapshot(player);
    }

    public void MovePlayer(PlayerSession player, float x, float y)
    {
        var previous = player.RuntimeState.CaptureSnapshot().CurrentState;
        var current = player.RuntimeState.UpdateCurrentState(state => state with
        {
            CurrentPosX = x,
            CurrentPosY = y,
            CurrentMapId = player.MapId == 0 ? MapCatalog.StarterFarmMapId : player.MapId,
            CurrentZoneIndex = player.ZoneIndex
        }).CurrentState;

        player.SynchronizeFromCurrentState(current);
        _interestService.HandlePositionUpdated(player, previous, current);
    }

    public void ChangeState(PlayerSession player, int hp, bool isDead, int stateCode)
    {
        var current = player.RuntimeState.UpdateCurrentState(state => state with
        {
            CurrentHp = hp,
            IsDead = isDead,
            CurrentState = stateCode
        }).CurrentState;

        player.SynchronizeFromCurrentState(current);
        _interestService.NotifyCurrentStateChanged(player, current);
    }

    public void ClearNetworkLog()
    {
        _network.Clear();
    }

    public void ExpectPacket<TPacket>(int connectionId, int expectedCount, string message)
        where TPacket : class, IPacket
    {
        var actual = _network.GetPackets<TPacket>(connectionId).Count;
        Assert(actual == expectedCount, $"{message} Expected={expectedCount}, Actual={actual}");
    }

    public void ExpectMapJoined(int connectionId, int expectedMapId, int expectedZoneIndex, string message)
    {
        var match = _network.GetPackets<MapJoinedPacket>(connectionId)
            .Any(packet => packet.Map.HasValue && packet.Map.Value.MapId == expectedMapId && packet.ZoneIndex == expectedZoneIndex);
        Assert(match, message);
    }

    public void ExpectSpawn(int connectionId, Guid observedCharacterId, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterSpawnedPacket>(connectionId)
            .Any(packet => packet.Character.HasValue && packet.Character.Value.Character.CharacterId == observedCharacterId);
        Assert(match, message);
    }

    public void ExpectDespawn(int connectionId, Guid observedCharacterId, int expectedZoneIndex, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterDespawnedPacket>(connectionId)
            .Any(packet => packet.CharacterId == observedCharacterId && packet.ZoneIndex == expectedZoneIndex);
        Assert(match, message);
    }

    public void ExpectMove(int connectionId, Guid observedCharacterId, int expectedZoneIndex, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterMovedPacket>(connectionId)
            .Any(packet => packet.CharacterId == observedCharacterId && packet.ZoneIndex == expectedZoneIndex);
        Assert(match, message);
    }

    public void ExpectState(int connectionId, Guid observedCharacterId, int expectedZoneIndex, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterCurrentStateChangedPacket>(connectionId)
            .Any(packet => packet.CurrentState.HasValue && packet.CurrentState.Value.CharacterId == observedCharacterId && packet.ZoneIndex == expectedZoneIndex);
        Assert(match, message);
    }

    private void Assert(bool condition, string message)
    {
        Assertions.Add($"{(condition ? "PASS" : "FAIL")}: {message}");
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static MapCatalog CreateMapCatalog()
    {
        var homeTemplate = new MapTemplate(
            TemplateId: MapCatalog.HomeMapId,
            Name: "Player Home",
            Type: MapType.Home,
            ClientMapKey: "map_home_01",
            SpiritualEnergyPerMinute: 100m,
            AdjacentMapIds: [MapCatalog.StarterFarmMapId],
            Width: 256,
            Height: 256,
            CellSize: 32,
            InterestRadius: 96,
            MaxPublicZoneCount: 0,
            MaxPlayersPerZone: 1,
            SupportsCavePlacement: false,
            DefaultSpawnPosition: new System.Numerics.Vector2(64, 64),
            IsPrivatePerPlayer: true);
        var starterTemplate = new MapTemplate(
            TemplateId: MapCatalog.StarterFarmMapId,
            Name: "Starter Plains",
            Type: MapType.Farm,
            ClientMapKey: "map_farm_01",
            SpiritualEnergyPerMinute: 100m,
            AdjacentMapIds: [MapCatalog.HomeMapId],
            Width: 1024,
            Height: 1024,
            CellSize: 64,
            InterestRadius: 160,
            MaxPublicZoneCount: 20,
            MaxPlayersPerZone: 20,
            SupportsCavePlacement: true,
            DefaultSpawnPosition: new System.Numerics.Vector2(128, 128),
            IsPrivatePerPlayer: false);

        var definitions = new Dictionary<int, MapDefinition>
        {
            [MapCatalog.HomeMapId] = new(homeTemplate),
            [MapCatalog.StarterFarmMapId] = new(starterTemplate)
        };
        var starterZones = Enumerable.Range(1, 20)
            .Select(zoneIndex => new MapZoneSlotDefinition(
                MapCatalog.StarterFarmMapId,
                zoneIndex,
                2,
                "medium",
                "Medium",
                1.0m))
            .ToArray();
        var zoneSlots = new Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>>
        {
            [MapCatalog.StarterFarmMapId] = starterZones
        };

        return new MapCatalog(definitions, zoneSlots);
    }
}

internal sealed class CapturingNetworkSender : INetworkSender
{
    private readonly List<SentPacket> _packets = new();

    public void Send(int clientId, IPacket packet)
    {
        _packets.Add(new SentPacket(clientId, packet));
    }

    public string IssueResumeToken(ConnectionSession session, Guid accountId) => "TEST";

    public bool TryResumeSession(ConnectionSession session, string resumeToken, out Guid accountId, out MessageCode errorCode)
    {
        accountId = Guid.Empty;
        errorCode = MessageCode.None;
        return false;
    }

    public IReadOnlyList<TPacket> GetPackets<TPacket>(int clientId)
        where TPacket : class, IPacket
    {
        return _packets
            .Where(packet => packet.ClientId == clientId)
            .Select(packet => packet.Packet)
            .OfType<TPacket>()
            .ToArray();
    }

    public void Clear()
    {
        _packets.Clear();
    }

    private sealed record SentPacket(int ClientId, IPacket Packet);
}

internal sealed record VerificationReport(
    bool Success,
    DateTime ExecutedAtUtc,
    IReadOnlyList<ScenarioReport> Scenarios);

internal sealed record ScenarioReport(
    string Name,
    bool Passed,
    string Summary,
    IReadOnlyList<string> Assertions);


