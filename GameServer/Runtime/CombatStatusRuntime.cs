namespace GameServer.Runtime;

public readonly record struct CombatStatModifierAggregate(
    decimal Flat,
    decimal Ratio,
    decimal Percent);

public readonly record struct CombatStatSnapshot(
    int MaxHp,
    int MaxMp,
    int MaxStamina,
    int Attack,
    int Speed,
    int SpiritualSense,
    double Fortune);

public sealed class CombatStatusCollection
{
    private readonly object _sync = new();
    private readonly List<CombatShieldState> _shields = [];
    private readonly List<CombatStatModifierState> _statModifiers = [];
    private DateTime? _stunnedUntilUtc;

    public bool IsStunned(DateTime utcNow)
    {
        lock (_sync)
        {
            CleanupExpiredNoLock(utcNow);
            return _stunnedUntilUtc.HasValue && utcNow < _stunnedUntilUtc.Value;
        }
    }

    public int AbsorbIncomingDamage(int incomingDamage, DateTime utcNow, out int absorbedDamage)
    {
        absorbedDamage = 0;
        if (incomingDamage <= 0)
            return 0;

        lock (_sync)
        {
            CleanupExpiredNoLock(utcNow);

            var remaining = incomingDamage;
            for (var index = 0; index < _shields.Count && remaining > 0; index++)
            {
                var shield = _shields[index];
                if (shield.RemainingValue <= 0)
                    continue;

                var absorbed = Math.Min(remaining, shield.RemainingValue);
                remaining -= absorbed;
                absorbedDamage += absorbed;
                _shields[index] = shield with { RemainingValue = shield.RemainingValue - absorbed };
            }

            _shields.RemoveAll(static shield => shield.RemainingValue <= 0);
            return remaining;
        }
    }

    public void AddShield(int amount, DateTime? expiresAtUtc)
    {
        if (amount <= 0)
            return;

        lock (_sync)
        {
            _shields.Add(new CombatShieldState(amount, expiresAtUtc));
        }
    }

    public void AddStun(DateTime stunnedUntilUtc)
    {
        lock (_sync)
        {
            if (!_stunnedUntilUtc.HasValue || stunnedUntilUtc > _stunnedUntilUtc.Value)
                _stunnedUntilUtc = stunnedUntilUtc;
        }
    }

    public void AddStatModifier(
        CharacterStatType statType,
        decimal value,
        CombatValueType valueType,
        DateTime? expiresAtUtc)
    {
        if (statType == CharacterStatType.None || value == 0)
            return;

        lock (_sync)
        {
            _statModifiers.Add(new CombatStatModifierState(statType, value, valueType, expiresAtUtc));
        }
    }

    public CombatStatModifierAggregate GetStatModifierAggregate(CharacterStatType statType, DateTime utcNow)
    {
        lock (_sync)
        {
            CleanupExpiredNoLock(utcNow);

            decimal flat = 0;
            decimal ratio = 0;
            decimal percent = 0;
            foreach (var modifier in _statModifiers)
            {
                if (modifier.StatType != statType)
                    continue;

                switch (modifier.ValueType)
                {
                    case CombatValueType.Flat:
                        flat += modifier.Value;
                        break;
                    case CombatValueType.Ratio:
                        ratio += modifier.Value;
                        break;
                    case CombatValueType.Percent:
                        percent += modifier.Value;
                        break;
                }
            }

            return new CombatStatModifierAggregate(flat, ratio, percent);
        }
    }

    public void CleanupExpired(DateTime utcNow)
    {
        lock (_sync)
        {
            CleanupExpiredNoLock(utcNow);
        }
    }

    private void CleanupExpiredNoLock(DateTime utcNow)
    {
        _shields.RemoveAll(shield => shield.ExpiresAtUtc.HasValue && utcNow >= shield.ExpiresAtUtc.Value);
        _statModifiers.RemoveAll(modifier => modifier.ExpiresAtUtc.HasValue && utcNow >= modifier.ExpiresAtUtc.Value);

        if (_stunnedUntilUtc.HasValue && utcNow >= _stunnedUntilUtc.Value)
            _stunnedUntilUtc = null;
    }

    private readonly record struct CombatShieldState(
        int RemainingValue,
        DateTime? ExpiresAtUtc);

    private readonly record struct CombatStatModifierState(
        CharacterStatType StatType,
        decimal Value,
        CombatValueType ValueType,
        DateTime? ExpiresAtUtc);
}

public static class CombatStatMath
{
    public static int ApplyModifiers(int baseValue, CombatStatModifierAggregate aggregate)
    {
        decimal result = baseValue;
        result += aggregate.Flat;
        result += baseValue * aggregate.Ratio;
        result += baseValue * (aggregate.Percent / 100m);
        return Math.Max(0, decimal.ToInt32(decimal.Truncate(result)));
    }

    public static double ApplyModifiers(double baseValue, CombatStatModifierAggregate aggregate)
    {
        double result = baseValue;
        result += (double)aggregate.Flat;
        result += baseValue * (double)aggregate.Ratio;
        result += baseValue * ((double)aggregate.Percent / 100d);
        return Math.Max(0d, result);
    }
}
