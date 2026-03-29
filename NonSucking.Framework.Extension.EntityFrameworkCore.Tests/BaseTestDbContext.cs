using Microsoft.EntityFrameworkCore;

namespace NonSucking.Framework.Extension.EntityFrameworkCore.Tests;

public class BaseTestDbContext : DatabaseContext
{
    private readonly string connectionString;
    private readonly string dbPluginPath;

    public BaseTestDbContext(string connectionString, string dbPluginPath)
    {
        this.connectionString = connectionString;
        this.dbPluginPath = dbPluginPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseFactory.Initialize(dbPluginPath);
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            item.OnConfiguring(optionsBuilder, connectionString).UseLazyLoadingProxies();
        }

        base.OnConfiguring(optionsBuilder);
    }
}