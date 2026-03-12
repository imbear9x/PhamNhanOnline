namespace GameShared.Diagnostics;

public sealed class PacketIncidentRecord
{
    public int SchemaVersion { get; set; } = 1;
    public string IncidentType { get; set; } = "ServerInboundPacketException";
    public DateTime CapturedAtUtc { get; set; }
    public string Source { get; set; } = "Server";
    public int? ConnectionId { get; set; }
    public string? RemoteEndPoint { get; set; }
    public bool? IsAuthenticated { get; set; }
    public Guid? PlayerId { get; set; }
    public byte? ChannelNumber { get; set; }
    public string? DeliveryMethod { get; set; }
    public string PacketType { get; set; } = string.Empty;
    public string PacketJson { get; set; } = string.Empty;
    public string PacketPayloadBase64 { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? ExceptionStackTrace { get; set; }
}
