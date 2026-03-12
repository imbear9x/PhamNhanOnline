using System.Security.Cryptography;
using System.Text;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameShared.Messages;
using LinqToDB;

namespace GameServer.Services;

public sealed class AccountService
{
    public const string ProviderPassword = "password";
    public const string ProviderGoogle = "google";
    public const string ProviderPhone = "phone";

    private readonly GameDb _db;
    private readonly AccountRepository _accounts;
    private readonly AccountCredentialRepository _credentials;

    public AccountService(GameDb db, AccountRepository accounts, AccountCredentialRepository credentials)
    {
        _db = db;
        _accounts = accounts;
        _credentials = credentials;
    }

    public async Task<AccountDto> RegisterWithPasswordAsync(
        string loginId,
        string password,
        CancellationToken cancellationToken = default)
    {
        loginId = NormalizeLoginId(loginId);
        password = password ?? string.Empty;

        var existing = await _credentials.GetByProviderUserIdAsync(ProviderPassword, loginId, cancellationToken);
        if (existing is not null)
            throw new GameException(MessageCode.LoginAlreadyExists);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            Status = 1,
        };

        var credential = new AccountCredential
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Provider = ProviderPassword,
            ProviderUserId = loginId,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow,
        };

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _accounts.CreateAsync(account, cancellationToken);
        await _credentials.CreateAsync(credential, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return AccountDto.FromEntity(account);
    }

    public async Task<LoginResultDto> LoginWithPasswordAsync(
        string loginId,
        string password,
        CancellationToken cancellationToken = default)
    {
        loginId = NormalizeLoginId(loginId);
        password = password ?? string.Empty;

        var cred = await _credentials.GetByProviderUserIdAsync(ProviderPassword, loginId, cancellationToken);
        if (cred?.PasswordHash is null)
            throw new GameException(MessageCode.InvalidCredentials);

        if (!VerifyPassword(password, cred.PasswordHash))
            throw new GameException(MessageCode.InvalidCredentials);

        var account = await _accounts.GetByIdAsync(cred.AccountId, cancellationToken);
        if (account is null)
            throw new GameException(MessageCode.AccountNotFound);

        account.LastLogin = DateTime.UtcNow;
        await _accounts.UpdateAsync(account, cancellationToken);

        return new LoginResultDto(AccountDto.FromEntity(account), Map(cred));
    }

    public async Task<LoginResultDto> LoginWithGoogleAsync(
        string googleUserId,
        CancellationToken cancellationToken = default)
    {
        googleUserId = NormalizeProviderUserId(googleUserId);

        var cred = await _credentials.GetByProviderUserIdAsync(ProviderGoogle, googleUserId, cancellationToken);
        if (cred is not null)
        {
            var existingAccount = await _accounts.GetByIdAsync(cred.AccountId, cancellationToken);
            if (existingAccount is null)
                throw new GameException(MessageCode.AccountNotFound);

            existingAccount.LastLogin = DateTime.UtcNow;
            await _accounts.UpdateAsync(existingAccount, cancellationToken);
            return new LoginResultDto(AccountDto.FromEntity(existingAccount), Map(cred));
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            Status = 1,
        };

        cred = new AccountCredential
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Provider = ProviderGoogle,
            ProviderUserId = googleUserId,
            PasswordHash = null,
            CreatedAt = DateTime.UtcNow,
        };

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _accounts.CreateAsync(account, cancellationToken);
        await _credentials.CreateAsync(cred, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new LoginResultDto(AccountDto.FromEntity(account), Map(cred));
    }

    public async Task<CredentialDto> LinkGoogleAsync(
        Guid accountId,
        string googleUserId,
        CancellationToken cancellationToken = default)
    {
        googleUserId = NormalizeProviderUserId(googleUserId);

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new GameException(MessageCode.AccountNotFound);

        var alreadyLinked = await _credentials.GetByAccountAndProviderAsync(accountId, ProviderGoogle, cancellationToken);
        if (alreadyLinked is not null)
            throw new GameException(MessageCode.GoogleCredentialAlreadyLinked);

        var usedByOther = await _credentials.GetByProviderUserIdAsync(ProviderGoogle, googleUserId, cancellationToken);
        if (usedByOther is not null)
            throw new GameException(MessageCode.GoogleCredentialLinkedToOtherAccount);

        var cred = new AccountCredential
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Provider = ProviderGoogle,
            ProviderUserId = googleUserId,
            PasswordHash = null,
            CreatedAt = DateTime.UtcNow,
        };

        await _credentials.CreateAsync(cred, cancellationToken);
        return Map(cred);
    }

    public async Task<CredentialDto> LinkPhoneAsync(
        Guid accountId,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        phoneNumber = NormalizeProviderUserId(phoneNumber);

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new GameException(MessageCode.AccountNotFound);

        var alreadyLinked = await _credentials.GetByAccountAndProviderAsync(accountId, ProviderPhone, cancellationToken);
        if (alreadyLinked is not null)
            throw new GameException(MessageCode.PhoneCredentialAlreadyLinked);

        var usedByOther = await _credentials.GetByProviderUserIdAsync(ProviderPhone, phoneNumber, cancellationToken);
        if (usedByOther is not null)
            throw new GameException(MessageCode.PhoneCredentialLinkedToOtherAccount);

        var cred = new AccountCredential
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Provider = ProviderPhone,
            ProviderUserId = phoneNumber,
            PasswordHash = null,
            CreatedAt = DateTime.UtcNow,
        };

        await _credentials.CreateAsync(cred, cancellationToken);
        return Map(cred);
    }

    public async Task<CredentialDto> ChangePasswordAsync(
        Guid accountId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        oldPassword = oldPassword ?? string.Empty;
        newPassword = newPassword ?? string.Empty;

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new GameException(MessageCode.AccountNotFound);

        var cred = await _credentials.GetByAccountAndProviderAsync(accountId, ProviderPassword, cancellationToken);
        if (cred?.PasswordHash is null)
            throw new GameException(MessageCode.PasswordCredentialNotFound);

        if (!VerifyPassword(oldPassword, cred.PasswordHash))
            throw new GameException(MessageCode.InvalidCredentials);

        cred.PasswordHash = HashPassword(newPassword);
        await _credentials.UpdateAsync(cred, cancellationToken);
        return Map(cred);
    }

    public async Task<CredentialDto> ChangeCredentialAsync(
        Guid accountId,
        string provider,
        string newProviderUserId,
        CancellationToken cancellationToken = default)
    {
        provider = NormalizeProvider(provider);
        if (provider == ProviderPassword)
            throw new GameException(MessageCode.UseChangePasswordForPasswordProvider);

        newProviderUserId = NormalizeProviderUserId(newProviderUserId);

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new GameException(MessageCode.AccountNotFound);

        var cred = await _credentials.GetByAccountAndProviderAsync(accountId, provider, cancellationToken);
        if (cred is null)
            throw new GameException(MessageCode.CredentialNotFound);

        var usedByOther = await _credentials.GetByProviderUserIdAsync(provider, newProviderUserId, cancellationToken);
        if (usedByOther is not null && usedByOther.AccountId != accountId)
            throw new GameException(MessageCode.CredentialAlreadyLinkedToOtherAccount);

        cred.ProviderUserId = newProviderUserId;
        await _credentials.UpdateAsync(cred, cancellationToken);
        return Map(cred);
    }

    private static CredentialDto Map(AccountCredential c) =>
        new(c.Id, c.AccountId, c.Provider, c.ProviderUserId, c.CreatedAt);

    private static string NormalizeLoginId(string loginId)
    {
        return (loginId ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeProvider(string provider)
    {
        provider = (provider ?? string.Empty).Trim().ToLowerInvariant();
        if (provider.Length == 0)
            throw new GameException(MessageCode.ProviderRequired);
        if (provider is not (ProviderGoogle or ProviderPhone or ProviderPassword))
            throw new GameException(MessageCode.UnsupportedProvider);
        return provider;
    }

    private static string NormalizeProviderUserId(string providerUserId)
    {
        providerUserId = (providerUserId ?? string.Empty).Trim();
        if (providerUserId.Length == 0)
            throw new GameException(MessageCode.ProviderUserIdRequired);
        return providerUserId;
    }

    // Format: PBKDF2-SHA256$<iterations>$<salt_b64>$<hash_b64>
    private static string HashPassword(string password)
    {
        const int iterations = 200_000;
        Span<byte> salt = stackalloc byte[16];
        RandomNumberGenerator.Fill(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2-SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4)
            return false;
        if (!string.Equals(parts[0], "PBKDF2-SHA256", StringComparison.Ordinal))
            return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed record CredentialDto(Guid Id, Guid AccountId, string Provider, string ProviderUserId, DateTime? CreatedAt);

public sealed record LoginResultDto(AccountDto Account, CredentialDto Credential);
