using PhamNhanOnline.Client.Core.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class PlayerStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private Sprite fallbackAvatarSprite;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private StatBarView hpBar;
        [SerializeField] private StatBarView mpBar;

        [Header("Fallback")]
        [SerializeField] private string defaultCharacterName = "Nhan vat";

        private bool lastInitializedState;
        private string lastCharacterName = string.Empty;
        private int lastCurrentHp = int.MinValue;
        private int lastMaxHp = int.MinValue;
        private int lastCurrentMp = int.MinValue;
        private int lastMaxMp = int.MinValue;

        private void Awake()
        {
            ApplyFallbackAvatar();
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        public void Refresh(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                if (!force && !lastInitializedState)
                    return;

                lastInitializedState = false;
                ApplyDisplay(defaultCharacterName, 0, 0, 0, 0, force: true);
                return;
            }

            lastInitializedState = true;

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

            ApplyDisplay(characterName, currentHp, maxHp, currentMp, maxMp, force);
        }

        private void ApplyDisplay(
            string characterName,
            int currentHp,
            int maxHp,
            int currentMp,
            int maxMp,
            bool force)
        {
            var changed =
                force ||
                !string.Equals(lastCharacterName, characterName) ||
                lastCurrentHp != currentHp ||
                lastMaxHp != maxHp ||
                lastCurrentMp != currentMp ||
                lastMaxMp != maxMp;

            if (!changed)
                return;

            lastCharacterName = characterName;
            lastCurrentHp = currentHp;
            lastMaxHp = maxHp;
            lastCurrentMp = currentMp;
            lastMaxMp = maxMp;

            if (nameText != null)
                nameText.text = characterName;

            if (hpBar != null)
                hpBar.SetValues(currentHp, maxHp, force: true);

            if (mpBar != null)
                mpBar.SetValues(currentMp, maxMp, force: true);

            ApplyFallbackAvatar();
        }

        private void ApplyFallbackAvatar()
        {
            if (avatarImage == null || fallbackAvatarSprite == null)
                return;

            if (avatarImage.sprite != fallbackAvatarSprite)
                avatarImage.sprite = fallbackAvatarSprite;
        }
    }
}
