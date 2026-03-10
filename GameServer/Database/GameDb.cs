using LinqToDB;
using LinqToDB.Data;

public class GameDb : DataConnection
{
    public GameDb(string connection)
        : base(new DataOptions()
            .UsePostgreSQL(connection))
    {
    }
}