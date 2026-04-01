using System;
using System.Threading;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Packets;
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
        private const string ConnectionLostMessageText = "Mat ket noi toi server.";
        private const string AccountLoggedInElsewhereMessageText = "Tai khoan da duoc dang nhap tren thiet bi khac.";

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
        private string forcedLogoutMessage = string.Empty;
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
            connection.Packets.Subscribe<SessionTerminationPacket>(HandleSessionTermination);
            characterState.CurrentStateChanged += HandleCharacterCurrentStateChanged;
        }

        public event Action RecoveryStateChanged;

        public bool IsRecovering { get; private set; }
        public bool IsForcedLogoutPending { get; private set; }

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

                return string.Format("Dang thu ket noi lai trong {0:0} giay.", Math.Ceiling(RemainingReconnectSeconds));
            }
        }

        public string ConnectionLostMessage
        {
            get { return ConnectionLostMessageText; }
        }

        public string ActivePopupMessage
        {
            get { return IsForcedLogoutPending ? forcedLogoutMessage : ConnectionLostMessage; }
        }

        public string ActivePopupStatusText
        {
            get { return IsForcedLogoutPending ? string.Empty : RecoveryStatusText; }
        }

        public bool ActivePopupAllowClose
        {
            get { return IsForcedLogoutPending; }
        }

        public bool ShouldBlockGameplayInput
        {
            get { return IsRecovering || IsForcedLogoutPending; }
        }

        public bool ShouldPreserveRuntimeStateOnDisconnect
        {
            get { return IsRecovering || IsForcedLogoutPending || CanAttemptWorldRecovery(); }
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
            forcedLogoutMessage = string.Empty;
            lastWorldCharacterId = null;
            IsRecovering = false;
            IsForcedLogoutPending = false;
            ClearPreservedRuntimeState();
            RaiseRecoveryStateChanged();
        }

        public async void ConfirmForcedLogout()
        {
            if (!IsForcedLogoutPending)
                return;

            await CompleteForcedLogoutAsync();
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

            if (IsRecovering || IsForcedLogoutPending)
                return;

            if (!CanAttemptWorldRecovery())
                return;

            _ = AttemptWorldRecoveryAsync();
        }

        private void HandleSessionTermination(SessionTerminationPacket packet)
        {
            BeginForcedLogout(packet.Message);
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
                    var outcome = await TryReconnectOnceAsync(resumeToken, characterId.Value, cancellationToken);
                    if (outcome == RecoveryAttemptOutcome.Recovered)
                    {
                        recovered = true;
                        break;
                    }

                    if (outcome == RecoveryAttemptOutcome.ForcedLogout)
                    {
                        await HandleForcedLogoutDetectedAsync();
                        return;
                    }

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

            if (IsForcedLogoutPending)
                return;

            await HandleRecoveryFailedAsync();
        }

        private async Task<RecoveryAttemptOutcome> TryReconnectOnceAsync(string resumeToken, Guid characterId, CancellationToken cancellationToken)
        {
            var connectResult = await connection.ConnectAsync(cancellationToken);
            if (!connectResult.Success)
                return RecoveryAttemptOutcome.Retry;

            var reconnectResult = await authService.ReconnectAsync(resumeToken);
            if (!reconnectResult.Success)
            {
                if (reconnectResult.Code == MessageCode.AccountLoggedInElsewhere)
                    return RecoveryAttemptOutcome.ForcedLogout;

                return RecoveryAttemptOutcome.Retry;
            }

            var enterWorldResult = await characterService.EnterWorldAsync(characterId);
            if (!enterWorldResult.Success)
                return RecoveryAttemptOutcome.Retry;

            if (!string.Equals(sceneFlow.ActiveSceneName, settings.WorldSceneName, StringComparison.Ordinal))
                await sceneFlow.LoadSceneAsync(settings.WorldSceneName, LoadSceneMode.Single, cancellationToken);

            return RecoveryAttemptOutcome.Recovered;
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

        private async Task HandleForcedLogoutDetectedAsync()
        {
            BeginForcedLogout(AccountLoggedInElsewhereMessageText);
            await ResetConnectionAsync(CancellationToken.None);
        }

        private void BeginForcedLogout(string message)
        {
            CancelRecovery();
            IsRecovering = false;
            IsForcedLogoutPending = true;
            pendingLoginPopupMessage = string.Empty;
            forcedLogoutMessage = string.IsNullOrWhiteSpace(message)
                ? AccountLoggedInElsewhereMessageText
                : message;
            RaiseRecoveryStateChanged();
        }

        private async Task CompleteForcedLogoutAsync()
        {
            CancelRecovery();
            IsRecovering = false;
            IsForcedLogoutPending = false;
            forcedLogoutMessage = string.Empty;
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

        private enum RecoveryAttemptOutcome
        {
            Retry = 0,
            Recovered = 1,
            ForcedLogout = 2
        }
    }
}

