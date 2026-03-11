using GameServer.Extensions;
using GameServer.Network;
using Microsoft.Extensions.DependencyInjection;


class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        services
            .AddDatabase()
            .AddNetworking()
            .AddWorldSystems()
            .AddGameServices()
            .AddDomainHandler()
            .AddRepositories();

        var provider = services.BuildServiceProvider();

        var server = provider.GetRequiredService<NetworkServer>();

        server.Start();

        Console.WriteLine("Game server started on port 7777");

        while (true)
        {
            server.PollEvents();
            Thread.Sleep(15);
        }
    }
}