
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace NonSucking.Framework.Extension.EntityFrameworkCore
{
    public abstract class DatabaseContext : DbContext, IAutoMigrationContext
    {
        public bool EnableUseLazyLoading { get; set; } = true;
        public string AssemblyRootName { get; set; }
        public bool AddAllEntities { get; set; }

        protected DatabaseContext()
        {
        }

        protected DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        protected DatabaseContext(bool enableUseLazyLoading, string assemblyRootName, bool addAllEntities)
        {
            EnableUseLazyLoading = enableUseLazyLoading;
            AssemblyRootName = assemblyRootName;
            AddAllEntities = addAllEntities;
        }

        public bool FindLastMigration(Type contextAttributeType, [MaybeNullWhen(false)] out Migration migration, [MaybeNullWhen(false)] out string id)
        {
            migration = null;
            id = "";
            TypeInfo? migrationType = null;
            var assembly = Database.GetService<IMigrationsAssembly>();
            var migrationsInDb = Database.GetAppliedMigrations().OrderByDescending(id => id);
            foreach (var item in migrationsInDb)
            {
                if (assembly.Migrations.TryGetValue(item, out migrationType))
                {
                    id = item;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(id) || migrationType is null)
                return false;

            migration = (Migration)Activator.CreateInstance(migrationType)!;

            return true;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies(EnableUseLazyLoading);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (AddAllEntities)
            {
                modelBuilder.BuildCurrent(AssemblyRootName);
            }

            base.OnModelCreating(modelBuilder);
        }
        /// <summary>
        /// Migrates with a transaction
        ///     <para>
        ///         Applies any pending migrations for the context to the database. Will create the database
        ///         if it does not already exist.
        ///     </para>
        ///     <para>
        ///         Note that this API is mutually exclusive with <see cref="DatabaseFacade.EnsureCreated" />. EnsureCreated does not use migrations
        ///         to create the database and therefore the database that is created cannot be later updated using migrations.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information.
        /// </remarks>
        /// <param name="databaseFacade">The <see cref="DatabaseFacade" /> for the context.</param>
        public void Migrate()
        {
            Database.Migrate();
        }
    }
}
