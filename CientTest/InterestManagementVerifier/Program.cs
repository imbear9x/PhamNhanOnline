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
        var scenarios = new List<ScenarioReport>();
        scenarios.Add(RunScenario("JoinMap publishes map snapshot and no observer packets when alone", VerifySoloJoin));
        scenarios.Add(RunScenario("Nearby players spawn for each other on world entry", VerifyNearbySpawnOnJoin));
        scenarios.Add(RunScenario("Far players do not spawn until they enter interest radius", VerifyFarPlayersStayHiddenUntilNear));
        scenarios.Add(RunScenario("Move and state-change packets only reach visible observers", VerifyMoveAndStateReplication));

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

    private static void VerifySoloJoin(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, 100, 100);
        context.EnterWorld(alice);

        context.ExpectPacket<MapJoinedPacket>(alice.ConnectionId, 1, "Alice should receive one MapJoined packet.");
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(alice.ConnectionId, 0, "Alice should not observe anyone when alone.");
    }

    private static void VerifyNearbySpawnOnJoin(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, 100, 100);
        var bob = context.CreatePlayer("Bob", 2, 120, 120);

        context.EnterWorld(alice);
        context.ClearNetworkLog();

        context.EnterWorld(bob);

        context.ExpectPacket<MapJoinedPacket>(bob.ConnectionId, 1, "Bob should receive map snapshot on enter.");
        context.ExpectSpawn(bob.ConnectionId, alice.PlayerId, "Bob should observe Alice when entering nearby.");
        context.ExpectSpawn(alice.ConnectionId, bob.PlayerId, "Alice should observe Bob when he enters nearby.");
    }

    private static void VerifyFarPlayersStayHiddenUntilNear(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, 100, 100);
        var charlie = context.CreatePlayer("Charlie", 3, 700, 700);

        context.EnterWorld(alice);
        context.ClearNetworkLog();

        context.EnterWorld(charlie);
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(alice.ConnectionId, 0, "Alice should not see Charlie when out of range.");
        context.ExpectPacket<ObservedCharacterSpawnedPacket>(charlie.ConnectionId, 0, "Charlie should not see Alice when out of range.");

        context.ClearNetworkLog();
        context.MovePlayer(charlie, 110, 110);

        context.ExpectSpawn(alice.ConnectionId, charlie.PlayerId, "Alice should see Charlie after he moves into range.");
        context.ExpectSpawn(charlie.ConnectionId, alice.PlayerId, "Charlie should see Alice after moving into range.");
    }

    private static void VerifyMoveAndStateReplication(ScenarioContext context)
    {
        var alice = context.CreatePlayer("Alice", 1, 100, 100);
        var bob = context.CreatePlayer("Bob", 2, 120, 120);
        var charlie = context.CreatePlayer("Charlie", 3, 700, 700);

        context.EnterWorld(alice);
        context.EnterWorld(bob);
        context.EnterWorld(charlie);
        context.ClearNetworkLog();

        context.MovePlayer(bob, 130, 140);
        context.ExpectMove(alice.ConnectionId, bob.PlayerId, "Alice should receive Bob's move while visible.");
        context.ExpectPacket<ObservedCharacterMovedPacket>(charlie.ConnectionId, 0, "Charlie should not receive Bob's move while far away.");

        context.ClearNetworkLog();
        context.ChangeState(bob, hp: 77, isDead: false, stateCode: 9);
        context.ExpectState(alice.ConnectionId, bob.PlayerId, "Alice should receive Bob's state change while visible.");
        context.ExpectPacket<ObservedCharacterCurrentStateChangedPacket>(charlie.ConnectionId, 0, "Charlie should not receive Bob's state change while far away.");

        context.ClearNetworkLog();
        context.MovePlayer(bob, 700, 700);
        context.ExpectDespawn(alice.ConnectionId, bob.PlayerId, "Alice should receive despawn when Bob leaves interest radius.");

        context.ClearNetworkLog();
        context.ChangeState(bob, hp: 55, isDead: false, stateCode: 10);
        context.ExpectPacket<ObservedCharacterCurrentStateChangedPacket>(alice.ConnectionId, 0, "Alice should not receive Bob's state after despawn.");
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
        var mapCatalog = new MapCatalog();
        var mapManager = new MapManager(mapCatalog);
        _worldManager = new WorldManager(mapManager);
        _interestService = new WorldInterestService(_worldManager, _network, _gameTimeService);
    }

    public List<string> Assertions { get; } = new();

    public PlayerSession CreatePlayer(string name, int ordinal, float x, float y)
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
            BasePotential: 0);
        var currentState = new CharacterCurrentStateDto(
            playerId,
            CurrentHp: 100,
            CurrentMp: 80,
            CurrentStamina: 100,
            LifespanEndGameMinute: 999999,
            CurrentMapId: 1,
            CurrentPosX: x,
            CurrentPosY: y,
            IsDead: false,
            CurrentState: 1,
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
            CurrentMapId = player.MapId == 0 ? 1 : player.MapId
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

    public void ExpectSpawn(int connectionId, Guid observedCharacterId, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterSpawnedPacket>(connectionId)
            .Any(packet => packet.Character.HasValue && packet.Character.Value.Character.CharacterId == observedCharacterId);
        Assert(match, message);
    }

    public void ExpectDespawn(int connectionId, Guid observedCharacterId, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterDespawnedPacket>(connectionId)
            .Any(packet => packet.CharacterId == observedCharacterId);
        Assert(match, message);
    }

    public void ExpectMove(int connectionId, Guid observedCharacterId, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterMovedPacket>(connectionId)
            .Any(packet => packet.CharacterId == observedCharacterId);
        Assert(match, message);
    }

    public void ExpectState(int connectionId, Guid observedCharacterId, string message)
    {
        var match = _network
            .GetPackets<ObservedCharacterCurrentStateChangedPacket>(connectionId)
            .Any(packet => packet.CurrentState.HasValue && packet.CurrentState.Value.CharacterId == observedCharacterId);
        Assert(match, message);
    }

    private void Assert(bool condition, string message)
    {
        Assertions.Add($"{(condition ? "PASS" : "FAIL")}: {message}");
        if (!condition)
            throw new InvalidOperationException(message);
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
