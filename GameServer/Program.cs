using GameServer.Extensions;
using GameServer.Network;
using GameServer.Runtime;
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
        var provider = services.BuildServiceProvider();

        Logger.Info("ServiceProvider built.");
        var server = provider.GetRequiredService<NetworkServer>();

        Logger.Info("NetworkServer resolved.");
        server.Start();
        provider.GetRequiredService<GameLoop>().Start();

        Logger.Info("Game server started on port 7777.");
        Console.WriteLine("Game server started on port 7777");

        while (true)
        {
            server.PollEvents();
            Thread.Sleep(15);
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
}
