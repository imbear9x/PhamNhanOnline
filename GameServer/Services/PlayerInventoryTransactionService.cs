using System.Globalization;
using LinqToDB.Data;

namespace GameServer.Services;

public sealed class PlayerInventoryTransactionService
{
    private const long InventoryLockNamespace = 0x504E4F494E56544C; // PNOINVTL

    private readonly GameDb _db;

    public PlayerInventoryTransactionService(GameDb db)
    {
        _db = db;
    }

    public async Task<T> ExecuteAsync<T>(
        Guid playerId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (_db.Transaction is not null)
        {
            await AcquirePlayerInventoryLockAsync(playerId, cancellationToken);
            return await action(cancellationToken);
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await AcquirePlayerInventoryLockAsync(playerId, cancellationToken);
        var result = await action(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return result;
    }

    public async Task ExecuteAsync(
        Guid playerId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (_db.Transaction is not null)
        {
            await AcquirePlayerInventoryLockAsync(playerId, cancellationToken);
            await action(cancellationToken);
            return;
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await AcquirePlayerInventoryLockAsync(playerId, cancellationToken);
        await action(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private Task AcquirePlayerInventoryLockAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var lockKey = BuildPlayerInventoryLockKey(playerId).ToString(CultureInfo.InvariantCulture);
        return _db.ExecuteAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);
    }

    private static long BuildPlayerInventoryLockKey(Guid playerId)
    {
        Span<byte> bytes = stackalloc byte[16];
        playerId.TryWriteBytes(bytes);
        return BitConverter.ToInt64(bytes[..8]) ^ BitConverter.ToInt64(bytes[8..]) ^ InventoryLockNamespace;
    }
}
