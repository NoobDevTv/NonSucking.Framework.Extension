using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace NonSucking.Framework.Extension.EntityFrameworkCore
{
    public interface IDatabaseConfigurator
    {
        string Name { get; }
        DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString);
        IAutoMigrationContextBuilder GetEmptyForMigration();
    }
}
