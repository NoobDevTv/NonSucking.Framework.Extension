
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

using System.ComponentModel.DataAnnotations.Schema;

namespace TestCoreExtension;

//[Migration(Id)]
[DbContext(typeof(DbContext2))]
public partial class TestMigration002 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.SetUpgradeOperations(this);

    }
}
[History]
public partial class TestMigration002 : IAutoMigrationTypeProvider
{
    public const string Id = $"2022_10_23-20_39_02-{nameof(DbContext2)}-InitialMigration";
    public IReadOnlyList<Type> GetEntityTypes()
    {
        return new[]
        {
            typeof(Example),
        };
    }

    [Table("Example")]
    private class Example : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public int Id2 { get; set; }
    }

}
