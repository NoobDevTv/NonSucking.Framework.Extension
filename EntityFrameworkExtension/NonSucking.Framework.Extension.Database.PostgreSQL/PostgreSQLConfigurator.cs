
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace NonSucking.Framework.Extension.Database.PostrgeSQL;


public class PostgreSQLConfigurator : IDatabaseConfigurator
{
    public string Name => "PostgreSQL";
    public class MigrationContext : MigrationDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder
                .UseNpgsql();
            base.OnConfiguring(optionsBuilder);
        }
    }

    public IAutoMigrationContextBuilder GetEmptyForMigration()
    {
        return new MigrationContext();
    }
    public DbContextOptionsBuilder OnConfiguring(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder
                .UseNpgsql(connectionString)
                .ReplaceService<IHistoryRepository, NonLockingNpgsqlHistoryRepository>();
    }


#pragma warning disable EF1001
    private class NonLockingNpgsqlHistoryRepository(HistoryRepositoryDependencies dependencies)
        : NpgsqlHistoryRepository(dependencies)
    {
        public override IMigrationsDatabaseLock AcquireDatabaseLock()
            => new NoopMigrationsDatabaseLock(this);

        public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IMigrationsDatabaseLock>(new NoopMigrationsDatabaseLock(this));

        class NoopMigrationsDatabaseLock(NpgsqlHistoryRepository historyRepository) : IMigrationsDatabaseLock
        {
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public IHistoryRepository HistoryRepository => historyRepository;
        }
    }
#pragma warning restore EF1001
}
