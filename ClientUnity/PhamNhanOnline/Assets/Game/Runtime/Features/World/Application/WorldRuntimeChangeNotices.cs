using System;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public readonly struct ObservedCharacterMovedNotice
    {
        public ObservedCharacterMovedNotice(
            Guid characterId,
            ObservedCharacterModel character,
            float previousPosX,
            float previousPosY,
            float currentPosX,
            float currentPosY)
        {
            CharacterId = characterId;
            Character = character;
            PreviousPosX = previousPosX;
            PreviousPosY = previousPosY;
            CurrentPosX = currentPosX;
            CurrentPosY = currentPosY;
        }

        public Guid CharacterId { get; }
        public ObservedCharacterModel Character { get; }
        public float PreviousPosX { get; }
        public float PreviousPosY { get; }
        public float CurrentPosX { get; }
        public float CurrentPosY { get; }
    }

    public readonly struct ObservedCharacterStateChangedNotice
    {
        public ObservedCharacterStateChangedNotice(
            Guid characterId,
            ObservedCharacterModel character,
            CharacterCurrentStateModel previousState,
            CharacterCurrentStateModel currentState,
            int previousMaxHp,
            int currentMaxHp,
            int previousMaxMp,
            int currentMaxMp)
        {
            CharacterId = characterId;
            Character = character;
            PreviousState = previousState;
            CurrentState = currentState;
            PreviousMaxHp = previousMaxHp;
            CurrentMaxHp = currentMaxHp;
            PreviousMaxMp = previousMaxMp;
            CurrentMaxMp = currentMaxMp;
        }

        public Guid CharacterId { get; }
        public ObservedCharacterModel Character { get; }
        public CharacterCurrentStateModel PreviousState { get; }
        public CharacterCurrentStateModel CurrentState { get; }
        public int PreviousMaxHp { get; }
        public int CurrentMaxHp { get; }
        public int PreviousMaxMp { get; }
        public int CurrentMaxMp { get; }
    }

    public readonly struct EnemyHpChangedNotice
    {
        public EnemyHpChangedNotice(
            int runtimeId,
            EnemyRuntimeModel enemy,
            int previousCurrentHp,
            int currentCurrentHp,
            int previousMaxHp,
            int currentMaxHp,
            int previousRuntimeState,
            int currentRuntimeState)
        {
            RuntimeId = runtimeId;
            Enemy = enemy;
            PreviousCurrentHp = previousCurrentHp;
            CurrentCurrentHp = currentCurrentHp;
            PreviousMaxHp = previousMaxHp;
            CurrentMaxHp = currentMaxHp;
            PreviousRuntimeState = previousRuntimeState;
            CurrentRuntimeState = currentRuntimeState;
        }

        public int RuntimeId { get; }
        public EnemyRuntimeModel Enemy { get; }
        public int PreviousCurrentHp { get; }
        public int CurrentCurrentHp { get; }
        public int PreviousMaxHp { get; }
        public int CurrentMaxHp { get; }
        public int PreviousRuntimeState { get; }
        public int CurrentRuntimeState { get; }
    }
}
