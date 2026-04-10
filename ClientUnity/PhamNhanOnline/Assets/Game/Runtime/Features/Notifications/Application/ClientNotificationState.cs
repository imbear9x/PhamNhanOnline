using System;
using System.Collections.Generic;
using System.Linq;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Notifications.Application
{
    public sealed class ClientNotificationState
    {
        private readonly List<PlayerNotificationModel> unreadNotifications = new List<PlayerNotificationModel>(4);

        public event Action Changed;

        public PlayerNotificationModel[] UnreadNotifications => unreadNotifications.ToArray();

        public PlayerNotificationModel? CurrentNotification =>
            unreadNotifications.Count > 0 ? unreadNotifications[0] : (PlayerNotificationModel?)null;

        public void Enqueue(PlayerNotificationModel notification)
        {
            for (var i = 0; i < unreadNotifications.Count; i++)
            {
                if (unreadNotifications[i].NotificationId != notification.NotificationId)
                    continue;

                unreadNotifications[i] = notification;
                SortUnread();
                NotifyChanged();
                return;
            }

            unreadNotifications.Add(notification);
            SortUnread();
            NotifyChanged();
        }

        public void Remove(long notificationId)
        {
            for (var i = 0; i < unreadNotifications.Count; i++)
            {
                if (unreadNotifications[i].NotificationId != notificationId)
                    continue;

                unreadNotifications.RemoveAt(i);
                NotifyChanged();
                return;
            }
        }

        public void Clear()
        {
            if (unreadNotifications.Count == 0)
                return;

            unreadNotifications.Clear();
            NotifyChanged();
        }

        private void SortUnread()
        {
            unreadNotifications.Sort(static (left, right) =>
            {
                var createdCompare = Nullable.Compare(left.CreatedUnixMs, right.CreatedUnixMs);
                if (createdCompare != 0)
                    return createdCompare;

                return left.NotificationId.CompareTo(right.NotificationId);
            });
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
