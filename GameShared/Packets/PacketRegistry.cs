namespace GameShared.Packets;

public static class PacketRegistry
{
    public static bool TryGetId(IPacket packet, out int id)
    {
        switch (packet)
        {
            case StartCultivationPacket:
                id = 28;
                return true;
            case StartCultivationResultPacket:
                id = 29;
                return true;
            case StopCultivationPacket:
                id = 30;
                return true;
            case StopCultivationResultPacket:
                id = 31;
                return true;
            case BreakthroughPacket:
                id = 32;
                return true;
            case BreakthroughResultPacket:
                id = 33;
                return true;
            case AllocatePotentialPacket:
                id = 34;
                return true;
            case AllocatePotentialResultPacket:
                id = 35;
                return true;
            case CultivationRewardsGrantedPacket:
                id = 36;
                return true;
            case GetMapZonesPacket:
                id = 37;
                return true;
            case GetMapZonesResultPacket:
                id = 38;
                return true;
            case SwitchMapZonePacket:
                id = 39;
                return true;
            case SwitchMapZoneResultPacket:
                id = 40;
                return true;
            case GetOwnedMartialArtsPacket:
                id = 41;
                return true;
            case GetOwnedMartialArtsResultPacket:
                id = 42;
                return true;
            case UseMartialArtBookPacket:
                id = 43;
                return true;
            case UseMartialArtBookResultPacket:
                id = 44;
                return true;
            case SetActiveMartialArtPacket:
                id = 45;
                return true;
            case SetActiveMartialArtResultPacket:
                id = 46;
                return true;
            case WorldRuntimeSnapshotPacket:
                id = 47;
                return true;
            case EnemySpawnedPacket:
                id = 48;
                return true;
            case EnemyDespawnedPacket:
                id = 49;
                return true;
            case EnemyHpChangedPacket:
                id = 50;
                return true;
            case GroundRewardSpawnedPacket:
                id = 51;
                return true;
            case GroundRewardDespawnedPacket:
                id = 52;
                return true;
            case AttackEnemyPacket:
                id = 53;
                return true;
            case AttackEnemyResultPacket:
                id = 54;
                return true;
            case PickupGroundRewardPacket:
                id = 55;
                return true;
            case PickupGroundRewardResultPacket:
                id = 56;
                return true;
            case MapInstanceClosedPacket:
                id = 57;
                return true;
            case GetInventoryPacket:
                id = 58;
                return true;
            case GetInventoryResultPacket:
                id = 59;
                return true;
            case EquipInventoryItemPacket:
                id = 60;
                return true;
            case EquipInventoryItemResultPacket:
                id = 61;
                return true;
            case UnequipInventoryItemPacket:
                id = 62;
                return true;
            case UnequipInventoryItemResultPacket:
                id = 63;
                return true;
        }

        return PacketGeneratedRegistry.TryGetId(packet, out id);
    }

    public static IPacket? Create(int id)
    {
        switch (id)
        {
            case 28:
                return new StartCultivationPacket();
            case 29:
                return new StartCultivationResultPacket();
            case 30:
                return new StopCultivationPacket();
            case 31:
                return new StopCultivationResultPacket();
            case 32:
                return new BreakthroughPacket();
            case 33:
                return new BreakthroughResultPacket();
            case 34:
                return new AllocatePotentialPacket();
            case 35:
                return new AllocatePotentialResultPacket();
            case 36:
                return new CultivationRewardsGrantedPacket();
            case 37:
                return new GetMapZonesPacket();
            case 38:
                return new GetMapZonesResultPacket();
            case 39:
                return new SwitchMapZonePacket();
            case 40:
                return new SwitchMapZoneResultPacket();
            case 41:
                return new GetOwnedMartialArtsPacket();
            case 42:
                return new GetOwnedMartialArtsResultPacket();
            case 43:
                return new UseMartialArtBookPacket();
            case 44:
                return new UseMartialArtBookResultPacket();
            case 45:
                return new SetActiveMartialArtPacket();
            case 46:
                return new SetActiveMartialArtResultPacket();
            case 47:
                return new WorldRuntimeSnapshotPacket();
            case 48:
                return new EnemySpawnedPacket();
            case 49:
                return new EnemyDespawnedPacket();
            case 50:
                return new EnemyHpChangedPacket();
            case 51:
                return new GroundRewardSpawnedPacket();
            case 52:
                return new GroundRewardDespawnedPacket();
            case 53:
                return new AttackEnemyPacket();
            case 54:
                return new AttackEnemyResultPacket();
            case 55:
                return new PickupGroundRewardPacket();
            case 56:
                return new PickupGroundRewardResultPacket();
            case 57:
                return new MapInstanceClosedPacket();
            case 58:
                return new GetInventoryPacket();
            case 59:
                return new GetInventoryResultPacket();
            case 60:
                return new EquipInventoryItemPacket();
            case 61:
                return new EquipInventoryItemResultPacket();
            case 62:
                return new UnequipInventoryItemPacket();
            case 63:
                return new UnequipInventoryItemResultPacket();
        }

        return PacketGeneratedRegistry.Create(id);
    }
}
