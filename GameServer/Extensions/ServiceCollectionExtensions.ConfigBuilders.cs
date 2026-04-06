using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Config;
using GameServer.Repositories;
using GameServer.Randomness;
using GameServer.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Extensions;

public static partial class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions ConfigJsonOptions = BuildConfigJsonOptions();

    private static GameTimeConfig BuildGameTimeBootstrapConfig()
    {
        return LoadConfig<GameTimeConfig>("gameTimeConfig.json");
    }

    private static CharacterCreateConfig BuildCharacterCreateConfig()
    {
        return LoadConfig<CharacterCreateConfig>("CharacterCreateConfig.json");
    }

    private static GameRandomConfig BuildGameRandomConfigFromDatabase(IServiceProvider rootProvider)
    {
        using var scope = rootProvider.CreateScope();
        var tableRepository = scope.ServiceProvider.GetRequiredService<GameRandomTableRepository>();
        var entryRepository = scope.ServiceProvider.GetRequiredService<GameRandomEntryRepository>();
        var entryTagRepository = scope.ServiceProvider.GetRequiredService<GameRandomEntryTagRepository>();
        var fortuneTagRepository = scope.ServiceProvider.GetRequiredService<GameRandomFortuneTagRepository>();

        var tables = tableRepository.GetAllAsync().GetAwaiter().GetResult();
        var entries = entryRepository.GetAllAsync().GetAwaiter().GetResult();
        var entryTags = entryTagRepository.GetAllAsync().GetAwaiter().GetResult();
        var fortuneTags = fortuneTagRepository.GetAllAsync().GetAwaiter().GetResult();

        var entryTagsByEntryId = entryTags
            .GroupBy(x => x.GameRandomEntryId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(x => x.Tag)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToArray());

        var fortuneTagsByTableId = fortuneTags
            .GroupBy(x => x.GameRandomTableId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(x => x.Tag)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList());

        var entriesByTableId = entries
            .GroupBy(x => x.GameRandomTableId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id).ToArray());

        return new GameRandomConfig
        {
            Tables = tables
                .OrderBy(x => x.Id)
                .Select(table => new GameRandomTableConfig
                {
                    TableId = table.TableId,
                    Mode = (GameRandomTableMode)table.Mode,
                    FortuneModifier = new GameRandomFortuneModifierConfig
                    {
                        Enabled = table.FortuneEnabled,
                        BonusPartsPerMillionPerFortunePoint = table.FortuneBonusPartsPerMillionPerFortunePoint,
                        MaxBonusPartsPerMillion = table.FortuneMaxBonusPartsPerMillion,
                        NoneEntryId = table.NoneEntryId,
                        ApplyToEntryTags = fortuneTagsByTableId.GetValueOrDefault(table.Id, [])
                    },
                    Entries = entriesByTableId.GetValueOrDefault(table.Id, [])
                        .Select(entry => new GameRandomEntryConfig
                        {
                            EntryId = entry.EntryId,
                            ChancePartsPerMillion = entry.ChancePartsPerMillion,
                            IsNone = entry.IsNone,
                            Tags = entryTagsByEntryId.GetValueOrDefault(entry.Id, []).ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static GameConfigValues BuildGameConfigValuesFromDatabase(IServiceProvider rootProvider)
    {
        using var scope = rootProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<GameConfigRepository>();
        var configs = repository.GetAllAsync().GetAwaiter().GetResult();
        var configsByKey = configs
            .GroupBy(x => x.ConfigKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    if (g.Count() != 1)
                        throw new InvalidOperationException($"Duplicate game config key detected: {g.Key}");

                    return g.Single().ConfigValue;
                },
                StringComparer.OrdinalIgnoreCase);

        return new GameConfigValues
        {
            NetworkReconnectResumeWindowSeconds = GetInt(configsByKey, GameConfigKeys.NetworkReconnectResumeWindowSeconds, 3),
            WorldPortalValidationBufferServerUnits = GetFloat(configsByKey, GameConfigKeys.WorldPortalValidationBufferServerUnits, 4f),
            CombatSkillRangeGraceBufferUnits = GetFloat(configsByKey, GameConfigKeys.CombatSkillRangeGraceBufferUnits, 12f),
            CombatDeathReturnHomeRecoveryRatio = GetDouble(configsByKey, GameConfigKeys.CombatDeathReturnHomeRecoveryRatio, 0.80d),
            ItemDropPlayerOwnershipSeconds = GetInt(configsByKey, GameConfigKeys.ItemDropPlayerOwnershipSeconds, 10),
            ItemDropPlayerFreeForAllSeconds = GetInt(configsByKey, GameConfigKeys.ItemDropPlayerFreeForAllSeconds, 50),
            ItemDropEnemyDefaultOwnershipSeconds = GetInt(configsByKey, GameConfigKeys.ItemDropEnemyDefaultOwnershipSeconds, 30),
            ItemDropEnemyDefaultFreeForAllSeconds = GetInt(configsByKey, GameConfigKeys.ItemDropEnemyDefaultFreeForAllSeconds, 30),
            ItemDropGroundSpawnOffsetServerUnits = GetFloat(configsByKey, GameConfigKeys.ItemDropGroundSpawnOffsetServerUnits, 30f),
            WorldEmptyPublicInstanceLifetimeSeconds = GetInt(configsByKey, GameConfigKeys.WorldEmptyPublicInstanceLifetimeSeconds, 120),
            CultivationPotentialPerCultivationPoint = GetInt(configsByKey, GameConfigKeys.CultivationPotentialPerCultivationPoint, 1),
            CultivationSettlementIntervalSeconds = GetInt(configsByKey, GameConfigKeys.CultivationSettlementIntervalSeconds, 300),
            CharacterHomeGardenPlotCount = GetInt(configsByKey, GameConfigKeys.CharacterHomeGardenPlotCount, 8),
            CharacterStarterBasicSkillId = GetInt(configsByKey, GameConfigKeys.CharacterStarterBasicSkillId, 0),
            CharacterStarterBasicSkillSlotIndex = GetInt(configsByKey, GameConfigKeys.CharacterStarterBasicSkillSlotIndex, 1),
            SkillMaxLoadoutSlotCount = GetInt(configsByKey, GameConfigKeys.SkillMaxLoadoutSlotCount, 5)
        };
    }

    private static T LoadConfig<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", fileName);
        if (!File.Exists(path))
            throw new Exception($"Config file not found: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, ConfigJsonOptions)
               ?? throw new Exception($"Failed to deserialize config: {path}");
    }

    private static int GetInt(IReadOnlyDictionary<string, string> configs, string key, int fallback)
    {
        if (!configs.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            return fallback;

        if (int.TryParse(rawValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        throw new InvalidOperationException($"Game config '{key}' is not a valid int: '{rawValue}'.");
    }

    private static float GetFloat(IReadOnlyDictionary<string, string> configs, string key, float fallback)
    {
        if (!configs.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            return fallback;

        if (float.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        throw new InvalidOperationException($"Game config '{key}' is not a valid float: '{rawValue}'.");
    }

    private static double GetDouble(IReadOnlyDictionary<string, string> configs, string key, double fallback)
    {
        if (!configs.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            return fallback;

        if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        throw new InvalidOperationException($"Game config '{key}' is not a valid double: '{rawValue}'.");
    }

    private static JsonSerializerOptions BuildConfigJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

