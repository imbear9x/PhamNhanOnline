using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("enemy_template_skills")]
public sealed class EnemyTemplateSkillEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("enemy_template_id"), NotNull] public int EnemyTemplateId { get; set; }
    [Column("skill_id"), NotNull] public int SkillId { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
}
