using System.Text.Json;

namespace AdminDesignerTool;

internal static class DatabaseConfigResolver
{
    public static bool TryResolve(out string connectionString, out string configPath, out string error)
    {
        connectionString = string.Empty;
        configPath = string.Empty;
        error = string.Empty;

        foreach (var candidate in EnumerateCandidates())
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                using var stream = File.OpenRead(candidate);
                using var document = JsonDocument.Parse(stream);
                if (!document.RootElement.TryGetProperty("ConnectionString", out var property))
                {
                    error = $"Khong tim thay ConnectionString trong {candidate}.";
                    continue;
                }

                var value = property.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = $"ConnectionString trong {candidate} dang rong.";
                    continue;
                }

                connectionString = value;
                configPath = candidate;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Doc dbConfig that bai: {ex.Message}";
            }
        }

        if (string.IsNullOrWhiteSpace(error))
            error = "Khong tim thay GameServer/Config/dbConfig.json tu vi tri chay tool.";

        return false;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var current = Path.GetFullPath(start);
            while (!string.IsNullOrWhiteSpace(current))
            {
                var candidate = Path.Combine(current, "GameServer", "Config", "dbConfig.json");
                if (visited.Add(candidate))
                    yield return candidate;

                var parent = Directory.GetParent(current);
                if (parent is null)
                    break;

                current = parent.FullName;
            }
        }
    }
}
