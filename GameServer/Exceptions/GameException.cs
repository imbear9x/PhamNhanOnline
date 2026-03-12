using GameShared.Messages;

namespace GameServer.Exceptions;

public sealed class GameException : Exception
{
    public MessageCode Code { get; }

    public GameException(MessageCode code)
        : base(code.ToString())
    {
        Code = code;
    }

    public GameException(MessageCode code, string? message)
        : base(message ?? code.ToString())
    {
        Code = code;
    }

    public GameException(MessageCode code, string? message, Exception? innerException)
        : base(message ?? code.ToString(), innerException)
    {
        Code = code;
    }
}
