using System.Text.Json;
using GameServer.Database;
using GameServer.Network;
using GameServer.Network.Handlers;
using GameServer.Network.Interface;
using GameServer.Network.Middleware;
using GameServer.Network.Validations;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.World;
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
        services.AddScoped<CharacterBaseStatRepository>();
        services.AddScoped<CharacterCurrentStateRepository>();
        services.AddScoped<RealmTemplateRepository>();
        services.AddScoped<AccountCredentialRepository>();
        

        return services;
    }
    public static IServiceCollection AddNetworking(this IServiceCollection services)
    {
        services.AddSingleton<NetworkServer>();
        services.AddSingleton<INetworkSender>(p => p.GetRequiredService<NetworkServer>());
        services.AddSingleton<PacketDispatcher>();
        
        return services;
    }
    public static IServiceCollection AddMiddleWare(this IServiceCollection services)
    {
        services.AddSingleton<IPacketMiddleware, RateLimitMiddleware>();
        services.AddSingleton<IPacketMiddleware, AuthMiddleware>();
        services.AddSingleton<IPacketMiddleware, PacketValidationMiddleware>();
        
        return services;
    }

    public static IServiceCollection AddWorldSystems(this IServiceCollection services)
    {
        services.AddSingleton<MapManager>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<CharacterRuntimeCalculator>();
        services.AddSingleton<CharacterRuntimeNotifier>();
        services.AddSingleton<CharacterRuntimeService>();
        services.AddSingleton<CharacterRuntimeSaveService>();
        services.AddSingleton<GameLoop>();


        return services;
    }
    
    public static IServiceCollection AddDomainHandler(this IServiceCollection services)
    {
        services.AddScoped<IPacketHandler<LoginPacket>, LoginHandler>();
        services.AddScoped<IPacketHandler<RegisterPacket>, RegisterHandler>();
        services.AddScoped<IPacketHandler<ReconnectPacket>, ReconnectHandler>();
        services.AddScoped<IPacketHandler<ChangePasswordPacket>, ChangePasswordHandler>();
        services.AddScoped<IPacketHandler<CreateCharacterPacket>, CreateCharacterHandler>();
        services.AddScoped<IPacketHandler<GetCharacterListPacket>, GetCharacterListHandler>();
        services.AddScoped<IPacketHandler<GetCharacterDataPacket>, GetCharacterDataHandler>();

        services.AddSingleton<IPacketValidator, LoginPacketValidator>();
        services.AddSingleton<IPacketValidator, RegisterPacketValidator>();
        services.AddSingleton<IPacketValidator, ReconnectPacketValidator>();
        services.AddSingleton<IPacketValidator, ChangePasswordPacketValidator>();
        services.AddSingleton<IPacketValidator, CreateCharacterPacketValidator>();
        services.AddSingleton<IPacketValidator, GetCharacterDataPacketValidator>();


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
