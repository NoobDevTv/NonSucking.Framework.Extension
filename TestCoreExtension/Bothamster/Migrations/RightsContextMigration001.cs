
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using TestCoreExtension.Bothamster;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

namespace BotMaster.RightsManagement.Migrations;

[Migration(Id)]
[DbContext(typeof(RightsDbContext))]
public partial class RightsContextMigration001 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.SetUpgradeOperations(this);
    }
}
