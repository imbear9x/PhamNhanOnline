namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public struct LoginFlowResult
    {
        public LoginFlowResult(bool success, bool requiresCharacterCreation, string message)
        {
            Success = success;
            RequiresCharacterCreation = requiresCharacterCreation;
            Message = message;
        }

        public bool Success { get; }
        public bool RequiresCharacterCreation { get; }
        public string Message { get; }

        public static LoginFlowResult Succeeded(string message)
        {
            return new LoginFlowResult(true, false, message);
        }

        public static LoginFlowResult RequiresCharacterCreationResult(string message)
        {
            return new LoginFlowResult(false, true, message);
        }

        public static LoginFlowResult Failed(string message)
        {
            return new LoginFlowResult(false, false, message);
        }
    }
}
