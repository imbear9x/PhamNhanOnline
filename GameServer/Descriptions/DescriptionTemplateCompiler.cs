using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GameServer.Descriptions;

public sealed class DescriptionTemplateCompiler
{
    private static readonly Regex TokenRegex = new(
        @"\{(?<key>[A-Za-z0-9_.]+)(\|(?<format>[A-Za-z0-9_]+))?\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public DescriptionCompileResult Compile(string template, DescriptionTemplateContext context)
    {
        if (string.IsNullOrWhiteSpace(template))
            return DescriptionCompileResult.Empty;

        var missingTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var compiled = TokenRegex.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            if (!context.TryGetValue(key, out var value))
            {
                missingTokens.Add(key);
                return match.Value;
            }

            return FormatValue(value, format);
        });

        return new DescriptionCompileResult(
            NormalizeCompiledText(compiled),
            missingTokens.Count == 0 ? Array.Empty<string>() : missingTokens.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static string FormatValue(object? value, string? format)
    {
        if (value is null)
            return string.Empty;

        var normalizedFormat = string.IsNullOrWhiteSpace(format) ? "plain" : format.Trim().ToLowerInvariant();
        return normalizedFormat switch
        {
            "plain" => ConvertToPlainString(value),
            "number" => FormatNumber(value, signed: false),
            "signed_number" => FormatNumber(value, signed: true),
            "percent" => FormatPercent(value, signed: false, normalizeRatio: false),
            "signed_percent" => FormatPercent(value, signed: true, normalizeRatio: false),
            "ratio_percent" => FormatPercent(value, signed: false, normalizeRatio: true),
            "signed_ratio_percent" => FormatPercent(value, signed: true, normalizeRatio: true),
            "duration" => FormatDuration(value),
            "seconds" => FormatSeconds(value),
            _ => ConvertToPlainString(value)
        };
    }

    private static string ConvertToPlainString(object value)
    {
        return value switch
        {
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatNumber(object value, bool signed)
    {
        if (!TryConvertDecimal(value, out var number))
            return ConvertToPlainString(value);

        var formatted = number.ToString("0.####", CultureInfo.InvariantCulture);
        if (signed && number > 0m)
            return $"+{formatted}";

        return formatted;
    }

    private static string FormatPercent(object value, bool signed, bool normalizeRatio)
    {
        if (!TryConvertDecimal(value, out var number))
            return ConvertToPlainString(value);

        if (normalizeRatio)
            number *= 100m;

        var formatted = number.ToString("0.####", CultureInfo.InvariantCulture);
        if (signed && number > 0m)
            formatted = $"+{formatted}";

        return $"{formatted}%";
    }

    private static string FormatDuration(object value)
    {
        if (!TryConvertLong(value, out var durationMs))
            return ConvertToPlainString(value);

        if (durationMs <= 0)
            return "0s";

        if (durationMs % 1000 == 0)
            return $"{durationMs / 1000}s";

        return (durationMs / 1000d).ToString("0.##s", CultureInfo.InvariantCulture);
    }

    private static string FormatSeconds(object value)
    {
        if (!TryConvertLong(value, out var durationMs))
            return ConvertToPlainString(value);

        return (durationMs / 1000d).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string NormalizeCompiledText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0)
            return string.Empty;

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                builder.AppendLine();

            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static bool TryConvertDecimal(object value, out decimal result)
    {
        switch (value)
        {
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case double doubleValue:
                result = (decimal)doubleValue;
                return true;
            case float floatValue:
                result = (decimal)floatValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            case string text when decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0m;
                return false;
        }
    }

    private static bool TryConvertLong(object value, out long result)
    {
        switch (value)
        {
            case long longValue:
                result = longValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            case decimal decimalValue:
                result = decimal.ToInt64(decimal.Truncate(decimalValue));
                return true;
            case double doubleValue:
                result = (long)Math.Truncate(doubleValue);
                return true;
            case float floatValue:
                result = (long)Math.Truncate(floatValue);
                return true;
            case string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0L;
                return false;
        }
    }
}

public sealed class DescriptionTemplateContext
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _values[key.Trim()] = value;
    }

    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);
}

public readonly record struct DescriptionCompileResult(string Text, IReadOnlyList<string> MissingTokens)
{
    public static DescriptionCompileResult Empty { get; } = new(string.Empty, Array.Empty<string>());

    public bool Success => MissingTokens.Count == 0;
}
