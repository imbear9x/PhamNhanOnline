using System.Text;

namespace GameShared.Logging;

public static class Logger
{
    private static readonly object Sync = new();
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static string? _logDirectory;

    public static void Configure(string? logRootPath = null)
    {
        lock (Sync)
        {
            var root = string.IsNullOrWhiteSpace(logRootPath)
                ? ResolveDefaultRootPath()
                : Path.GetFullPath(logRootPath);

            _logDirectory = string.Equals(
                Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                "Logs",
                StringComparison.OrdinalIgnoreCase)
                ? root
                : Path.Combine(root, "Logs");

            Directory.CreateDirectory(_logDirectory);
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception exception, string? message = null)
    {
        var content = string.IsNullOrWhiteSpace(message)
            ? exception.ToString()
            : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", content);
    }

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            EnsureConfigured();

            try
            {
                var now = DateTime.Now;
                var fileName = $"{now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_logDirectory!, fileName);
                var line = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

                using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Utf8NoBom);
                writer.WriteLine(line);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Logger write failed: {ex.Message}");
            }
        }
    }

    private static void EnsureConfigured()
    {
        if (_logDirectory is not null)
            return;

        Configure();
    }

    private static string ResolveDefaultRootPath()
    {
        var current = Directory.GetCurrentDirectory();
        var fromCurrent = FindSolutionRoot(current);
        if (fromCurrent is not null)
            return fromCurrent;

        var fromBase = FindSolutionRoot(AppContext.BaseDirectory);
        if (fromBase is not null)
            return fromBase;

        return current;
    }

    private static string? FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.sln").Any())
                return directory.FullName;

            directory = directory.Parent;
        }

        return null;
    }
}
