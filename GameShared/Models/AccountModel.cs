namespace GameShared.Models;

// Network-safe projection (avoid DB-specific fields/types).
public struct AccountModel
{
    public Guid AccountId;
    public int StatusCode;
    public long? CreatedUnixMs;
    public long? LastLoginUnixMs;
}

