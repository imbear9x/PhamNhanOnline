using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Packets
{
    public enum PacketType : byte
    {
        Unknown = 0,
        Register = 1,
        RegisterResult = 2,
        Login = 3,
        LoginResult = 4,
        Reconnect = 5,
        ReconnectResult = 6
    }
}
