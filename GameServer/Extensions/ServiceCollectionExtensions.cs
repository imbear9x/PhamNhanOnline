using System.Text.Json;
using GameServer.Database;
using GameServer.Network;
using GameServer.Network.Handlers;
using GameServer.Network.Interface;
using GameServer.Network.Middleware;
using GameServer.Repositories;
using GameServer.Services;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        // add game services
        services.AddScoped<AccountService>();
        services.AddScoped<CharacterService>();
        

        return services;
    }
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // add game AddRepositories DB
        services.AddScoped<AccountRepository>();
        services.AddScoped<CharacterRepository>();
        services.AddScoped<AccountCredentialRepository>();
        

        return services;
    }
    public static IServiceCollection AddNetworking(this IServiceCollection services)
    {
        services.AddSingleton<NetworkServer>();
        services.AddSingleton<INetworkSender>(p => p.GetRequiredService<NetworkServer>());

        services.AddSingleton<PacketDispatcher>();
        services.AddScoped<IPacketMiddleware, AuthMiddleware>();
        services.AddScoped<IPacketMiddleware, RateLimitMiddleware>();
        return services;
    }
    public static IServiceCollection AddWorldSystems(this IServiceCollection services)
    {
        


        return services;
    }
    
    public static IServiceCollection AddDomainHandler(this IServiceCollection services)
    {
        services.AddScoped<IPacketHandler<LoginPacket>, LoginHandler>();
        services.AddScoped<IPacketHandler<RegisterPacket>, RegisterHandler>();



        return services;
    }
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "dbConfig.json");

        if (!File.Exists(path))
            throw new Exception($"Config file not found: {path}");

        var json = File.ReadAllText(path);


        var dbConfig = JsonSerializer.Deserialize<DbConfig>(json);
        services.AddScoped<GameDb>(sp =>
        {
            return new GameDb(dbConfig.ConnectionString);
        });

        return services;
    }
}

