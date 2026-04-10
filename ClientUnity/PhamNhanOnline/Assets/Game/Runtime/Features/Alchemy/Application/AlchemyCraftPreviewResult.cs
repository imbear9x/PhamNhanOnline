using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct AlchemyCraftPreviewResult
    {
        public AlchemyCraftPreviewResult(
            bool success,
            MessageCode? code,
            AlchemyCraftPreviewModel? preview,
            string failureReason,
            string message)
        {
            Success = success;
            Code = code;
            Preview = preview;
            FailureReason = failureReason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public AlchemyCraftPreviewModel? Preview { get; }
        public string FailureReason { get; }
        public string Message { get; }
    }
}
