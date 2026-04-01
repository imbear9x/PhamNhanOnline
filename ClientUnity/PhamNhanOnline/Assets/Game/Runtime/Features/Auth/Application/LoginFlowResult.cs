namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public enum LoginFlowFailureKind
    {
        None = 0,
        ConnectionUnavailable = 1,
        AuthenticationFailed = 2,
        CharacterListFailed = 3,
        EnterWorldFailed = 4
    }

    public struct LoginFlowResult
    {
        public LoginFlowResult(
            bool success,
            bool requiresCharacterCreation,
            string message,
            LoginFlowFailureKind failureKind)
        {
            Success = success;
            RequiresCharacterCreation = requiresCharacterCreation;
            Message = message;
            FailureKind = failureKind;
        }

        public bool Success { get; }
        public bool RequiresCharacterCreation { get; }
        public string Message { get; }
        public LoginFlowFailureKind FailureKind { get; }
        public bool IsConnectionFailure { get { return FailureKind == LoginFlowFailureKind.ConnectionUnavailable; } }

        public static LoginFlowResult Succeeded(string message)
        {
            return new LoginFlowResult(true, false, message, LoginFlowFailureKind.None);
        }

        public static LoginFlowResult RequiresCharacterCreationResult(string message)
        {
            return new LoginFlowResult(false, true, message, LoginFlowFailureKind.None);
        }

        public static LoginFlowResult Failed(string message, LoginFlowFailureKind failureKind = LoginFlowFailureKind.None)
        {
            return new LoginFlowResult(false, false, message, failureKind);
        }
    }
}
