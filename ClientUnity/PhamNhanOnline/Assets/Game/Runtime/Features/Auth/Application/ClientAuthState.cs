using System;

namespace PhamNhanOnline.Client.Features.Auth.Application
{
    public sealed class ClientAuthState
    {
        public string LastUsername { get; private set; } = string.Empty;
        public Guid? AccountId { get; private set; }
        public string ResumeToken { get; private set; } = string.Empty;
        public bool IsAuthenticated { get { return AccountId.HasValue; } }

        public void RememberUsername(string username)
        {
            LastUsername = username != null ? username.Trim() : string.Empty;
        }

        public void ApplyAuthenticatedSession(Guid accountId, string resumeToken)
        {
            AccountId = accountId;
            ResumeToken = resumeToken ?? string.Empty;
        }

        public void Clear()
        {
            AccountId = null;
            ResumeToken = string.Empty;
        }
    }
}
