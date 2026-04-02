using System;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public readonly struct CharacterStateTransitionNotice
    {
        public CharacterStateTransitionNotice(Guid? characterId, int reason)
        {
            CharacterId = characterId;
            Reason = reason;
        }

        public Guid? CharacterId { get; }
        public int Reason { get; }
    }
}
