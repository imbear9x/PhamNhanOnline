using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Database;
using GameServer.Diagnostics;
using GameServer.Network;
using GameServer.Network.Handlers;
using GameServer.Network.Interface;
using GameServer.Network.Middleware;
using GameServer.Network.Validations;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions ConfigJsonOptions = BuildConfigJsonOptions();

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
        services.AddScoped<GameTimeStateRepository>();
        services.AddScoped<MapTemplateRepository>();
        services.AddScoped<MapTemplateAdjacentMapRepository>();
        services.AddScoped<MapZoneSlotRepository>();
        services.AddScoped<SpiritualEnergyTemplateRepository>();
        services.AddScoped<PotentialStatUpgradeTierRepository>();
        services.AddScoped<RealmTemplateRepository>();
        services.AddScoped<BreakthroughAttemptRepository>();
        services.AddScoped<AccountCredentialRepository>();

        return services;
    }

    public static IServiceCollection AddNetworking(this IServiceCollection services)
    {
        services.AddSingleton<ServerMetricsService>();
        services.AddSingleton<ServerMetricsLoggerService>();
        services.AddSingleton<NetworkServer>();
        services.AddSingleton<INetworkSender>(p => p.GetRequiredService<NetworkServer>());
        services.AddSingleton<PacketDispatcher>();

        return services;
    }

    public static IServiceCollection AddMiddleWare(this IServiceCollection services)
    {
        services.AddSingleton<IPacketMiddleware, RateLimitMiddleware>();
        services.AddSingleton<IPacketMiddleware, AuthMiddleware>();
        services.AddSingleton<IPacketMiddleware, CharacterActionRestrictionMiddleware>();
        services.AddSingleton<IPacketMiddleware, PacketValidationMiddleware>();

        return services;
    }

    public static IServiceCollection AddWorldSystems(this IServiceCollection services)
    {
        services.AddSingleton(BuildGameTimeBootstrapConfig());
        services.AddSingleton(BuildGameRandomConfig());
        services.AddSingleton<GameTimeService>();
        services.AddSingleton<MapCatalog>();
        services.AddSingleton<PotentialStatCatalog>();
        services.AddSingleton<IRandomNumberProvider, CryptoRandomNumberProvider>();
        services.AddSingleton<IGameRandomService, GameRandomService>();
        services.AddSingleton<CharacterBaseStatsComposer>();
        services.AddSingleton<MapManager>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<WorldInterestService>();
        services.AddSingleton<CharacterRuntimeCalculator>();
        services.AddSingleton<CharacterRuntimeNotifier>();
        services.AddSingleton<CharacterRuntimeService>();
        services.AddSingleton<CharacterRuntimeSaveService>();
        services.AddSingleton<CharacterLifecycleService>();
        services.AddSingleton<CharacterCultivationService>();
        services.AddSingleton<GameLoop>();
        services.AddSingleton<RuntimeMaintenanceService>();

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
        services.AddScoped<IPacketHandler<EnterWorldPacket>, EnterWorldHandler>();
        services.AddScoped<IPacketHandler<TravelToMapPacket>, TravelToMapHandler>();
        services.AddScoped<IPacketHandler<GetMapZonesPacket>, GetMapZonesHandler>();
        services.AddScoped<IPacketHandler<SwitchMapZonePacket>, SwitchMapZoneHandler>();
        services.AddScoped<IPacketHandler<CharacterPositionSyncPacket>, CharacterPositionSyncHandler>();
        services.AddScoped<IPacketHandler<StartCultivationPacket>, StartCultivationHandler>();
        services.AddScoped<IPacketHandler<StopCultivationPacket>, StopCultivationHandler>();
        services.AddScoped<IPacketHandler<BreakthroughPacket>, BreakthroughHandler>();
        services.AddScoped<IPacketHandler<AllocatePotentialPacket>, AllocatePotentialHandler>();

        services.AddSingleton<IPacketValidator, LoginPacketValidator>();
        services.AddSingleton<IPacketValidator, RegisterPacketValidator>();
        services.AddSingleton<IPacketValidator, ReconnectPacketValidator>();
        services.AddSingleton<IPacketValidator, ChangePasswordPacketValidator>();
        services.AddSingleton<IPacketValidator, CreateCharacterPacketValidator>();
        services.AddSingleton<IPacketValidator, GetCharacterDataPacketValidator>();
        services.AddSingleton<IPacketValidator, EnterWorldPacketValidator>();
        services.AddSingleton<IPacketValidator, TravelToMapPacketValidator>();
        services.AddSingleton<IPacketValidator, AllocatePotentialPacketValidator>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var dbConfig = LoadConfig<DbConfig>("dbConfig.json");
        services.AddScoped<GameDb>(sp =>
        {
            if (string.IsNullOrWhiteSpace(dbConfig.ConnectionString))
                throw new Exception("ConnectionString is missing in Config/dbConfig.json");

            return new GameDb(dbConfig.ConnectionString);
        });

        return services;
    }

    private static GameTimeConfig BuildGameTimeBootstrapConfig()
    {
        return LoadConfig<GameTimeConfig>("gameTimeConfig.json");
    }

    private static GameRandomConfig BuildGameRandomConfig()
    {
        return LoadConfig<GameRandomConfig>("gameRandomConfig.json");
    }

    private static T LoadConfig<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", fileName);
        if (!File.Exists(path))
            throw new Exception($"Config file not found: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, ConfigJsonOptions)
               ?? throw new Exception($"Failed to deserialize config: {path}");
    }

    private static JsonSerializerOptions BuildConfigJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
