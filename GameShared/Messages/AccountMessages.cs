// NOTE: Mirror transport integration will be implemented later.
// This file is guarded to avoid build breaks when Mirror runtime APIs are unavailable.
#if MIRROR
using GameShared.Models;
using Mirror;

namespace GameShared.Messages;

public struct RegisterRequest : NetworkMessage
{
    public string Username;
    public string Password;
}

public struct RegisterResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public AccountModel Account;
}

public struct LoginRequest : NetworkMessage
{
    public string Username;
    public string Password;
}

public struct LoginResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public AccountModel Account;
}

public struct ProviderLoginRequest : NetworkMessage
{
    // e.g. "google"
    public string Provider;
    public string ProviderUserId;
}

public struct ProviderLoginResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public AccountModel Account;
}

public struct LinkCredentialRequest : NetworkMessage
{
    public Guid AccountId;
    // e.g. "google", "phone"
    public string Provider;
    public string ProviderUserId;
}

public struct LinkCredentialResponse : NetworkMessage
{
    public bool Success;
    public string Error;
}

#endif
