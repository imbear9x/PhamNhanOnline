using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class CharacterOverviewView : MonoBehaviour
    {
        [Header("Subviews")]
        [SerializeField] private CharacterSummaryView characterSummaryView;
        [SerializeField] private CharacterEquipmentLoadoutView characterEquipmentLoadoutView;

        private bool runtimeEventsBound;

        private void Awake()
        {
            if (characterSummaryView == null)
                characterSummaryView = GetComponentInChildren<CharacterSummaryView>(true);
        }

        private void OnEnable()
        {
            BindRuntimeEvents();
            Refresh(force: true);
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            if (BindRuntimeEvents())
                Refresh(force: true);
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        private bool BindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return false;

            ClientRuntime.Character.SelectedCharacterChanged += HandleCharacterChanged;
            ClientRuntime.Character.BaseStatsChanged += HandleCharacterBaseStatsChanged;
            ClientRuntime.Character.CurrentStateChanged += HandleCharacterCurrentStateChanged;
            ClientRuntime.Inventory.Changed += HandleInventoryChanged;
            runtimeEventsBound = true;
            return true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound)
                return;

            if (ClientRuntime.IsInitialized)
            {
                ClientRuntime.Character.SelectedCharacterChanged -= HandleCharacterChanged;
                ClientRuntime.Character.BaseStatsChanged -= HandleCharacterBaseStatsChanged;
                ClientRuntime.Character.CurrentStateChanged -= HandleCharacterCurrentStateChanged;
                ClientRuntime.Inventory.Changed -= HandleInventoryChanged;
            }

            runtimeEventsBound = false;
        }

        private void HandleCharacterChanged()
        {
            if (isActiveAndEnabled)
                Refresh(force: true);
        }

        private void HandleCharacterBaseStatsChanged(CharacterBaseStatsChangeNotice notice)
        {
            if (isActiveAndEnabled)
                Refresh(force: true);
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            if (isActiveAndEnabled)
                Refresh(force: true);
        }

        private void HandleInventoryChanged()
        {
            if (isActiveAndEnabled)
                Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                characterSummaryView?.ApplyCharacterState(null, null, null, force);
                characterEquipmentLoadoutView?.Clear(force);
                return;
            }

            characterSummaryView?.ApplyCharacterState(
                ClientRuntime.Character.SelectedCharacter,
                ClientRuntime.Character.CurrentState,
                ClientRuntime.Character.BaseStats,
                force);

            if (ClientRuntime.Inventory.HasLoadedInventory)
                characterEquipmentLoadoutView?.SetItems(
                    ClientRuntime.Inventory.Items,
                    ClientRuntime.Inventory.EquipmentSlotCount,
                    force);
            else
                characterEquipmentLoadoutView?.Clear(force);
        }
    }
}
