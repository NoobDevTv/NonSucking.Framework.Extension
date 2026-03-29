using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace NonSucking.Framework.Extension.EntityFrameworkCore.Tests;


public class SqLIteTests
{
    [SetUp]
    public void Setup()
    {
        using var ctx = new InMemoryDbContext();
        ctx.Database.Migrate();
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [DbContext(typeof(InMemoryDbContext)), Migration("10000")]
    public class InitialCreate : Migration, IAutoMigrationTypeProvider
    {
        public IReadOnlyList<Type> GetEntityTypes() => [typeof(TestEntity)];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetUpgradeOperations(this);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetDowngradeOperations(this);
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

        }
    }

    [DbContext(typeof(InMemoryDbContext)), Migration("10001")]
    public class SecondCreate : Migration, IAutoMigrationTypeProvider
    {
        public IReadOnlyList<Type> GetEntityTypes() => [typeof(TestEntity)];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetUpgradeOperations(this);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetDowngradeOperations(this);
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int Age { get; set; }
            public int Balance { get; set; }
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class InMemoryDbContext : BaseTestDbContext
    {
        public DbSet<TestEntity> Tests => Set<TestEntity>();

        public InMemoryDbContext() : base(new SqliteConnectionStringBuilder()
        {
            Mode = SqliteOpenMode.ReadWriteCreate,
            DataSource = "test.db"

        }.ToString(), "NonSucking.Framework.Extension.Database.Sqlite.dll")
        {
        }
    }
}