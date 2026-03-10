using System.Numerics;

namespace GameServer.World;

public sealed class MapInstance
{
    public const int MaxPlayers = 10;

    private readonly object _sync = new();
    private int _nextMonsterId = 1;

    public int InstanceId { get; }
    public int MapId { get; }

    public List<PlayerSession> Players { get; } = new();
    public List<MonsterEntity> Monsters { get; } = new();

    public MapInstance(int instanceId, int mapId)
    {
        InstanceId = instanceId;
        MapId = mapId;
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
            if (Players.Count >= MaxPlayers)
                return false;

            if (!Players.Contains(player))
            {
                Players.Add(player);
                player.MapId = MapId;
                player.InstanceId = InstanceId;
            }

            return true;
        }
    }

    public void RemovePlayer(PlayerSession player)
    {
        lock (_sync)
        {
            Players.Remove(player);
            if (player.MapId == MapId && player.InstanceId == InstanceId)
            {
                player.InstanceId = 0;
            }
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
}

