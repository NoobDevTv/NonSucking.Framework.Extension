
using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace NonSucking.Framework.Extension.Database.MSSQL;
public class MSSQLConfigurator : IDatabaseConfigurator
{
    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlServer();
            base.OnConfiguring(optionsBuilder);
        }

    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }

    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseSqlServer(connectionString);
    }
}
