using System;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Application;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Features.Auth.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.Infrastructure.SceneLoading;
using PhamNhanOnline.Client.Network.Packets;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.Network.Transport;
using PhamNhanOnline.Client.UI.Common;

namespace PhamNhanOnline.Client.Core.Application
{
    public static class ClientRuntime
    {
        public static bool IsInitialized { get; private set; }
        public static ClientBootstrapSettings Settings { get; private set; }
        public static ISceneFlowService SceneFlow { get; private set; }
        public static ClientConnectionService Connection { get; private set; }
        public static ClientPacketDispatcher PacketDispatcher { get; private set; }
        public static ClientAuthState Auth { get; private set; }
        public static ClientAuthService AuthService { get; private set; }
        public static ClientConnectionRecoveryService ConnectionRecovery { get; private set; }
        public static ClientCharacterState Character { get; private set; }
        public static ClientCharacterService CharacterService { get; private set; }
        public static ClientCombatDeathRecoveryService CombatDeathRecoveryService { get; private set; }
        public static ClientInventoryState Inventory { get; private set; }
        public static ClientInventoryService InventoryService { get; private set; }
        public static ClientMartialArtState MartialArts { get; private set; }
        public static ClientMartialArtService MartialArtService { get; private set; }
        public static ClientSkillState Skills { get; private set; }
        public static ClientSkillService SkillService { get; private set; }
        public static ClientCombatState Combat { get; private set; }
        public static ClientCombatService CombatService { get; private set; }
        public static ClientSkillPresentationState SkillPresentation { get; private set; }
        public static ClientSkillPresentationService SkillPresentationService { get; private set; }
        public static ClientTargetState Target { get; private set; }
        public static ClientWorldState World { get; private set; }
        public static ClientWorldService WorldService { get; private set; }
        public static ClientGroundRewardService GroundRewardService { get; private set; }
        public static ClientWorldTravelService WorldTravelService { get; private set; }
        public static ClientLoginFlowService LoginFlow { get; private set; }
        public static UiScreenService UiScreens { get; private set; }

        public static void Initialize(ClientBootstrapSettings settings)
        {
            if (IsInitialized)
                return;

            if (settings == null)
                throw new ArgumentNullException("settings");

            Settings = settings;
            ClientLog.VerboseEnabled = settings.VerboseLogging;

            SceneFlow = new UnitySceneFlowService();
            PacketDispatcher = new ClientPacketDispatcher();
            Connection = new ClientConnectionService(new LiteNetLibClientTransport(), settings.ServerEndpoint, PacketDispatcher);
            Auth = new ClientAuthState();
            Character = new ClientCharacterState();
            Inventory = new ClientInventoryState();
            MartialArts = new ClientMartialArtState();
            Skills = new ClientSkillState();
            Combat = new ClientCombatState();
            SkillPresentation = new ClientSkillPresentationState();
            Target = new ClientTargetState();
            World = new ClientWorldState();
            UiScreens = new UiScreenService();
            AuthService = new ClientAuthService(Connection, Auth);
            CharacterService = new ClientCharacterService(Connection, Character);
            CombatDeathRecoveryService = new ClientCombatDeathRecoveryService(Connection, Character);
            ConnectionRecovery = new ClientConnectionRecoveryService(Connection, AuthService, Auth, CharacterService, Character, SceneFlow, settings);
            InventoryService = new ClientInventoryService(Connection, Character, Inventory);
            MartialArtService = new ClientMartialArtService(Connection, Character, MartialArts);
            SkillService = new ClientSkillService(Connection, Skills);
            CombatService = new ClientCombatService(Connection, Combat, Character);
            SkillPresentationService = new ClientSkillPresentationService(Combat, Skills, SkillPresentation);
            WorldService = new ClientWorldService(Connection, World, Character, Target);
            GroundRewardService = new ClientGroundRewardService(Connection, InventoryService, Target);
            WorldTravelService = new ClientWorldTravelService(Connection);
            LoginFlow = new ClientLoginFlowService(Connection, AuthService, CharacterService, SceneFlow, settings);

            IsInitialized = true;
            ClientLog.Info("Client runtime initialized.");
        }
    }
}
