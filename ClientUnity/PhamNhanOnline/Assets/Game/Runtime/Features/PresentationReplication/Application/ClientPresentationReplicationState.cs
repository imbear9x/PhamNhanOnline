using System;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.PresentationReplication.Application
{
    public sealed class ClientPresentationReplicationState
    {
        private readonly Queue<ClientPresentationReplicationEvent> recentEvents = new Queue<ClientPresentationReplicationEvent>();
        private const int MaxRecentEvents = 64;

        public event Action<ClientPresentationReplicationEvent> EventPublished;

        public IReadOnlyCollection<ClientPresentationReplicationEvent> RecentEvents
        {
            get { return recentEvents.ToArray(); }
        }

        public void Publish(ClientPresentationReplicationEvent replicationEvent)
        {
            recentEvents.Enqueue(replicationEvent);
            while (recentEvents.Count > MaxRecentEvents)
                recentEvents.Dequeue();

            var handler = EventPublished;
            if (handler != null)
                handler(replicationEvent);
        }

        public void Clear()
        {
            recentEvents.Clear();
        }
    }
}
