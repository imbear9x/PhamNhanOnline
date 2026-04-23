using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldSceneContext
    {
        public WorldSceneController SceneController { get; set; }
        public WorldMapPresenter WorldMapPresenter { get; set; }
        public WorldSceneReadinessService WorldSceneReadinessService { get; set; }
        public WorldLocalPlayerPresenter WorldLocalPlayerPresenter { get; set; }
        public WorldLocalMovementSyncController WorldLocalMovementSyncController { get; set; }
        public WorldRemotePlayersPresenter WorldRemotePlayersPresenter { get; set; }
        public WorldEnemiesPresenter WorldEnemiesPresenter { get; set; }
        public WorldPortalPresenter WorldPortalPresenter { get; set; }
        public WorldGroundRewardPresenter WorldGroundRewardPresenter { get; set; }
        public WorldTargetActionController WorldTargetActionController { get; set; }
        public WorldClickTargetSelectionController WorldClickTargetSelectionController { get; set; }
        public WorldAutoTargetSelectionController WorldAutoTargetSelectionController { get; set; }
        public WorldTargetLifecycleController WorldTargetLifecycleController { get; set; }
        public WorldTargetSelectionIndicatorController WorldTargetSelectionIndicatorController { get; set; }
        public Camera WorldCamera { get; set; }
        public Transform MapRoot { get; set; }
        public Transform EntitiesRoot { get; set; }
        public Transform WorldUiRoot { get; set; }
    }
}
