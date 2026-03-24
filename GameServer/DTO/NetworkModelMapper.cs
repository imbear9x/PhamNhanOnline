using GameShared.Models;
using GameServer.Runtime;
using GameServer.Time;
using GameServer.World;

namespace GameServer.DTO;

public static class NetworkModelMapper
{
    public static CharacterModel ToModel(this CharacterDto dto)
    {
        return new CharacterModel
        {
            CharacterId = dto.CharacterId,
            OwnerAccountId = dto.OwnerAccountId,
            WorldServerId = dto.WorldServerId,
            Name = dto.Name,
            Appearance = new CharacterAppearanceModel
            {
                ModelId = dto.Appearance.ModelId ?? 0,
                Gender = dto.Appearance.Gender ?? 0,
                HairColor = dto.Appearance.HairColor ?? 0,
                EyeColor = dto.Appearance.EyeColor ?? 0,
                FaceId = dto.Appearance.FaceId ?? 0
            },
            CreatedUnixMs = ToUnixMs(dto.CreatedUtc)
        };
    }

    public static CharacterBaseStatsModel ToModel(this CharacterBaseStatsDto dto)
    {
        return new CharacterBaseStatsModel
        {
            CharacterId = dto.CharacterId,
            RealmTemplateId = dto.RealmTemplateId ?? 0,
            Cultivation = dto.Cultivation ?? 0,
            BaseHp = dto.BaseHp ?? 0,
            BaseMp = dto.BaseMp ?? 0,
            BaseAttack = dto.BaseAttack ?? 0,
            BaseSpeed = dto.BaseSpeed ?? 0,
            BaseSpiritualSense = dto.BaseSpiritualSense ?? 0,
            BaseStamina = dto.BaseStamina ?? 0,
            LifespanBonus = dto.LifespanBonus ?? 0,
            BaseFortune = dto.BaseFortune ?? 0,
            BasePotential = dto.BasePotential ?? 0,
            UnallocatedPotential = dto.UnallocatedPotential ?? 0,
            FinalHp = dto.FinalHp ?? dto.BaseHp ?? 0,
            FinalMp = dto.FinalMp ?? dto.BaseMp ?? 0,
            FinalAttack = dto.FinalAttack ?? dto.BaseAttack ?? 0,
            FinalSpeed = dto.FinalSpeed ?? dto.BaseSpeed ?? 0,
            FinalSpiritualSense = dto.FinalSpiritualSense ?? dto.BaseSpiritualSense ?? 0,
            FinalStamina = dto.FinalStamina ?? dto.BaseStamina ?? 0,
            FinalFortune = dto.FinalFortune ?? dto.BaseFortune ?? 0,
            HpUpgradeCount = dto.HpUpgradeCount ?? 0,
            MpUpgradeCount = dto.MpUpgradeCount ?? 0,
            AttackUpgradeCount = dto.AttackUpgradeCount ?? 0,
            SpeedUpgradeCount = dto.SpeedUpgradeCount ?? 0,
            SpiritualSenseUpgradeCount = dto.SpiritualSenseUpgradeCount ?? 0,
            FortuneUpgradeCount = dto.FortuneUpgradeCount ?? 0,
            ActiveMartialArtId = dto.ActiveMartialArtId ?? 0,
            PotentialUpgradePreviews = dto.PotentialUpgradePreviews?.Select(ToModel).ToList()
        };
    }

    public static PotentialUpgradePreviewModel ToModel(this PotentialUpgradePreviewDto dto)
    {
        return new PotentialUpgradePreviewModel
        {
            TargetStat = (int)dto.Target,
            NextUpgradeCount = dto.NextUpgradeCount,
            TierIndex = dto.TierIndex,
            PotentialCost = dto.PotentialCost,
            StatGain = (double)dto.StatGain,
            IsAvailable = dto.IsAvailable,
            CanUpgrade = dto.CanUpgrade
        };
    }

    public static PlayerMartialArtModel ToModel(this PlayerMartialArtDto dto)
    {
        return new PlayerMartialArtModel
        {
            MartialArtId = dto.MartialArtId,
            Code = dto.Code,
            Name = dto.Name,
            Quality = dto.Quality,
            Category = dto.Category,
            CurrentStage = dto.CurrentStage,
            CurrentExp = dto.CurrentExp,
            MaxStage = dto.MaxStage,
            QiAbsorptionRate = (double)dto.QiAbsorptionRate,
            IsActive = dto.IsActive
        };
    }

    public static CharacterCurrentStateModel ToModel(this CharacterCurrentStateDto dto, GameTimeSnapshot gameTime)
    {
        return new CharacterCurrentStateModel
        {
            CharacterId = dto.CharacterId,
            CurrentHp = dto.CurrentHp,
            CurrentMp = dto.CurrentMp,
            CurrentStamina = dto.CurrentStamina,
            RemainingLifespan = CharacterLifespanRules.CalculateRemainingLifespanYears(dto.LifespanEndGameMinute, gameTime),
            CurrentMapId = dto.CurrentMapId,
            CurrentZoneIndex = dto.CurrentZoneIndex,
            CurrentPosX = dto.CurrentPosX,
            CurrentPosY = dto.CurrentPosY,
            IsDead = dto.IsDead,
            CurrentState = dto.CurrentState,
            CultivationStartedUnixMs = ToUnixMs(dto.CultivationStartedAtUtc),
            LastCultivationRewardedUnixMs = ToUnixMs(dto.LastCultivationRewardedAtUtc),
            LastSavedUnixMs = ToUnixMs(dto.LastSavedAt) ?? 0
        };
    }

