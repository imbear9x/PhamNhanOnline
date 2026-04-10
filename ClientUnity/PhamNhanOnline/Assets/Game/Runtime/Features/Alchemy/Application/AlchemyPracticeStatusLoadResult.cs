using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct AlchemyPracticeStatusLoadResult
    {
        public AlchemyPracticeStatusLoadResult(
            bool success,
            MessageCode? code,
            AlchemyPracticeStatusModel? status,
            string message)
        {
            Success = success;
            Code = code;
            Status = status;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public AlchemyPracticeStatusModel? Status { get; }
        public string Message { get; }
    }
}
