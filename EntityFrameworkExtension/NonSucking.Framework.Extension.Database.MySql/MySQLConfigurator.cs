
using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace NonSucking.Framework.Extension.Database.MySql;
public class MySQLConfigurator : IDatabaseConfigurator
{
    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseMySql("server=none;userid=none;password=none;database=none", ServerVersion.Create(10, 9, 3, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MariaDb));
            base.OnConfiguring(optionsBuilder);
        }

    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }

    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
}

