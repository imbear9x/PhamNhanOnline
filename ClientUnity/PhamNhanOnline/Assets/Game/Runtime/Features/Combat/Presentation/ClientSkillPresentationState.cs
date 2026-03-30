using System;
using System.Collections.Generic;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    public sealed class ClientSkillPresentationState
    {
        private readonly Dictionary<SkillExecutionKey, SkillPresentationExecutionSnapshot> activeExecutions =
            new Dictionary<SkillExecutionKey, SkillPresentationExecutionSnapshot>();

        public event Action Changed;
        public event Action<SkillPresentationExecutionSnapshot> ExecutionStarted;
        public event Action<SkillPresentationExecutionSnapshot> ExecutionPhaseChanged;
        public event Action<SkillPresentationExecutionSnapshot> ExecutionCompleted;

        public IReadOnlyCollection<SkillPresentationExecutionSnapshot> ActiveExecutions => activeExecutions.Values;

        public void BeginExecution(SkillPresentationExecutionSnapshot snapshot)
        {
            activeExecutions[snapshot.Key] = snapshot;
            NotifyChanged();

            var handler = ExecutionStarted;
            if (handler != null)
                handler(snapshot);
        }

        public void UpdateExecution(SkillPresentationExecutionSnapshot snapshot)
        {
            activeExecutions[snapshot.Key] = snapshot;
            NotifyChanged();

            var handler = ExecutionPhaseChanged;
            if (handler != null)
                handler(snapshot);
        }

        public void CompleteExecution(SkillPresentationExecutionSnapshot snapshot)
        {
            activeExecutions.Remove(snapshot.Key);
            NotifyChanged();

            var handler = ExecutionCompleted;
            if (handler != null)
                handler(snapshot);
        }

        public bool TryGetExecution(SkillExecutionKey key, out SkillPresentationExecutionSnapshot snapshot)
        {
            return activeExecutions.TryGetValue(key, out snapshot);
        }

        public void Clear()
        {
            if (activeExecutions.Count == 0)
                return;

            activeExecutions.Clear();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
