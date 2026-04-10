using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct PracticeSessionActionResult
    {
        public PracticeSessionActionResult(
            bool success,
            MessageCode? code,
            PracticeSessionModel? session,
            string message)
        {
            Success = success;
            Code = code;
            Session = session;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public PracticeSessionModel? Session { get; }
        public string Message { get; }
    }
}
