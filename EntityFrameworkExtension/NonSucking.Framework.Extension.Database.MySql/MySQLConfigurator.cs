
using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace NonSucking.Framework.Extension.Database.MySql;
public class MySQLConfigurator : IDatabaseConfigurator
{
    public string Name => "MySQL";
    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseMySQL("server=none;userid=none;password=none;database=none");
            base.OnConfiguring(optionsBuilder);
        }

    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }

    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseMySQL(connectionString);
    }
}

