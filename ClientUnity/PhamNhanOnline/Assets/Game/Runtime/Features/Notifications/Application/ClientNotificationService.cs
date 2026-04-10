using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Notifications.Application
{
    public sealed class ClientNotificationService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientNotificationState notificationState;

        private TaskCompletionSource<NotificationAcknowledgeResult> acknowledgeCompletionSource;

        public ClientNotificationService(
            ClientConnectionService connection,
            ClientNotificationState notificationState)
        {
            this.connection = connection;
            this.notificationState = notificationState;

            connection.Packets.Subscribe<PlayerNotificationReceivedPacket>(HandleNotificationReceived);
            connection.Packets.Subscribe<AcknowledgePlayerNotificationResultPacket>(HandleAcknowledgeResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<NotificationAcknowledgeResult> AcknowledgeAsync(long notificationId)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new NotificationAcknowledgeResult(
                    false,
                    null,
                    null,
                    "Not connected to server."));
            }

            if (acknowledgeCompletionSource != null && !acknowledgeCompletionSource.Task.IsCompleted)
                return acknowledgeCompletionSource.Task;

            acknowledgeCompletionSource = new TaskCompletionSource<NotificationAcknowledgeResult>();
            connection.Send(new AcknowledgePlayerNotificationPacket
            {
                NotificationId = notificationId
            });
            return acknowledgeCompletionSource.Task;
        }

        private void HandleNotificationReceived(PlayerNotificationReceivedPacket packet)
        {
            if (!packet.Notification.HasValue)
                return;

            notificationState.Enqueue(packet.Notification.Value);
        }

        private void HandleAcknowledgeResult(AcknowledgePlayerNotificationResultPacket packet)
        {
            if (packet.Success == true && packet.NotificationId.HasValue)
                notificationState.Remove(packet.NotificationId.Value);

            var message = packet.Success == true
                ? "Notification acknowledged."
                : string.Format("Failed to acknowledge notification: {0}", packet.Code ?? MessageCode.UnknownError);

            var pending = acknowledgeCompletionSource;
            acknowledgeCompletionSource = null;
            if (pending != null)
            {
                pending.TrySetResult(new NotificationAcknowledgeResult(
                    packet.Success == true,
                    packet.Code,
                    packet.NotificationId,
                    message));
            }
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            notificationState.Clear();
            var pending = acknowledgeCompletionSource;
            acknowledgeCompletionSource = null;
            if (pending != null)
            {
                pending.TrySetResult(new NotificationAcknowledgeResult(
                    false,
                    null,
                    null,
                    "Connection closed."));
            }
        }
    }

    public readonly struct NotificationAcknowledgeResult
    {
        public NotificationAcknowledgeResult(
            bool success,
            MessageCode? code,
            long? notificationId,
            string message)
        {
            Success = success;
            Code = code;
            NotificationId = notificationId;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public long? NotificationId { get; }
        public string Message { get; }
    }
}
