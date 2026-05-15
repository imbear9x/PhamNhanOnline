using LinqToDB;
using LinqToDB.Data;

namespace GameServer.Entities;

public partial class PhamnhanOnlineDb
{
    public ITable<PlayerBagEntity> PlayerBags => this.GetTable<PlayerBagEntity>();
    public ITable<BagGradeConfigEntity> BagGradeConfigs => this.GetTable<BagGradeConfigEntity>();
}