    public static MapDefinitionModel ToModel(this MapDefinition definition)
    {
        return new MapDefinitionModel
        {
            MapId = definition.MapId,
            Name = definition.Name,
            MapType = (int)definition.Type,
            ClientMapKey = definition.ClientMapKey,
            AdjacentMapIds = definition.AdjacentMapIds.ToList(),
            Width = definition.Width,
            Height = definition.Height,
            CellSize = definition.CellSize,
            InterestRadius = definition.InterestRadius,
            DefaultSpawnX = definition.DefaultSpawnPosition.X,
            DefaultSpawnY = definition.DefaultSpawnPosition.Y,
            MaxPublicZoneCount = definition.MaxPublicZoneCount,
            MaxPlayersPerZone = definition.MaxPlayersPerZone,
            SupportsCavePlacement = definition.SupportsCavePlacement,
            IsPrivatePerPlayer = definition.IsPrivatePerPlayer
        };
    }

    public static MapZoneSummaryModel ToSummaryModel(this MapZoneSlotDefinition zoneSlot, int currentPlayerCount, int maxPlayerCount)
    {
        return new MapZoneSummaryModel
        {
            ZoneIndex = zoneSlot.ZoneIndex,
            CurrentPlayerCount = currentPlayerCount,
            MaxPlayerCount = maxPlayerCount,
            IsActive = currentPlayerCount > 0
        };
    }

    public static MapZoneDetailModel ToDetailModel(this MapZoneSlotDefinition zoneSlot)
    {
        return new MapZoneDetailModel
        {
            ZoneIndex = zoneSlot.ZoneIndex,
            SpiritualEnergyTemplateId = zoneSlot.SpiritualEnergyTemplateId,
            SpiritualEnergyCode = zoneSlot.SpiritualEnergyCode,
            SpiritualEnergyName = zoneSlot.SpiritualEnergyName,
            SpiritualEnergyPerMinute = zoneSlot.SpiritualEnergyPerMinute
        };
    }

    public static ObservedCharacterModel ToObservedCharacterModel(this PlayerSession player, GameTimeSnapshot gameTime)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        return new ObservedCharacterModel
        {
            Character = player.CharacterData.ToModel(),
            CurrentState = snapshot.CurrentState.ToModel(gameTime),
            MaxHp = snapshot.BaseStats.GetEffectiveHp(),
            MaxMp = snapshot.BaseStats.GetEffectiveMp(),
            MapId = player.MapId,
            ZoneIndex = player.ZoneIndex
        };
    }

    public static EnemyRuntimeModel ToModel(this MonsterEntity enemy)
    {
        return new EnemyRuntimeModel
        {
            RuntimeId = enemy.Id,
            EnemyTemplateId = enemy.Definition.Id,
            Code = enemy.Definition.Code,
            Name = enemy.Definition.Name,
            Kind = (int)enemy.Definition.Kind,
            RuntimeState = (int)enemy.State,
            CurrentHp = enemy.Hp,
            MaxHp = enemy.MaxHp,
            PosX = enemy.Position.X,
            PosY = enemy.Position.Y,
            SpawnGroupId = enemy.SpawnGroupId
        };
    }

    public static GroundRewardItemModel ToModel(this GroundRewardItem item)
    {
        return new GroundRewardItemModel
        {
            ItemTemplateId = item.ItemTemplateId,
            Quantity = item.Quantity,
            IsBound = item.IsBound
        };
    }

    public static GroundRewardModel ToModel(this GroundRewardEntity reward)
    {
        return new GroundRewardModel
        {
            RewardId = reward.Id,
            OwnerCharacterId = reward.OwnerCharacterId,
            PosX = reward.Position.X,
            PosY = reward.Position.Y,
            CreatedUnixMs = ToUnixMs(reward.CreatedAtUtc) ?? 0,
            FreeAtUnixMs = ToUnixMs(reward.FreeAtUtc),
            DestroyAtUnixMs = ToUnixMs(reward.DestroyAtUtc) ?? 0,
            Items = reward.Items.Select(ToModel).ToList()
        };
    }

    public static InventoryItemModel ToModel(this InventoryItemView view)
    {
        return new InventoryItemModel
        {
            PlayerItemId = view.PlayerItemId,
            ItemTemplateId = view.Definition.Id,
            Code = view.Definition.Code,
            Name = view.Definition.Name,
            ItemType = (int)view.Definition.ItemType,
            Rarity = (int)view.Definition.Rarity,
            Quantity = view.Quantity,
            IsBound = view.IsBound,
            MaxStack = view.Definition.MaxStack,
            IsTradeable = view.Definition.IsTradeable,
            IsDroppable = view.Definition.IsDroppable,
            IsDestroyable = view.Definition.IsDestroyable,
            Icon = view.Definition.Icon,
            BackgroundIcon = view.Definition.BackgroundIcon,
            Description = view.Definition.Description,
            EquipmentSlotType = view.Definition.Equipment is not null ? (int)view.Definition.Equipment.SlotType : null,
            EquipmentType = view.Definition.Equipment is not null ? (int)view.Definition.Equipment.EquipmentType : null,
            LevelRequirement = view.Definition.Equipment?.LevelRequirement,
            IsEquipped = view.IsEquipped,
            EquippedSlot = view.EquippedSlot.HasValue ? (int)view.EquippedSlot.Value : null,
            EnhanceLevel = view.EnhanceLevel,
            Durability = view.Durability
        };
    }

    private static long? ToUnixMs(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var value = dateTime.Value;
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }
}
