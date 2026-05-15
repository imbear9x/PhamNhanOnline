using GameShared.Messages;

namespace GameServer.DTO;

public sealed record BagStateDto(
    int Grade,
    int UsedSlots,
    int TotalSlots,
    string DisplayName);

public sealed record ItemGrantRequest(
    int ItemTemplateId,
    int Quantity,
    bool IsBound,
    DateTime? ExpireAtUtc = null);

public sealed record InventoryCapacityCheckResult(
    bool CanFit,
    int UsedSlots,
    int TotalSlots,
    int AdditionalSlotsNeeded);

public sealed record BagUpgradeResult(
    bool Success,
    MessageCode Code,
    BagStateDto? BagState,
    int RemainingLinhThach,
    string? FailureReason = null)
{
    public static BagUpgradeResult Failed(MessageCode code, string? failureReason = null) =>
        new(false, code, null, 0, failureReason);

    public static BagUpgradeResult Succeeded(BagStateDto bagState, int remainingLinhThach) =>
        new(true, MessageCode.None, bagState, remainingLinhThach);
}