using System.Collections.Concurrent;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;

public class GameDb : DataConnection
{
    private static readonly ConcurrentDictionary<Type, AuditTimestampMetadata> AuditMetadataCache = new();

    public GameDb(string connection)
        : base(new DataOptions()
            .UsePostgreSQL(connection))
    {
    }

    public Task<int> InsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        ApplyAuditTimestampsForInsert(entity);
        return this.InsertAsync(entity, token: cancellationToken);
    }

    public Task<int> UpdateEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        ApplyAuditTimestampsForUpdate(entity);
        return this.UpdateAsync(entity, token: cancellationToken);
    }

    public Task<int> InsertEntityWithInt32IdentityAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        ApplyAuditTimestampsForInsert(entity);
        return this.InsertWithInt32IdentityAsync(entity, token: cancellationToken);
    }

    public Task<long> InsertEntityWithInt64IdentityAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        ApplyAuditTimestampsForInsert(entity);
        return this.InsertWithInt64IdentityAsync(entity, token: cancellationToken);
    }

    private static void ApplyAuditTimestampsForInsert<T>(T entity)
        where T : class
    {
        var metadata = GetAuditMetadata(typeof(T));
        if (!metadata.HasAnyAuditFields)
            return;

        var utcNow = DateTime.UtcNow;
        SetValueIfMissing(metadata.CreatedAtProperty, entity, utcNow);
        SetValueIfMissing(metadata.UpdatedAtProperty, entity, utcNow);
    }

    private static void ApplyAuditTimestampsForUpdate<T>(T entity)
        where T : class
    {
        var metadata = GetAuditMetadata(typeof(T));
        if (metadata.UpdatedAtProperty is null)
            return;

        SetValue(metadata.UpdatedAtProperty, entity, DateTime.UtcNow);
    }

    private static AuditTimestampMetadata GetAuditMetadata(Type entityType) =>
        AuditMetadataCache.GetOrAdd(entityType, static type =>
        {
            var createdAtProperty = type.GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public);
            var updatedAtProperty = type.GetProperty("UpdatedAt", BindingFlags.Instance | BindingFlags.Public);
            return new AuditTimestampMetadata(createdAtProperty, updatedAtProperty);
        });

    private static void SetValueIfMissing(PropertyInfo? property, object entity, DateTime utcNow)
    {
        if (property is null || !property.CanWrite)
            return;

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (propertyType != typeof(DateTime))
            return;

        var currentValue = property.GetValue(entity);
        if (currentValue is DateTime dateTimeValue)
        {
            if (dateTimeValue != default)
                return;
        }
        else if (currentValue is not null)
        {
            return;
        }

        SetValue(property, entity, utcNow);
    }

    private static void SetValue(PropertyInfo property, object entity, DateTime utcNow)
    {
        if (!property.CanWrite)
            return;

        if (property.PropertyType == typeof(DateTime))
        {
            property.SetValue(entity, utcNow);
            return;
        }

        if (property.PropertyType == typeof(DateTime?))
            property.SetValue(entity, utcNow);
    }

    private sealed record AuditTimestampMetadata(
        PropertyInfo? CreatedAtProperty,
        PropertyInfo? UpdatedAtProperty)
    {
        public bool HasAnyAuditFields => CreatedAtProperty is not null || UpdatedAtProperty is not null;
    }
}
