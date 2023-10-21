
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace TestCoreExtension;

//[Migration(Id)]
[DbContext(typeof(DbContext2))]
public partial class UserConnectionContextMigration001 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.SetUpgradeOperations(this);

    }
}
