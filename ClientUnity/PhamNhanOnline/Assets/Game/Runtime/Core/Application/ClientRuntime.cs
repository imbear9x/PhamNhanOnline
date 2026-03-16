using System;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Auth.Application;
using PhamNhanOnline.Client.Features.Character.Application;
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
        public static ClientCharacterState Character { get; private set; }
        public static ClientCharacterService CharacterService { get; private set; }
        public static ClientWorldState World { get; private set; }
        public static ClientWorldService WorldService { get; private set; }
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
            World = new ClientWorldState();
            UiScreens = new UiScreenService();
            AuthService = new ClientAuthService(Connection, Auth);
            CharacterService = new ClientCharacterService(Connection, Character);
            WorldService = new ClientWorldService(Connection, World, Character);
            WorldTravelService = new ClientWorldTravelService(Connection);
            LoginFlow = new ClientLoginFlowService(Connection, AuthService, CharacterService, SceneFlow, settings);

            IsInitialized = true;
            ClientLog.Info("Client runtime initialized.");
        }
    }
}


