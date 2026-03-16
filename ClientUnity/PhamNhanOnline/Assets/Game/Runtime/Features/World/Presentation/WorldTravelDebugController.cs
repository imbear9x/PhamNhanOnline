using System;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTravelDebugController : MonoBehaviour
    {
        [SerializeField] private KeyCode travelToAdjacentMapKey = KeyCode.T;
        private bool travelInFlight;

        private void Update()
        {
            if (!PhamNhanOnline.Client.Core.Application.ClientRuntime.IsInitialized)
                return;

            if (!Input.GetKeyDown(travelToAdjacentMapKey) || travelInFlight)
                return;

            var adjacentMapIds = PhamNhanOnline.Client.Core.Application.ClientRuntime.World.CurrentAdjacentMapIds;
            if (adjacentMapIds == null || adjacentMapIds.Count == 0)
            {
                ClientLog.Warn("WorldTravelDebugController found no adjacent map to travel to.");
                return;
            }

            var targetMapId = adjacentMapIds[0];
            _ = TravelAsync(targetMapId);
        }

        private async System.Threading.Tasks.Task TravelAsync(int targetMapId)
        {
            travelInFlight = true;
            try
            {
                var result = await PhamNhanOnline.Client.Core.Application.ClientRuntime.WorldTravelService.TravelToMapAsync(targetMapId);
                if (!result.Success)
                    ClientLog.Warn($"WorldTravelDebugController travel failed: {result.Message}");
                else
                    ClientLog.Info($"WorldTravelDebugController travel succeeded: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController travel exception: {ex.Message}");
            }
            finally
            {
                travelInFlight = false;
            }
        }
    }
}
