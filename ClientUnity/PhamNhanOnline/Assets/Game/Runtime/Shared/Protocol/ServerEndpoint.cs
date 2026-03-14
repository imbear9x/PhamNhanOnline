namespace PhamNhanOnline.Client.Shared.Protocol
{
    public struct ServerEndpoint
    {
        public ServerEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }
    }
}
