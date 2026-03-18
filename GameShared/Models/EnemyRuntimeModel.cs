using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct EnemyRuntimeModel
{
    public int RuntimeId;
    public int EnemyTemplateId;
    public string Code;
    public string Name;
    public int Kind;
    public int RuntimeState;
    public int CurrentHp;
    public int MaxHp;
    public float PosX;
    public float PosY;
    public int SpawnGroupId;
}
