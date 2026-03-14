using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public sealed class ClientAuthService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientAuthState authState;

        private TaskCompletionSource<AuthOperationResult> loginCompletionSource;
        private TaskCompletionSource<AuthOperationResult> reconnectCompletionSource;

        public ClientAuthService(ClientConnectionService connection, ClientAuthState authState)
        {
            this.connection = connection;
            this.authState = authState;

            connection.Packets.Subscribe<LoginResultPacket>(HandleLoginResult);
            connection.Packets.Subscribe<ReconnectResultPacket>(HandleReconnectResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<AuthOperationResult> LoginAsync(string username, string password)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(AuthOperationResult.From(false, null, "Not connected to server."));

            authState.RememberUsername(username);
            loginCompletionSource = new TaskCompletionSource<AuthOperationResult>();
            connection.Send(new LoginPacket
            {
                Username = username != null ? username.Trim() : string.Empty,
                Password = password ?? string.Empty
            });

            return loginCompletionSource.Task;
        }

        public Task<AuthOperationResult> ReconnectAsync(string resumeToken)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(AuthOperationResult.From(false, null, "Not connected to server."));

            reconnectCompletionSource = new TaskCompletionSource<AuthOperationResult>();
            connection.Send(new ReconnectPacket
            {
                ResumeToken = resumeToken ?? string.Empty
            });

            return reconnectCompletionSource.Task;
        }

        private void HandleLoginResult(LoginResultPacket packet)
        {
            var result = BuildResult(packet.Success, packet.Code);
            if (packet.Success == true && packet.AccountId.HasValue)
                authState.ApplyAuthenticatedSession(packet.AccountId.Value, packet.ResumeToken);

            CompletePending(ref loginCompletionSource, result);
        }

        private void HandleReconnectResult(ReconnectResultPacket packet)
        {
            var result = BuildResult(packet.Success, packet.Code);
            if (packet.Success == true && packet.AccountId.HasValue)
                authState.ApplyAuthenticatedSession(packet.AccountId.Value, packet.ResumeToken);

            CompletePending(ref reconnectCompletionSource, result);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            authState.Clear();
            CompletePending(ref loginCompletionSource, AuthOperationResult.From(false, null, "Connection closed."));
            CompletePending(ref reconnectCompletionSource, AuthOperationResult.From(false, null, "Connection closed."));
        }

        private static AuthOperationResult BuildResult(bool? success, MessageCode? code)
        {
            var isSuccess = success == true;
            var message = isSuccess
                ? "Authenticated successfully."
                : string.Format("Authentication failed: {0}", code ?? MessageCode.UnknownError);

            return AuthOperationResult.From(isSuccess, code, message);
        }

        private static void CompletePending(ref TaskCompletionSource<AuthOperationResult> completionSource, AuthOperationResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
