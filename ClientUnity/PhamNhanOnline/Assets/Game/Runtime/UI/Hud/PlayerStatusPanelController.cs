using PhamNhanOnline.Client.Core.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class PlayerStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StatusPanelView statusPanelView;
        [SerializeField] private Sprite fallbackAvatarSprite;

        [Header("Fallback")]
        [SerializeField] private string defaultCharacterName = "Nhan vat";

        private void Awake()
        {
            AutoWireReferences();
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        public void Refresh(bool force)
        {
            if (statusPanelView == null)
                return;

            if (!ClientRuntime.IsInitialized)
            {
                statusPanelView.Apply(
                    new StatusPanelViewData(
                        true,
                        "player",
                        defaultCharacterName,
                        fallbackAvatarSprite,
                        true,
                        0,
                        0,
                        true,
                        0,
                        0),
                    force: true);
                return;
            }

            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;

            var characterName = selectedCharacter.HasValue && !string.IsNullOrWhiteSpace(selectedCharacter.Value.Name)
                ? selectedCharacter.Value.Name
                : defaultCharacterName;

            var maxHp = baseStats.HasValue
                ? Mathf.Max(0, baseStats.Value.FinalHp)
                : 0;
            var maxMp = baseStats.HasValue
                ? Mathf.Max(0, baseStats.Value.FinalMp)
                : 0;

            var currentHp = currentState.HasValue ? currentState.Value.CurrentHp : maxHp;
            var currentMp = currentState.HasValue ? currentState.Value.CurrentMp : maxMp;

            statusPanelView.Apply(
                new StatusPanelViewData(
                    true,
                    "player",
                    characterName,
                    fallbackAvatarSprite,
                    true,
                    currentHp,
                    maxHp,
                    true,
                    currentMp,
                    maxMp),
                force);
        }

        private void AutoWireReferences()
        {
            if (statusPanelView == null)
                statusPanelView = GetComponent<StatusPanelView>();
        }
    }
}
