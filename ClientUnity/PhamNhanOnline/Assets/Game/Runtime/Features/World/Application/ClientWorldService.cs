using GameShared.Packets;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Network.Session;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientWorldService
    {
        private readonly ClientWorldState worldState;
        private readonly ClientCharacterState characterState;

        public ClientWorldService(
            ClientConnectionService connection,
            ClientWorldState worldState,
            ClientCharacterState characterState)
        {
            this.worldState = worldState;
            this.characterState = characterState;

            connection.Packets.Subscribe<MapJoinedPacket>(HandleMapJoined);
            connection.Packets.Subscribe<ObservedCharacterSpawnedPacket>(HandleObservedCharacterSpawned);
            connection.Packets.Subscribe<ObservedCharacterDespawnedPacket>(HandleObservedCharacterDespawned);
            connection.Packets.Subscribe<ObservedCharacterMovedPacket>(HandleObservedCharacterMoved);
            connection.Packets.Subscribe<ObservedCharacterCurrentStateChangedPacket>(HandleObservedCharacterCurrentStateChanged);
            connection.Packets.Subscribe<CharacterCurrentStateChangedPacket>(HandleCharacterCurrentStateChanged);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        private void HandleMapJoined(MapJoinedPacket packet)
        {
            if (!packet.Map.HasValue)
                return;

            var localPosition = Vector2.zero;
            if (characterState.CurrentState.HasValue)
            {
                var currentState = characterState.CurrentState.Value;
                localPosition = new Vector2(currentState.CurrentPosX, currentState.CurrentPosY);
            }

            worldState.ApplyMapJoin(packet.Map.Value, packet.ZoneIndex ?? 0, localPosition);
        }

        private void HandleObservedCharacterSpawned(ObservedCharacterSpawnedPacket packet)
        {
            if (!packet.Character.HasValue)
                return;

            worldState.UpsertObservedCharacter(packet.Character.Value);
        }

        private void HandleObservedCharacterDespawned(ObservedCharacterDespawnedPacket packet)
        {
            if (!packet.CharacterId.HasValue)
                return;

            worldState.RemoveObservedCharacter(packet.CharacterId.Value);
        }

        private void HandleObservedCharacterMoved(ObservedCharacterMovedPacket packet)
        {
            if (!packet.CharacterId.HasValue || !packet.CurrentPosX.HasValue || !packet.CurrentPosY.HasValue)
                return;

            worldState.ApplyObservedMove(packet.CharacterId.Value, packet.CurrentPosX.Value, packet.CurrentPosY.Value);
        }

        private void HandleObservedCharacterCurrentStateChanged(ObservedCharacterCurrentStateChangedPacket packet)
        {
            if (!packet.CurrentState.HasValue)
                return;

            worldState.ApplyObservedCurrentState(packet.CurrentState.Value);
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangedPacket packet)
        {
            if (!packet.CurrentState.HasValue)
                return;

            var currentState = packet.CurrentState.Value;
            worldState.ApplyLocalPlayerPosition(new Vector2(currentState.CurrentPosX, currentState.CurrentPosY));
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state == ClientConnectionState.Disconnected)
                worldState.Clear();
        }
    }
}
