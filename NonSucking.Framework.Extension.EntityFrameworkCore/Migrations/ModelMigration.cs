using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

using System.Reflection;
using System.Runtime.Loader;

namespace NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

public static class ModelMigration
{
    public static void BuildCurrent(this ModelBuilder modelBuilder)
    {
        foreach (var type in AssemblyLoadContext
                                    .Default
                                    .Assemblies
                                    .SelectMany(x => x.GetTypes())
                                    .Where(type => !type.IsAbstract && !type.IsInterface && type.IsAssignableTo(typeof(IEntity)))
                                    .Where(type => type.GetCustomAttribute<HistoryAttribute>() is null)
        )
        {
            if (modelBuilder.Model.FindEntityType(type) is null)
                _ = modelBuilder.Model.AddEntityType(type);
        }
    }

    public static void BuildVersion(this ModelBuilder modelBuilder, string version)
    {
        static bool HasCorrectVersion(Type type, string version)
        {
            var attribute = type.GetCustomAttribute<HistoryAttribute>();

            return attribute is not null && attribute.Version == version;
        }

        foreach (var type in AssemblyLoadContext
                                    .Default
                                    .Assemblies
                                    .SelectMany(x => x.GetTypes())
                                    .Where(type => !type.IsAbstract && !type.IsInterface && type.IsAssignableTo(typeof(IEntity)))
                                    .Where(type => HasCorrectVersion(type, version))
        )
        {
            if (modelBuilder.Model.FindEntityType(type) is null)
                _ = modelBuilder.Model.AddEntityType(type);
        }
    }

    public static void BuildVersion(this ModelBuilder modelBuilder, IAutoMigrationTypeProvider typeProvider)
    {
        foreach (var type in typeProvider.GetEntityTypes())
        {
            if (modelBuilder.Model.FindEntityType(type) is null)
                _ = modelBuilder.Model.AddEntityType(type);
        }
    }

    public static void SetUpgradeOperations(this MigrationBuilder migrationBuilder, Migration migration)
    {
        IAutoMigrationContextBuilder providerContextBuilder;
        IModel target, source;
        GetMigrationClasses(migration, out providerContextBuilder, out target, out source);

        var diff = providerContextBuilder.GenerateDiff(source, target);
        migrationBuilder.Operations.AddRange(diff);
    }
    public static void SetDowngradeOperations(this MigrationBuilder migrationBuilder, Migration migration)
    {
        IAutoMigrationContextBuilder providerContextBuilder;
        IModel target, source;
        GetMigrationClasses(migration, out providerContextBuilder, out target, out source);

        var diff = providerContextBuilder.GenerateDiff(target, source);
        migrationBuilder.Operations.AddRange(diff);
    }

    private static void GetMigrationClasses(Migration migration, out IAutoMigrationContextBuilder providerContextBuilder, out IModel target, out IModel? source)
    {
        var migrationType = migration.GetType();
        var contextAttribute = migrationType.GetCustomAttribute<DbContextAttribute>() ?? throw new ArgumentNullException();
        var builderFromConfig = DatabaseFactory.DatabaseConfigurators.FirstOrDefault()?.GetEmptyForMigration();

        providerContextBuilder = builderFromConfig ?? (IAutoMigrationContextBuilder)Activator.CreateInstance(contextAttribute.ContextType)!;

        var currentContext = (IAutoMigrationContext)Activator.CreateInstance(contextAttribute.ContextType)!;
        var targetBuilder = providerContextBuilder.CreateBuilder();

        if (migration is IAutoMigrationTypeProvider autoTypeProvider)
        {
            targetBuilder.BuildVersion(autoTypeProvider);
        }
        else
        {
            var idAttribute
                = migrationType.GetCustomAttribute<MigrationAttribute>()
                    ?? throw new ArgumentNullException();

            targetBuilder.BuildVersion(idAttribute.Id);
        }

        target = providerContextBuilder.FinalizeModel((IModel)targetBuilder.Model);
        source = null;

        if (currentContext.FindLastMigration(contextAttribute.ContextType, out var lastMigration, out var lastMigrationId))
        {
            var sourceBuilder = providerContextBuilder.CreateBuilder();

            if (lastMigration is IAutoMigrationTypeProvider lastTypeProvider)
            {
                sourceBuilder.BuildVersion(lastTypeProvider);
            }
            else
            {
                sourceBuilder.BuildVersion(lastMigrationId);
            }

            source = providerContextBuilder.FinalizeModel((IModel)sourceBuilder.Model);
        }
    }
}
