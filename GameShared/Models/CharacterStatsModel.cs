using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterStatsModel
{
    public Guid CharacterId;
    public int RealmTemplateId;
    public long Cultivation;

    public int Health;
    public int Mana;
    public int Physique;
    public int Attack;
    public int Speed;
    public int SpiritualSense;
    public double Fortune;
    public int Potential;
}
