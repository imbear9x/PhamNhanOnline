namespace PhamNhanOnline.Client.Network.Session
{
    public struct ConnectionAttemptResult
    {
        public ConnectionAttemptResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }

        public static ConnectionAttemptResult Succeeded(string message)
        {
            return new ConnectionAttemptResult(true, message);
        }

        public static ConnectionAttemptResult Failed(string message)
        {
            return new ConnectionAttemptResult(false, message);
        }
    }
}
