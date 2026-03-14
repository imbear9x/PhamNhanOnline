using System.Numerics;

namespace GameServer.World;

public sealed class MapInstance
{
    private readonly object _sync = new();
    private readonly Dictionary<(int X, int Y), HashSet<Guid>> _playersByCell = new();
    private readonly Dictionary<Guid, (int X, int Y)> _playerCells = new();
    private readonly Dictionary<Guid, PlayerSession> _playersById = new();
    private int _nextMonsterId = 1;

    public int InstanceId { get; }
    public MapDefinition Definition { get; }
    public int MapId => Definition.MapId;

    public List<PlayerSession> Players { get; } = new();
    public List<MonsterEntity> Monsters { get; } = new();

    public MapInstance(int instanceId, MapDefinition definition)
    {
        InstanceId = instanceId;
        Definition = definition;
    }

    public int PlayerCount
    {
        get
        {
            lock (_sync)
            {
                return Players.Count;
            }
        }
    }

    public bool AddPlayer(PlayerSession player)
    {
        lock (_sync)
        {
            if (Players.Count >= Definition.MaxPlayersPerInstance && !_playersById.ContainsKey(player.PlayerId))
                return false;

            if (!_playersById.ContainsKey(player.PlayerId))
            {
                Players.Add(player);
                _playersById[player.PlayerId] = player;
                player.MapId = MapId;
                player.InstanceId = InstanceId;
            }

            UpdatePlayerCellUnsafe(player);
            return true;
        }
    }

    public void RemovePlayer(PlayerSession player)
    {
        lock (_sync)
        {
            Players.Remove(player);
            _playersById.Remove(player.PlayerId);
            RemovePlayerCellUnsafe(player.PlayerId);
            if (player.MapId == MapId && player.InstanceId == InstanceId)
            {
                player.InstanceId = 0;
            }
        }
    }

    public void UpdatePlayerPosition(PlayerSession player)
    {
        lock (_sync)
        {
            if (!_playersById.ContainsKey(player.PlayerId))
                return;

            UpdatePlayerCellUnsafe(player);
        }
    }

    public IReadOnlyCollection<PlayerSession> GetNearbyPlayers(Vector2 position, float radius, Guid? excludePlayerId = null)
    {
        lock (_sync)
        {
            var radiusSquared = radius * radius;
            var baseCell = GetCell(position);
            var cellRadius = Math.Max(1, (int)MathF.Ceiling(radius / Definition.CellSize));
            var result = new List<PlayerSession>();
            var seen = new HashSet<Guid>();

            for (var dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (var dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cell = (baseCell.X + dx, baseCell.Y + dy);
                    if (!_playersByCell.TryGetValue(cell, out var playerIds))
                        continue;

                    foreach (var playerId in playerIds)
                    {
                        if (excludePlayerId.HasValue && playerId == excludePlayerId.Value)
                            continue;

                        if (!seen.Add(playerId))
                            continue;

                        if (!_playersById.TryGetValue(playerId, out var player))
                            continue;

                        if (Vector2.DistanceSquared(player.Position, position) > radiusSquared)
                            continue;

                        result.Add(player);
                    }
                }
            }

            return result;
        }
    }

    public MonsterEntity SpawnMonster(int monsterTemplateId, Vector2 position)
    {
        lock (_sync)
        {
            var monster = new MonsterEntity(
                id: _nextMonsterId++,
                monsterTemplateId: monsterTemplateId,
                position: position,
                maxHp: 100);

            Monsters.Add(monster);
            return monster;
        }
    }

    public void Update()
    {
        // Keep minimal for now. Systems (AI/combat/spawn) can be plugged in later.
        lock (_sync)
        {
            // Example: you might later clean up or respawn monsters here.
        }
    }

    private (int X, int Y) GetCell(Vector2 position)
    {
        var clamped = Definition.ClampPosition(position);
        var x = (int)MathF.Floor(clamped.X / Definition.CellSize);
        var y = (int)MathF.Floor(clamped.Y / Definition.CellSize);
        return (x, y);
    }

    private void UpdatePlayerCellUnsafe(PlayerSession player)
    {
        var newCell = GetCell(player.Position);
        if (_playerCells.TryGetValue(player.PlayerId, out var existingCell) && existingCell == newCell)
            return;

        RemovePlayerCellUnsafe(player.PlayerId);

        if (!_playersByCell.TryGetValue(newCell, out var players))
        {
            players = new HashSet<Guid>();
            _playersByCell[newCell] = players;
        }

        players.Add(player.PlayerId);
        _playerCells[player.PlayerId] = newCell;
    }

    private void RemovePlayerCellUnsafe(Guid playerId)
    {
        if (!_playerCells.TryGetValue(playerId, out var cell))
            return;

        if (_playersByCell.TryGetValue(cell, out var players))
        {
            players.Remove(playerId);
            if (players.Count == 0)
                _playersByCell.Remove(cell);
        }

        _playerCells.Remove(playerId);
    }
}

