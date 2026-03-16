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
        }

        return PacketGeneratedRegistry.Create(id);
    }
}
