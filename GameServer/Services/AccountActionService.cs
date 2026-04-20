using GameServer.Exceptions;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class AccountActionService
{
    private readonly AccountService _accountService;

    public AccountActionService(AccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<AccountActionResult> RegisterAsync(
        string loginId,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _accountService.RegisterWithPasswordAsync(loginId, password, cancellationToken);
            return AccountActionResult.SuccessResult();
        }
        catch (GameException ex)
        {
            return AccountActionResult.Failure(ex.Code);
        }
    }

    public async Task<AccountLoginActionResult> LoginAsync(
        string loginId,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _accountService.LoginWithPasswordAsync(loginId, password, cancellationToken);
            return AccountLoginActionResult.SuccessResult(result);
        }
        catch (GameException ex)
        {
            return AccountLoginActionResult.Failure(ex.Code);
        }
    }

    public async Task<AccountActionResult> ChangePasswordAsync(
        Guid accountId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _accountService.ChangePasswordAsync(accountId, oldPassword, newPassword, cancellationToken);
            return AccountActionResult.SuccessResult();
        }
        catch (GameException ex)
        {
            return AccountActionResult.Failure(ex.Code);
        }
    }
}

public readonly record struct AccountActionResult(
    bool Success,
    MessageCode Code)
{
    public static AccountActionResult SuccessResult() => new(true, MessageCode.None);

    public static AccountActionResult Failure(MessageCode code) => new(false, code);
}

public readonly record struct AccountLoginActionResult(
    bool Success,
    MessageCode Code,
    LoginResultDto? Login)
{
    public static AccountLoginActionResult SuccessResult(LoginResultDto login) =>
        new(true, MessageCode.None, login);

    public static AccountLoginActionResult Failure(MessageCode code) =>
        new(false, code, null);
}
