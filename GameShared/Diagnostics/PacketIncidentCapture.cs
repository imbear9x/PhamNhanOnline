using System.Text.Json;
using GameShared.Logging;

namespace GameShared.Diagnostics;

public static class PacketIncidentCapture
{
    private const string Marker = "PACKET_INCIDENT ";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private static bool _enabled = true;

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public static void Configure(bool enabled)
    {
        _enabled = enabled;
    }

    public static void Log(PacketIncidentRecord record)
    {
        if (!_enabled)
            return;

        try
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            Logger.Error($"{Marker}{json}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to serialize packet incident record.");
        }
    }

    public static bool TryParse(string text, out PacketIncidentRecord? record)
    {
        record = null;
        var json = ExtractJson(text);
        if (json is null)
            return false;

        try
        {
            record = JsonSerializer.Deserialize<PacketIncidentRecord>(json, JsonOptions);
            return record is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        var markerIndex = trimmed.IndexOf(Marker, StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            return trimmed[(markerIndex + Marker.Length)..].Trim();
        }

        var first = trimmed.IndexOf('{');
        var last = trimmed.LastIndexOf('}');
        if (first >= 0 && last > first)
            return trimmed[first..(last + 1)];

        return null;
    }
}
