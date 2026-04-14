using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Models;

namespace GameShared.Packets;

[Packet(7)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 1000)]
public partial class CreateCharacterPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterNameInvalid)]
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string? Name { get; set; }

    [ValidationCode(MessageCode.CharacterServerInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ServerId { get; set; }

    [ValidationCode(MessageCode.CharacterModelInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ModelId { get; set; }
}

[Packet(8)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class CreateCharacterResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(13)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetCharacterListPacket : IPacket
{
}

[Packet(14)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetCharacterListResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<CharacterModel>? Characters { get; set; }
}

[Packet(11)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetCharacterDataPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterIdInvalid)]
    [Required]
    public Guid? CharacterId { get; set; }
}

[Packet(12)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetCharacterDataResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(9)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class EnterWorldPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterIdInvalid)]
    [Required]
    public Guid? CharacterId { get; set; }
}

[Packet(10)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class EnterWorldResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(3)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CharacterBaseStatsChangedPacket : IPacket
{
    public CharacterBaseStatsModel? BaseStats { get; set; }
}

[Packet(4)]
[PacketTransport(PacketTransportMode.ReliableSequenced, PacketTrafficClass.StateSync)]
public partial class CharacterCurrentStateChangedPacket : IPacket
{
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(6)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CharacterStateTransitionPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public int? Reason { get; set; }
}

[Packet(28)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class StartCultivationPacket : IPacket
{
}

[Packet(29)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class StartCultivationResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(30)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class StopCultivationPacket : IPacket
{
}

[Packet(31)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class StopCultivationResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(32)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class BreakthroughPacket : IPacket
{
}

[Packet(33)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class BreakthroughResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(34)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class AllocatePotentialPacket : IPacket
{
    [ValidationCode(MessageCode.PotentialTargetInvalid)]
    [Range(1, int.MaxValue)]
    public int? TargetStat { get; set; }

    [ValidationCode(MessageCode.PotentialAllocationInvalid)]
    [Range(1, int.MaxValue)]
    public int? RequestedPotentialAmount { get; set; }
}

[Packet(35)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class AllocatePotentialResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public int? RequestedPotentialAmount { get; set; }
    public int? SpentPotentialAmount { get; set; }
    public int? AppliedUpgradeCount { get; set; }
}

[Packet(36)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CultivationRewardsGrantedPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public long? CultivationGranted { get; set; }
    public int? UnallocatedPotentialGranted { get; set; }
    public bool? ReachedRealmCap { get; set; }
    public bool? IsOfflineSettlement { get; set; }
    public long? RewardedFromUnixMs { get; set; }
    public long? RewardedToUnixMs { get; set; }
}

[Packet(41)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetOwnedMartialArtsPacket : IPacket
{
}

[Packet(42)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetOwnedMartialArtsResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<PlayerMartialArtModel>? MartialArts { get; set; }
    public int? ActiveMartialArtId { get; set; }
    public CultivationPreviewModel? CultivationPreview { get; set; }
}

[Packet(43)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 300)]
public partial class UseMartialArtBookPacket : IPacket
{
    [ValidationCode(MessageCode.MartialArtBookItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerItemId { get; set; }
}

[Packet(44)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class UseMartialArtBookResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public PlayerMartialArtModel? LearnedMartialArt { get; set; }
    public CultivationPreviewModel? CultivationPreview { get; set; }
}

[Packet(45)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 300)]
public partial class SetActiveMartialArtPacket : IPacket
{
    [ValidationCode(MessageCode.ActiveMartialArtInvalid)]
    [Required]
    [Range(0, int.MaxValue)]
    public int? MartialArtId { get; set; }
}

[Packet(46)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class SetActiveMartialArtResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CultivationPreviewModel? CultivationPreview { get; set; }
}

[Packet(64)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetOwnedSkillsPacket : IPacket
{
}

[Packet(65)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetOwnedSkillsResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? MaxLoadoutSlotCount { get; set; }
    public List<PlayerSkillModel>? Skills { get; set; }
    public List<SkillLoadoutSlotModel>? LoadoutSlots { get; set; }
}

[Packet(66)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class SetSkillLoadoutSlotPacket : IPacket
{
    [ValidationCode(MessageCode.SkillLoadoutSlotInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? SlotIndex { get; set; }

    [ValidationCode(MessageCode.PlayerSkillInvalid)]
    [Required]
    [Range(0, long.MaxValue)]
    public long? PlayerSkillId { get; set; }
}

[Packet(67)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class SetSkillLoadoutSlotResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? MaxLoadoutSlotCount { get; set; }
    public List<PlayerSkillModel>? Skills { get; set; }
    public List<SkillLoadoutSlotModel>? LoadoutSlots { get; set; }
}

[Packet(58)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetInventoryPacket : IPacket
{
}

[Packet(59)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetInventoryResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
}

[Packet(60)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class EquipInventoryItemPacket : IPacket
{
    [ValidationCode(MessageCode.InventoryItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerItemId { get; set; }

    [ValidationCode(MessageCode.EquipmentSlotInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? Slot { get; set; }
}

[Packet(61)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class EquipInventoryItemResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(62)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class UnequipInventoryItemPacket : IPacket
{
    [ValidationCode(MessageCode.EquipmentSlotInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? Slot { get; set; }
}

[Packet(63)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class UnequipInventoryItemResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(71)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class DropInventoryItemPacket : IPacket
{
    [ValidationCode(MessageCode.InventoryItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerItemId { get; set; }

    [ValidationCode(MessageCode.InventoryItemQuantityInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }
}

[Packet(72)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class DropInventoryItemResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? PlayerItemId { get; set; }
    public int? Quantity { get; set; }
    public int? RewardId { get; set; }
}

[Packet(73)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 300)]
public partial class ReturnHomeAfterCombatDeathPacket : IPacket
{
}

[Packet(74)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class ReturnHomeAfterCombatDeathResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet(75)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class UseItemPacket : IPacket
{
    [ValidationCode(MessageCode.InventoryItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerItemId { get; set; }

    [ValidationCode(MessageCode.InventoryItemQuantityInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }
}

[Packet(76)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class UseItemResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? PlayerItemId { get; set; }
    public int? RequestedQuantity { get; set; }
    public int? AppliedQuantity { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public PlayerMartialArtModel? LearnedMartialArt { get; set; }
    public CultivationPreviewModel? CultivationPreview { get; set; }
    public int? CooldownMs { get; set; }
    public long? CooldownEndsUnixMs { get; set; }
}

[Packet(77)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetLearnedPillRecipesPacket : IPacket
{
}

[Packet(78)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetLearnedPillRecipesResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<LearnedPillRecipeModel>? Recipes { get; set; }
}

[Packet(79)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class GetPillRecipeDetailPacket : IPacket
{
    [ValidationCode(MessageCode.PillRecipeInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PillRecipeTemplateId { get; set; }
}

[Packet(80)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetPillRecipeDetailResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public string? FailureReason { get; set; }
    public PillRecipeDetailModel? Recipe { get; set; }
}

[Packet(81)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class PreviewCraftPillPacket : IPacket
{
    [ValidationCode(MessageCode.PillRecipeInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PillRecipeTemplateId { get; set; }

    [ValidationCode(MessageCode.InventoryItemQuantityInvalid)]
    [Range(1, int.MaxValue)]
    public int? RequestedCraftCount { get; set; }

    public List<long>? SelectedPlayerItemIds { get; set; }
    public List<AlchemyOptionalInputSelectionModel>? SelectedOptionalInputs { get; set; }
}

[Packet(82)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class PreviewCraftPillResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public string? FailureReason { get; set; }
    public AlchemyCraftPreviewModel? Preview { get; set; }
}

[Packet(83)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class CraftPillPacket : IPacket
{
    [ValidationCode(MessageCode.PillRecipeInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? PillRecipeTemplateId { get; set; }

    [ValidationCode(MessageCode.InventoryItemQuantityInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? RequestedCraftCount { get; set; }

    public List<long>? SelectedPlayerItemIds { get; set; }
    public List<AlchemyOptionalInputSelectionModel>? SelectedOptionalInputs { get; set; }
}

[Packet(84)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class CraftPillResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public string? FailureReason { get; set; }
    public int? PillRecipeTemplateId { get; set; }
    public PracticeSessionModel? Session { get; set; }
    public List<AlchemyConsumedItemModel>? ConsumedItems { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
    public PillRecipeDetailModel? Recipe { get; set; }
}

[Packet(85)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class GetAlchemyPracticeStatusPacket : IPacket
{
}

[Packet(86)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetAlchemyPracticeStatusResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public AlchemyPracticeStatusModel? Status { get; set; }
}

[Packet(87)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class PausePracticePacket : IPacket
{
    [ValidationCode(MessageCode.PracticeInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PracticeSessionId { get; set; }
}

[Packet(88)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class PausePracticeResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public PracticeSessionModel? Session { get; set; }
}

[Packet(89)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class ResumePracticePacket : IPacket
{
    [ValidationCode(MessageCode.PracticeInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PracticeSessionId { get; set; }
}

[Packet(90)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class ResumePracticeResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public PracticeSessionModel? Session { get; set; }
}

[Packet(91)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class CancelPracticePacket : IPacket
{
    [ValidationCode(MessageCode.PracticeInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PracticeSessionId { get; set; }
}

[Packet(92)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class CancelPracticeResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
}

[Packet(93)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class AcknowledgePracticeResultPacket : IPacket
{
    [ValidationCode(MessageCode.PracticeInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PracticeSessionId { get; set; }
}

[Packet(94)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class AcknowledgePracticeResultResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
}

[Packet(95)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class PracticeCompletedPacket : IPacket
{
    public PracticeSessionModel? Session { get; set; }
    public PracticeCompletionResultModel? Result { get; set; }
}

[Packet(96)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class PlayerNotificationReceivedPacket : IPacket
{
    public PlayerNotificationModel? Notification { get; set; }
}

[Packet(97)]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 150)]
public partial class AcknowledgePlayerNotificationPacket : IPacket
{
    [ValidationCode(MessageCode.NotificationInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? NotificationId { get; set; }
}

[Packet(98)]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class AcknowledgePlayerNotificationResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public long? NotificationId { get; set; }
}
