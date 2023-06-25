
using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace NonSucking.Framework.Extension.Database.InMemory;
public class InMemoryConfigurator : IDatabaseConfigurator
{
    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseInMemoryDatabase("Empty");
            base.OnConfiguring(optionsBuilder);
        }
    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }

    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseInMemoryDatabase(connectionString);
    }
}
