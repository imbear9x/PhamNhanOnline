namespace GameServer.World;

public sealed record MapZoneSlotDefinition(
    int MapId,
    int ZoneIndex,
    int SpiritualEnergyTemplateId,
    string SpiritualEnergyCode,
    string SpiritualEnergyName,
    decimal SpiritualEnergyPerMinute);
