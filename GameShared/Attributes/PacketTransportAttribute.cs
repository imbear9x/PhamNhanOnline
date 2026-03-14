namespace GameShared.Attributes;

public enum PacketTransportMode
{
    ReliableOrdered,
    ReliableSequenced,
    ReliableUnordered,
    Sequenced,
    Unreliable,
    UnreliableSequenced
}

public enum PacketTrafficClass
{
    Business,
    StateSync,
    RealtimeState
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketTransportAttribute : Attribute
{
    public PacketTransportAttribute(
        PacketTransportMode mode,
        PacketTrafficClass trafficClass = PacketTrafficClass.Business)
    {
        Mode = mode;
        TrafficClass = trafficClass;
    }

    public PacketTransportMode Mode { get; }
    public PacketTrafficClass TrafficClass { get; }
    public int MinIntervalMs { get; set; }
}
