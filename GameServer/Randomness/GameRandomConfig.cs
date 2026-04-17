using System.Text.Json.Serialization;

namespace GameServer.Randomness;

public sealed class GameRandomConfig
{
    public const int ChanceScale = 1_000_000;

    public List<GameRandomTableConfig> Tables { get; init; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameRandomTableMode
{
    Exclusive = 0
}

public sealed class GameRandomTableConfig
{
    public string TableId { get; init; } = string.Empty;
    public GameRandomTableMode Mode { get; init; } = GameRandomTableMode.Exclusive;
    public GameRandomLuckModifierConfig LuckModifier { get; init; } = new();
    public List<GameRandomEntryConfig> Entries { get; init; } = [];
}

public sealed class GameRandomEntryConfig
{
    public string EntryId { get; init; } = string.Empty;
    public int ChancePartsPerMillion { get; init; }
    public bool IsNone { get; init; }
    public List<string> Tags { get; init; } = [];
}

public sealed class GameRandomLuckModifierConfig
{
    public static GameRandomLuckModifierConfig Disabled { get; } = new()
    {
        Enabled = false
    };

    public bool Enabled { get; init; } = true;
    public int BonusPartsPerMillionPerLuckPoint { get; init; }
    public int MaxBonusPartsPerMillion { get; init; }
    public string NoneEntryId { get; init; } = "__none__";
    public List<string> ApplyToEntryTags { get; init; } = [];
}
