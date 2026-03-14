using UnityEngine;

namespace PhamNhanOnline.Client.Core.Logging
{
    public static class ClientLog
    {
        private const string Prefix = "[Client]";

        public static bool VerboseEnabled { get; set; } = true;

        public static void Info(string message)
        {
            if (!VerboseEnabled)
                return;

            Debug.Log(string.Format("{0} {1}", Prefix, message));
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(string.Format("{0} {1}", Prefix, message));
        }

        public static void Error(string message)
        {
            Debug.LogError(string.Format("{0} {1}", Prefix, message));
        }
    }
}
