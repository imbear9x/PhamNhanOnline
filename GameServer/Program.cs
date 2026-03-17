using System.Threading;
using GameServer.Diagnostics;
using GameServer.Extensions;
using GameServer.Network;
using GameServer.Randomness;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Diagnostics;
using GameShared.Logging;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        Logger.Configure(GetLogRootPath(args));
        PacketIncidentCapture.Configure(GetBoolArg(args, "--packetIncidentCapture=", true));
        Logger.Info("Game server bootstrap started.");
        Logger.Info($"Packet incident capture enabled: {PacketIncidentCapture.Enabled}");

        var services = new ServiceCollection();

        services
            .AddDatabase()
            .AddNetworking()
            .AddMiddleWare()
            .AddWorldSystems()
            .AddGameServices()
            .AddDomainHandler()
            .AddRepositories();

        Logger.Info("Service registration completed.");
        using var provider = services.BuildServiceProvider();

        Logger.Info("ServiceProvider built.");
        if (TryRunCommandMode(args, provider))
            return;

        var server = provider.GetRequiredService<NetworkServer>();
        var gameLoop = provider.GetRequiredService<GameLoop>();
        var maintenance = provider.GetRequiredService<RuntimeMaintenanceService>();
        var metricsLogger = provider.GetRequiredService<ServerMetricsLoggerService>();
        using var shutdownCts = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdownCts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            Logger.Info("NetworkServer resolved.");
            server.Start();
            gameLoop.Start();
            maintenance.Start();
            metricsLogger.Start();

            Logger.Info("Game server started on port 7777.");
            Console.WriteLine("Game server started on port 7777");
            Console.WriteLine("Press Ctrl+C to stop the server.");

            while (!shutdownCts.IsCancellationRequested)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            Logger.Info("Game server shutdown started.");

            metricsLogger.Stop();
            maintenance.Stop();
            gameLoop.Stop();
            server.Stop();

            Logger.Info("Game server shutdown completed.");
        }
    }

    private static string? GetLogRootPath(string[] args)
    {
        const string prefix = "--logRoot=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return null;
    }

    private static bool GetBoolArg(string[] args, string prefix, bool defaultValue)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = arg[prefix.Length..];
            if (bool.TryParse(raw, out var parsed))
                return parsed;
        }

        return defaultValue;
    }

    private static bool TryRunCommandMode(string[] args, ServiceProvider provider)
    {
        var command = GetStringArg(args, "--command=");
        if (string.IsNullOrWhiteSpace(command))
            return false;

        if (string.Equals(command, "sync-game-time-config", StringComparison.OrdinalIgnoreCase))
        {
            RunSyncGameTimeConfig(provider);
            return true;
        }

        if (string.Equals(command, "preview-random-table", StringComparison.OrdinalIgnoreCase))
        {
            RunPreviewRandomTable(provider, args);
            return true;
        }

        throw new ArgumentException($"Unknown command: {command}");
    }

    private static void RunSyncGameTimeConfig(ServiceProvider provider)
    {
        var bootstrapConfig = provider.GetRequiredService<GameTimeConfig>();
        var gameTimeService = provider.GetRequiredService<GameTimeService>();
        var updated = gameTimeService.ApplyConfigAsync(bootstrapConfig).GetAwaiter().GetResult();
        var snapshot = gameTimeService.GetCurrentSnapshot();

        Logger.Info(
            $"Game time config synced. Scale={updated.GameMinutesPerRealMinute}, DaysPerYear={updated.DaysPerGameYear}, RuntimeSaveInterval={updated.RuntimeSaveIntervalSeconds}, DerivedRefreshInterval={updated.DerivedStateRefreshIntervalSeconds}, AnchorGameMinute={updated.AnchorGameMinute}");

        Console.WriteLine("Game time config synced from gameTimeConfig.json.");
        Console.WriteLine($"GameMinutesPerRealMinute={updated.GameMinutesPerRealMinute}");
        Console.WriteLine($"DaysPerGameYear={updated.DaysPerGameYear}");
        Console.WriteLine($"RuntimeSaveIntervalSeconds={updated.RuntimeSaveIntervalSeconds}");
        Console.WriteLine($"DerivedStateRefreshIntervalSeconds={updated.DerivedStateRefreshIntervalSeconds}");
        Console.WriteLine($"CurrentGameMinute={snapshot.CurrentGameMinute}");
    }

    private static void RunPreviewRandomTable(ServiceProvider provider, string[] args)
    {
        var tableId = GetStringArg(args, "--tableId=");
        if (string.IsNullOrWhiteSpace(tableId))
            throw new ArgumentException("Missing --tableId= for preview-random-table.");

        var fortune = GetNullableDoubleArg(args, "--fortune=");
        var rolls = Math.Clamp(GetIntArg(args, "--rolls=", 3), 1, 100);
        var randomService = provider.GetRequiredService<IGameRandomService>();
        var context = new GameRandomContext();
        var options = new GameRandomOptions(Fortune: fortune);
        var preview = randomService.PreviewTable(tableId, context, options);

        Console.WriteLine($"Random table: {preview.TableId}");
        Console.WriteLine($"Mode: {preview.Mode}");
        Console.WriteLine(preview.Fortune.HasValue
            ? $"Fortune: {preview.Fortune.Value:0.##}"
            : "Fortune: <disabled>");
        Console.WriteLine($"AppliedFortuneBonusPpm: {preview.AppliedFortuneBonusPartsPerMillion}");
        Console.WriteLine("Effective entries:");
        foreach (var entry in preview.EffectiveEntries)
        {
            var tags = entry.Tags.Count == 0 ? "-" : string.Join(",", entry.Tags);
            Console.WriteLine(
                $"- {entry.EntryId} | base={entry.BaseChancePartsPerMillion} | effective={entry.EffectiveChancePartsPerMillion} | bonus={entry.BonusChancePartsPerMillion} | none={entry.IsNone} | tags={tags}");
        }

        Console.WriteLine($"Sample rolls ({rolls}):");
        for (var i = 0; i < rolls; i++)
        {
            var result = randomService.Roll(tableId, context, options);
            Console.WriteLine($"- roll={result.RollValue} -> {result.SelectedEntry.EntryId}");
        }
    }

    private static string? GetStringArg(string[] args, string prefix)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return null;
    }

    private static int GetIntArg(string[] args, string prefix, int defaultValue)
    {
        var raw = GetStringArg(args, prefix);
        return int.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static double GetDoubleArg(string[] args, string prefix, double defaultValue)
    {
        var raw = GetStringArg(args, prefix);
        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static double? GetNullableDoubleArg(string[] args, string prefix)
    {
        var raw = GetStringArg(args, prefix);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
