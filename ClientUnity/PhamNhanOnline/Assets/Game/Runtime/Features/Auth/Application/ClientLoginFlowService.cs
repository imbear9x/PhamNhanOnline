using System.Threading.Tasks;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.Infrastructure.SceneLoading;
using PhamNhanOnline.Client.Network.Session;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public sealed class ClientLoginFlowService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientAuthService authService;
        private readonly ClientCharacterService characterService;
        private readonly ISceneFlowService sceneFlow;
        private readonly ClientBootstrapSettings settings;

        public ClientLoginFlowService(
            ClientConnectionService connection,
            ClientAuthService authService,
            ClientCharacterService characterService,
            ISceneFlowService sceneFlow,
            ClientBootstrapSettings settings)
        {
            this.connection = connection;
            this.authService = authService;
            this.characterService = characterService;
            this.sceneFlow = sceneFlow;
            this.settings = settings;
        }

        public async Task<LoginFlowResult> ConnectLoginAndEnterWorldAsync(string username, string password)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                var connectResult = await connection.ConnectAsync();
                if (!connectResult.Success)
                    return LoginFlowResult.Failed(connectResult.Message);
            }

            var authResult = await authService.LoginAsync(username, password);
            if (!authResult.Success)
                return LoginFlowResult.Failed(authResult.Message);

            var listResult = await characterService.LoadCharacterListAsync();
            if (!listResult.Success)
                return LoginFlowResult.Failed(listResult.Message);

            if (listResult.Characters.Length == 0)
                return LoginFlowResult.RequiresCharacterCreationResult("Account has no characters yet. Create one to continue.");

            var selectedCharacter = listResult.Characters[0];
            var enterWorldResult = await characterService.EnterWorldAsync(selectedCharacter.CharacterId);
            if (!enterWorldResult.Success)
                return LoginFlowResult.Failed(enterWorldResult.Message);

            if (sceneFlow.ActiveSceneName != settings.WorldSceneName)
                await sceneFlow.LoadSceneAsync(settings.WorldSceneName, LoadSceneMode.Single);

            return LoginFlowResult.Succeeded(string.Format("Entered world as {0}.", selectedCharacter.Name));
        }
    }
}
