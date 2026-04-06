using SharedLogger = GameShared.Logging.Logger;
using UnityEngine;

namespace PhamNhanOnline.Client.Core.Logging
{
    public static class ClientLog
    {
        private const string BasePrefix = "[Client]";

        public static bool VerboseEnabled { get; set; } = true;

        public static void Info(string message, bool persistToLogger = false)
        {
            if (!VerboseEnabled)
                return;

            var formattedMessage = string.Format("{0} {1}", ResolvePrefix(), message);
            Debug.Log(formattedMessage);
            if (persistToLogger)
                SharedLogger.Info(formattedMessage);
        }

        public static void Warn(string message, bool persistToLogger = false)
        {
            var formattedMessage = string.Format("{0} {1}", ResolvePrefix(), message);
            Debug.LogWarning(formattedMessage);
            if (persistToLogger)
                SharedLogger.Info(string.Format("[WARN] {0}", formattedMessage));
        }

        public static void Error(string message, bool persistToLogger = false)
        {
            var formattedMessage = string.Format("{0} {1}", ResolvePrefix(), message);
            Debug.LogError(formattedMessage);
            if (persistToLogger)
                SharedLogger.Error(formattedMessage);
        }

        private static string ResolvePrefix()
        {
            var username = string.Empty;
            if (PhamNhanOnline.Client.Core.Application.ClientRuntime.IsInitialized &&
                PhamNhanOnline.Client.Core.Application.ClientRuntime.Auth != null)
            {
                username = PhamNhanOnline.Client.Core.Application.ClientRuntime.Auth.LastUsername ?? string.Empty;
            }

            username = username.Trim();
            if (string.IsNullOrWhiteSpace(username))
                return BasePrefix;

            return string.Format("{0}[{1}]", BasePrefix, username);
        }
    }
}
