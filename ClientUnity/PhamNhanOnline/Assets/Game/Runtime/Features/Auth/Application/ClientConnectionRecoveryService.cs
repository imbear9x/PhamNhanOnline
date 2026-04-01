using System;
using System.Threading;
using System.Threading.Tasks;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.Infrastructure.SceneLoading;
using PhamNhanOnline.Client.Network.Session;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public sealed class ClientConnectionRecoveryService
    {
        private const float RecoveryWindowSeconds = 3f;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        private readonly ClientConnectionService connection;
        private readonly ClientAuthService authService;
        private readonly ClientAuthState authState;
        private readonly ClientCharacterService characterService;
        private readonly ClientCharacterState characterState;
        private readonly ISceneFlowService sceneFlow;
        private readonly ClientBootstrapSettings settings;

        private CancellationTokenSource recoveryCancellation;
        private Guid? lastWorldCharacterId;
        private string pendingLoginPopupMessage = string.Empty;
        private DateTime recoveryDeadlineUtc;

        public ClientConnectionRecoveryService(
            ClientConnectionService connection,
            ClientAuthService authService,
            ClientAuthState authState,
            ClientCharacterService characterService,
            ClientCharacterState characterState,
            ISceneFlowService sceneFlow,
            ClientBootstrapSettings settings)
        {
            this.connection = connection;
            this.authService = authService;
            this.authState = authState;
            this.characterService = characterService;
            this.characterState = characterState;
            this.sceneFlow = sceneFlow;
            this.settings = settings;

            connection.StateChanged += HandleConnectionStateChanged;
            characterState.CurrentStateChanged += HandleCharacterCurrentStateChanged;
        }

        public event Action RecoveryStateChanged;

        public bool IsRecovering { get; private set; }

        public float RemainingReconnectSeconds
        {
            get
            {
                if (!IsRecovering)
                    return 0f;

                return Math.Max(0f, (float)(recoveryDeadlineUtc - DateTime.UtcNow).TotalSeconds);
            }
        }

        public string RecoveryStatusText
        {
            get
            {
                if (!IsRecovering)
                    return string.Empty;

                return string.Format("Đang thử kết nối lại trong {0:0} giây.", Math.Ceiling(RemainingReconnectSeconds));
            }
        }

        public string ConnectionLostMessage
        {
            get { return "Mất kết nối tới server."; }
        }

        public bool ShouldPreserveRuntimeStateOnDisconnect
        {
            get { return IsRecovering || CanAttemptWorldRecovery(); }
        }

        public bool ConsumePendingLoginPopup(out string message)
        {
            message = pendingLoginPopupMessage;
            pendingLoginPopupMessage = string.Empty;
            return !string.IsNullOrWhiteSpace(message);
        }

        public void ClearSessionContext()
        {
            CancelRecovery();
            pendingLoginPopupMessage = string.Empty;
            lastWorldCharacterId = null;
            IsRecovering = false;
            ClearPreservedRuntimeState();
            RaiseRecoveryStateChanged();
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            if (!notice.CurrentState.HasValue || !characterState.SelectedCharacterId.HasValue)
                return;

            lastWorldCharacterId = characterState.SelectedCharacterId.Value;
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            if (IsRecovering)
                return;

            if (!CanAttemptWorldRecovery())
                return;

            _ = AttemptWorldRecoveryAsync();
        }

        private bool CanAttemptWorldRecovery()
        {
            return settings != null &&
                   !string.IsNullOrWhiteSpace(settings.WorldSceneName) &&
                   string.Equals(sceneFlow.ActiveSceneName, settings.WorldSceneName, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(authState.ResumeToken) &&
                   lastWorldCharacterId.HasValue;
        }

        private async Task AttemptWorldRecoveryAsync()
        {
            var resumeToken = authState.ResumeToken;
            var characterId = lastWorldCharacterId;
            if (string.IsNullOrWhiteSpace(resumeToken) || !characterId.HasValue)
                return;

            CancelRecovery();
            recoveryCancellation = new CancellationTokenSource();
            var cancellationToken = recoveryCancellation.Token;
            var recovered = false;

            IsRecovering = true;
            recoveryDeadlineUtc = DateTime.UtcNow.AddSeconds(RecoveryWindowSeconds);
            pendingLoginPopupMessage = string.Empty;
            RaiseRecoveryStateChanged();

            try
            {
                while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < recoveryDeadlineUtc)
                {
                    recovered = await TryReconnectOnceAsync(resumeToken, characterId.Value, cancellationToken);
                    if (recovered)
                        break;

                    await ResetConnectionAsync(cancellationToken);

                    var remaining = recoveryDeadlineUtc - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var delay = remaining < RetryDelay ? remaining : RetryDelay;
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (recoveryCancellation != null && recoveryCancellation.Token == cancellationToken)
                {
                    recoveryCancellation.Dispose();
                    recoveryCancellation = null;
                }
            }

            if (recovered)
            {
                IsRecovering = false;
                RaiseRecoveryStateChanged();
                return;
            }

            await HandleRecoveryFailedAsync();
        }

        private async Task<bool> TryReconnectOnceAsync(string resumeToken, Guid characterId, CancellationToken cancellationToken)
        {
            var connectResult = await connection.ConnectAsync(cancellationToken);
            if (!connectResult.Success)
                return false;

            var reconnectResult = await authService.ReconnectAsync(resumeToken);
            if (!reconnectResult.Success)
                return false;

            var enterWorldResult = await characterService.EnterWorldAsync(characterId);
            if (!enterWorldResult.Success)
                return false;

            if (!string.Equals(sceneFlow.ActiveSceneName, settings.WorldSceneName, StringComparison.Ordinal))
                await sceneFlow.LoadSceneAsync(settings.WorldSceneName, LoadSceneMode.Single, cancellationToken);

            return true;
        }

        private async Task HandleRecoveryFailedAsync()
        {
            IsRecovering = false;
            pendingLoginPopupMessage = ConnectionLostMessage;
            RaiseRecoveryStateChanged();

            await ResetConnectionAsync(CancellationToken.None);
            authState.Clear();
            lastWorldCharacterId = null;
            ClearPreservedRuntimeState();

            if (!string.Equals(sceneFlow.ActiveSceneName, settings.LoginSceneName, StringComparison.Ordinal))
                await sceneFlow.LoadSceneAsync(settings.LoginSceneName, LoadSceneMode.Single);
        }

        private static void ClearPreservedRuntimeState()
        {
            ClientRuntime.Target?.Clear();
            ClientRuntime.SkillPresentationService?.Clear();
            ClientRuntime.World?.Clear();
            ClientRuntime.Character?.Clear();
        }

        private async Task ResetConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await connection.DisconnectAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        private void CancelRecovery()
        {
            if (recoveryCancellation == null)
                return;

            recoveryCancellation.Cancel();
            recoveryCancellation.Dispose();
            recoveryCancellation = null;
        }

        private void RaiseRecoveryStateChanged()
        {
            var handler = RecoveryStateChanged;
            if (handler != null)
                handler();
        }
    }
}

