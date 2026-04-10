using LinqToDB;

namespace GameServer.Entities;

public partial class PhamnhanOnlineDb
{
    public ITable<PlayerNotificationEntity> PlayerNotifications => this.GetTable<PlayerNotificationEntity>();
}
