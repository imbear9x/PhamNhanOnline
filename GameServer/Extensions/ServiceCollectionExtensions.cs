using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Config;
using GameServer.Database;
using GameServer.Descriptions;
using GameServer.Diagnostics;
using GameServer.DTO;
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

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton(BuildCharacterCreateConfig());
        services.AddSingleton(BuildGameConfigValuesFromDatabase);

        // add game services
        services.AddScoped<AccountService>();
        services.AddScoped<CharacterService>();
        services.AddScoped<ItemService>();
        services.AddScoped<MartialArtService>();
        services.AddScoped<SkillService>();
        services.AddScoped<EquipmentService>();
        services.AddScoped<ItemUseService>();
        services.AddScoped<CraftService>();
        services.AddScoped<EquipmentStatService>();
        services.AddScoped<CharacterFinalStatService>();
        services.AddScoped<PillRecipeService>();
        services.AddScoped<AlchemyService>();
        services.AddScoped<HerbService>();
        services.AddScoped<AlchemyModelBuilder>();
        services.AddScoped<PlayerNotificationModelBuilder>();
        services.AddSingleton<PracticeService>();
        services.AddSingleton<AlchemyPracticeService>();
        services.AddSingleton<PlayerNotificationService>();

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
        services.AddScoped<GameConfigRepository>();
        services.AddScoped<MapTemplateRepository>();
        services.AddScoped<MapTemplateAdjacentMapRepository>();
        services.AddScoped<MapZoneSlotRepository>();
        services.AddScoped<MapSpawnPointRepository>();
        services.AddScoped<MapPortalRepository>();
        services.AddScoped<SpiritualEnergyTemplateRepository>();
        services.AddScoped<GameRandomTableRepository>();
        services.AddScoped<GameRandomEntryRepository>();
        services.AddScoped<GameRandomEntryTagRepository>();
        services.AddScoped<GameRandomLuckTagRepository>();
        services.AddScoped<PotentialStatUpgradeTierRepository>();
        services.AddScoped<RealmTemplateRepository>();
        services.AddScoped<BreakthroughAttemptRepository>();
        services.AddScoped<AccountCredentialRepository>();
        services.AddScoped<MartialArtRepository>();
        services.AddScoped<MartialArtStageRepository>();
        services.AddScoped<MartialArtStageStatBonusRepository>();
        services.AddScoped<SkillRepository>();
        services.AddScoped<SkillEffectRepository>();
        services.AddScoped<MartialArtSkillRepository>();
        services.AddScoped<PlayerMartialArtRepository>();
        services.AddScoped<PlayerSkillRepository>();
        services.AddScoped<PlayerSkillLoadoutRepository>();
        services.AddScoped<ItemTemplateRepository>();
        services.AddScoped<PlayerItemRepository>();
        services.AddScoped<EquipmentTemplateRepository>();
        services.AddScoped<EquipmentTemplateStatRepository>();
        services.AddScoped<PlayerEquipmentRepository>();
        services.AddScoped<PlayerEquipmentStatBonusRepository>();
        services.AddScoped<CraftRecipeRepository>();
        services.AddScoped<CraftRecipeRequirementRepository>();
        services.AddScoped<CraftRecipeMutationBonusRepository>();
        services.AddScoped<MartialArtBookTemplateRepository>();
        services.AddScoped<PlayerCaveRepository>();
        services.AddScoped<PlayerGardenPlotRepository>();
        services.AddScoped<SoilTemplateRepository>();
        services.AddScoped<PlayerSoilRepository>();
        services.AddScoped<HerbTemplateRepository>();
        services.AddScoped<HerbGrowthStageConfigRepository>();
        services.AddScoped<PlayerHerbRepository>();
        services.AddScoped<HerbHarvestOutputRepository>();
        services.AddScoped<PillTemplateRepository>();
        services.AddScoped<PillEffectRepository>();
        services.AddScoped<PillRecipeTemplateRepository>();
        services.AddScoped<PillRecipeInputRepository>();
        services.AddScoped<PlayerPillRecipeRepository>();
        services.AddScoped<PillRecipeMasteryStageRepository>();
        services.AddScoped<PlayerPracticeSessionRepository>();
        services.AddScoped<PlayerNotificationRepository>();
        services.AddScoped<EnemyTemplateRepository>();
        services.AddScoped<EnemyTemplateSkillRepository>();
        services.AddScoped<EnemyRewardRuleRepository>();
        services.AddScoped<MapEnemySpawnGroupRepository>();
        services.AddScoped<MapEnemySpawnEntryRepository>();
        services.AddScoped<MapInstanceConfigRepository>();

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
        services.AddSingleton(BuildGameRandomConfigFromDatabase);
        services.AddSingleton<GameTimeService>();
        services.AddSingleton<MapCatalog>();
        services.AddSingleton<PotentialStatCatalog>();
        services.AddSingleton<CombatDefinitionCatalog>();
        services.AddSingleton<ItemDefinitionCatalog>();
        services.AddSingleton<AlchemyDefinitionCatalog>();
        services.AddSingleton<EnemyDefinitionCatalog>();
        services.AddSingleton<DescriptionTemplateCompiler>();
        services.AddSingleton<GameplayDescriptionService>();
        services.AddSingleton<IRandomNumberProvider, CryptoRandomNumberProvider>();
        services.AddSingleton<IGameRandomService, GameRandomService>();
        services.AddSingleton<MartialArtProgressionService>();
        services.AddSingleton<SkillRuntimeBuilder>();
        services.AddSingleton<SkillExecutionService>();
        services.AddSingleton<MapManager>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<WorldInterestService>();
        services.AddSingleton<CharacterRuntimeCalculator>();
        services.AddSingleton<CharacterRuntimeNotifier>();
        services.AddSingleton<CharacterRuntimeService>();
        services.AddSingleton<CharacterCombatDeathRecoveryService>();
        services.AddSingleton<GroundItemRuntimeService>();
        services.AddSingleton<CharacterRuntimeSaveService>();
        services.AddSingleton<CharacterLifecycleService>();
        services.AddSingleton<CharacterCultivationService>();
        services.AddSingleton<EnemyRewardRuntimeService>();
        services.AddSingleton<MapInstanceLifecycleService>();
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
        services.AddScoped<IPacketHandler<GetInventoryPacket>, GetInventoryHandler>();
        services.AddScoped<IPacketHandler<EquipInventoryItemPacket>, EquipInventoryItemHandler>();
        services.AddScoped<IPacketHandler<UnequipInventoryItemPacket>, UnequipInventoryItemHandler>();
        services.AddScoped<IPacketHandler<DropInventoryItemPacket>, DropInventoryItemHandler>();
        services.AddScoped<IPacketHandler<UseItemPacket>, UseItemHandler>();
        services.AddScoped<IPacketHandler<ReturnHomeAfterCombatDeathPacket>, ReturnHomeAfterCombatDeathHandler>();
        services.AddScoped<IPacketHandler<TravelToMapPacket>, TravelToMapHandler>();
        services.AddScoped<IPacketHandler<GetMapZonesPacket>, GetMapZonesHandler>();
        services.AddScoped<IPacketHandler<SwitchMapZonePacket>, SwitchMapZoneHandler>();
        services.AddScoped<IPacketHandler<CharacterPositionSyncPacket>, CharacterPositionSyncHandler>();
        services.AddScoped<IPacketHandler<StartCultivationPacket>, StartCultivationHandler>();
        services.AddScoped<IPacketHandler<StopCultivationPacket>, StopCultivationHandler>();
        services.AddScoped<IPacketHandler<BreakthroughPacket>, BreakthroughHandler>();
        services.AddScoped<IPacketHandler<AllocatePotentialPacket>, AllocatePotentialHandler>();
        services.AddScoped<IPacketHandler<GetOwnedMartialArtsPacket>, GetOwnedMartialArtsHandler>();
        services.AddScoped<IPacketHandler<UseMartialArtBookPacket>, UseMartialArtBookHandler>();
        services.AddScoped<IPacketHandler<SetActiveMartialArtPacket>, SetActiveMartialArtHandler>();
        services.AddScoped<IPacketHandler<GetOwnedSkillsPacket>, GetOwnedSkillsHandler>();
        services.AddScoped<IPacketHandler<SetSkillLoadoutSlotPacket>, SetSkillLoadoutSlotHandler>();
        services.AddScoped<IPacketHandler<GetLearnedPillRecipesPacket>, GetLearnedPillRecipesHandler>();
        services.AddScoped<IPacketHandler<GetPillRecipeDetailPacket>, GetPillRecipeDetailHandler>();
        services.AddScoped<IPacketHandler<PreviewCraftPillPacket>, PreviewCraftPillHandler>();
        services.AddScoped<IPacketHandler<GetAlchemyPracticeStatusPacket>, GetAlchemyPracticeStatusHandler>();
        services.AddScoped<IPacketHandler<CraftPillPacket>, CraftPillHandler>();
        services.AddScoped<IPacketHandler<PausePracticePacket>, PausePracticeHandler>();
        services.AddScoped<IPacketHandler<ResumePracticePacket>, ResumePracticeHandler>();
        services.AddScoped<IPacketHandler<CancelPracticePacket>, CancelPracticeHandler>();
        services.AddScoped<IPacketHandler<AcknowledgePracticeResultPacket>, AcknowledgePracticeResultHandler>();
        services.AddScoped<IPacketHandler<AcknowledgePlayerNotificationPacket>, AcknowledgePlayerNotificationHandler>();
        services.AddScoped<IPacketHandler<AttackEnemyPacket>, AttackEnemyHandler>();
        services.AddScoped<IPacketHandler<PickupGroundRewardPacket>, PickupGroundRewardHandler>();

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
}
