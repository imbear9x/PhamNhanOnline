using GameShared.Enums;

namespace GameShared.Models;

public sealed class CombatTargetModel
{
    public CombatTargetKind? Kind { get; set; }
    public Guid? CharacterId { get; set; }
    public int? RuntimeId { get; set; }
    public float? GroundPosX { get; set; }
    public float? GroundPosY { get; set; }
}
