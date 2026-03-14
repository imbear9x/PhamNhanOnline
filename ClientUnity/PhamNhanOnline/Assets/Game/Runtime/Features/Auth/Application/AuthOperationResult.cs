using GameShared.Messages;

namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public struct AuthOperationResult
    {
        public AuthOperationResult(bool success, MessageCode? code, string message)
        {
            Success = success;
            Code = code;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public string Message { get; }

        public static AuthOperationResult From(bool success, MessageCode? code, string message)
        {
            return new AuthOperationResult(success, code, message);
        }
    }
}
