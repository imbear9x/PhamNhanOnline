using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Models;

namespace GameShared.Packets;

[Packet(200)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetGardenPlotsPacket : IPacket
{
    [ValidationCode(MessageCode.GardenCaveNotFound)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? CaveId { get; set; }
}

[Packet(210)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetGardenPlotsResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? CaveId { get; set; }
    public List<GardenPlotStateModel>? Plots { get; set; }
}

[Packet(201)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class InsertSoilPacket : IPacket
{
    [ValidationCode(MessageCode.InventoryItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? SoilPlayerItemId { get; set; }

    [ValidationCode(MessageCode.GardenCaveNotFound)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? CaveId { get; set; }

    [ValidationCode(MessageCode.GardenPlotNotFound)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PlotIndex { get; set; }
}

[Packet(211)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class InsertSoilResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public GardenPlotStateModel? Plot { get; set; }
}

[Packet(202)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class PlantHerbSeedPacket : IPacket
{
    [ValidationCode(MessageCode.InventoryItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? SeedPlayerItemId { get; set; }

    [ValidationCode(MessageCode.GardenCaveNotFound)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? CaveId { get; set; }

    [ValidationCode(MessageCode.GardenPlotNotFound)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PlotIndex { get; set; }
}

[Packet(212)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class PlantHerbSeedResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? PlayerHerbId { get; set; }
    public GardenPlotStateModel? Plot { get; set; }
}

[Packet(203)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class PlantExistingHerbPacket : IPacket
{
    [ValidationCode(MessageCode.GardenHerbNotOwned)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerHerbId { get; set; }

    [ValidationCode(MessageCode.GardenCaveNotFound)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? CaveId { get; set; }

    [ValidationCode(MessageCode.GardenPlotNotFound)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PlotIndex { get; set; }
}

[Packet(213)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class PlantExistingHerbResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public GardenPlotStateModel? Plot { get; set; }
}

[Packet(204)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class HarvestHerbPacket : IPacket
{
    [ValidationCode(MessageCode.GardenHerbNotOwned)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerHerbId { get; set; }
}

[Packet(214)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class HarvestHerbResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? PlayerHerbId { get; set; }
    public long? ExpireAtUnixMs { get; set; }
}

[Packet(205)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class ExtractHerbPacket : IPacket
{
    [ValidationCode(MessageCode.GardenHerbNotOwned)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerHerbId { get; set; }
}

[Packet(215)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class ExtractHerbResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
    public bool? MamNonReturned { get; set; }
}
