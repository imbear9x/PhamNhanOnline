using System.Text.Json;
using Npgsql;

namespace SkillWorldCatalogSyncTool;

internal static class Program
{
    private static int Main()
    {
        try
        {
            var payload = BuildPayload();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            Console.Out.Write(JsonSerializer.Serialize(payload, options));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static SkillWorldCatalogSyncPayload BuildPayload()
    {
        if (!DatabaseConfigResolver.TryResolve(out var connectionString, out _, out var error))
            throw new InvalidOperationException(error);

        var skills = new List<SkillWorldCatalogSkillRecord>();
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        const string sql = """
            select
                id,
                code,
                skill_group_code,
                skill_level,
                coalesce(name, '') as name
            from public.skills
            where btrim(coalesce(skill_group_code, '')) <> ''
              and btrim(coalesce(code, '')) <> ''
            order by skill_group_code, skill_level, id;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            skills.Add(new SkillWorldCatalogSkillRecord
            {
                SkillId = reader.GetInt32(0),
                SkillCode = reader.GetString(1),
                SkillGroupCode = reader.GetString(2),
                SkillLevel = reader.GetInt32(3),
                Name = reader.GetString(4)
            });
        }

        return new SkillWorldCatalogSyncPayload
        {
            Skills = skills
        };
    }

    private sealed class SkillWorldCatalogSyncPayload
    {
        public List<SkillWorldCatalogSkillRecord> Skills { get; init; } = new();
    }

    private sealed class SkillWorldCatalogSkillRecord
    {
        public int SkillId { get; init; }

        public string SkillCode { get; init; } = string.Empty;

        public string SkillGroupCode { get; init; } = string.Empty;

        public int SkillLevel { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
