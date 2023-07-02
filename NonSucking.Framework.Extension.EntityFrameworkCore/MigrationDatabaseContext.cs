
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace NonSucking.Framework.Extension.EntityFrameworkCore
{
    public abstract class MigrationDatabaseContext : DbContext, IAutoMigrationContextBuilder
    {
        public ModelBuilder CreateBuilder()
        {
            var dependencies = Database.GetService<ModelDependencies>();
            var setBuilder = Database.GetService<IConventionSetBuilder>();
            var serviceProvider = Database.GetService<IServiceProvider>();
            var modelConfigurationBuilder =
                new ModelConfigurationBuilder(setBuilder.CreateConventionSet(), serviceProvider);

            return modelConfigurationBuilder.CreateModelBuilder(dependencies);
        }

        public IModel FinalizeModel(IModel model)
        {
            var initializer = Database.GetService<IModelRuntimeInitializer>();
            return initializer.Initialize(model);
        }

        public IReadOnlyList<MigrationOperation> GenerateDiff(IModel? source, IModel? target)
        {
            var sourceModel = source?.GetRelationalModel();
            var targetModel = target?.GetRelationalModel();

            var differ = Database.GetService<IMigrationsModelDiffer>();
            return differ.GetDifferences(sourceModel, targetModel);
        }
    }
}
