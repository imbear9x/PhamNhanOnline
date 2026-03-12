using GameShared.Attributes;

namespace GameShared.Models;

// Network-safe projection (avoid DB-specific fields/types).
[PacketModel]
public struct AccountModel
{
    public Guid AccountId;
    public int StatusCode;
    public long? CreatedUnixMs;
    public long? LastLoginUnixMs;
}
