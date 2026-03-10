using System.Numerics;

namespace GameServer.World;

public sealed class MonsterEntity
{
    private readonly object _sync = new();
    private readonly int _maxHp;

    public int Id { get; }
    public int MonsterTemplateId { get; }
    public Vector2 Position { get; private set; }
    public int Hp { get; private set; }
    public bool IsAlive { get; private set; }

    public MonsterEntity(int id, int monsterTemplateId, Vector2 position, int maxHp)
    {
        if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));

        Id = id;
        MonsterTemplateId = monsterTemplateId;
        Position = position;
        _maxHp = maxHp;

        Hp = maxHp;
        IsAlive = true;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        lock (_sync)
        {
            if (!IsAlive) return;

            Hp -= damage;
            if (Hp <= 0)
            {
                Hp = 0;
                IsAlive = false;
            }
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            Hp = _maxHp;
            IsAlive = true;
        }
    }
}

