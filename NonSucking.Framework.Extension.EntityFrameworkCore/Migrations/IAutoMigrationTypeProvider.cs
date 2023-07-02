namespace NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

public interface IAutoMigrationTypeProvider
{
    IReadOnlyList<Type> GetEntityTypes();
}