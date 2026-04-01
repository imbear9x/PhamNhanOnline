using System;
using PhamNhanOnline.Client.Core.Application;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldConnectionDebugController : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode shortOutageKey = KeyCode.F8;
        [SerializeField] private KeyCode longOutageKey = KeyCode.F9;
        [SerializeField] private KeyCode toggleBlockKey = KeyCode.F10;
        [SerializeField] private KeyCode unblockKey = KeyCode.F11;

        [Header("Durations")]
        [SerializeField] private float shortOutageSeconds = 8f;
        [SerializeField] private float longOutageSeconds = 25f;

        [Header("Optional UI")]
        [SerializeField] private TMP_Text statusText;

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (Input.GetKeyDown(shortOutageKey))
                SimulateShortOutage();
            else if (Input.GetKeyDown(longOutageKey))
                SimulateLongOutage();
            else if (Input.GetKeyDown(toggleBlockKey))
                ToggleManualBlock();
            else if (Input.GetKeyDown(unblockKey))
                UnblockNetwork();

            RefreshStatusText();
        }

        public void SimulateShortOutage()
        {
            BlockForSeconds(shortOutageSeconds);
        }

        public void SimulateLongOutage()
        {
            BlockForSeconds(longOutageSeconds);
        }

        public void ToggleManualBlock()
        {
            if (!ClientRuntime.IsInitialized || !ClientRuntime.Connection.SupportsDebugNetworkControl)
                return;

            if (ClientRuntime.Connection.IsDebugNetworkBlocked)
                ClientRuntime.Connection.UnblockNetworkForDebug();
            else
                ClientRuntime.Connection.BlockNetworkForDebug();

            RefreshStatusText();
        }

        public void UnblockNetwork()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Connection.UnblockNetworkForDebug();
            RefreshStatusText();
        }

        private void BlockForSeconds(float seconds)
        {
            if (!ClientRuntime.IsInitialized || !ClientRuntime.Connection.SupportsDebugNetworkControl)
                return;

            var duration = TimeSpan.FromSeconds(Math.Max(0.1f, seconds));
            ClientRuntime.Connection.BlockNetworkForDebug(duration);
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (statusText == null)
                return;

            if (!ClientRuntime.IsInitialized || !ClientRuntime.Connection.SupportsDebugNetworkControl)
            {
                statusText.text = "Connection debug unavailable.";
                return;
            }

            if (!ClientRuntime.Connection.IsDebugNetworkBlocked)
            {
                statusText.text = string.Format(
                    "Connection debug ready. {0}: {1:0}s | {2}: {3:0}s | {4}: toggle | {5}: unblock",
                    shortOutageKey,
                    shortOutageSeconds,
                    longOutageKey,
                    longOutageSeconds,
                    toggleBlockKey,
                    unblockKey);
                return;
            }

            var remaining = ClientRuntime.Connection.DebugNetworkBlockRemainingSeconds;
            if (remaining > 0f)
            {
                statusText.text = string.Format(
                    "Connection blocked for debug. Remaining: {0:0}s | {1}: unblock",
                    Mathf.Ceil(remaining),
                    unblockKey);
                return;
            }

            statusText.text = string.Format("Connection blocked for debug until manual unblock. {0}: unblock", unblockKey);
        }
    }
}
