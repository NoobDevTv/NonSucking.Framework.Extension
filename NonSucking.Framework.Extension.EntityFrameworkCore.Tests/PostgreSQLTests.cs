using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Testing.Platform.TestHost;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace NonSucking.Framework.Extension.EntityFrameworkCore.Tests;


[Explicit]
public class PostgreSQLTests
{
    [SetUp]
    public void Setup()
    {
        using var ctx = new PostgreDbContext();
        ctx.Database.Migrate();
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [DbContext(typeof(PostgreDbContext)), Migration("10000")]
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
            public int Age { get; set; }
            public int Balance { get; set; }

        }
    }

    [DbContext(typeof(PostgreDbContext)), Migration("10001")]
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
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class PostgreDbContext : BaseTestDbContext
    {
        public DbSet<TestEntity> Tests => Set<TestEntity>();

        public PostgreDbContext() : base("Host=localhost;Database=UnitTest;Username=testung;Password=unittest", "NonSucking.Framework.Extension.Database.PostgreSQL.dll")
        {
        }
    }
}