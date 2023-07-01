
using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace NonSucking.Framework.Extension.Database.Sqlite;
public class SqLiteConfigurator : IDatabaseConfigurator
{
    public string Name => "SQLite";

    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlite();
            base.OnConfiguring(optionsBuilder);
        }

    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }
    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseSqlite(connectionString);
    }
}
