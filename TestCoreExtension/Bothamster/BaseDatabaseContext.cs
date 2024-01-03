using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;

namespace TestCoreExtension.Bothamster;

public class BaseDatabaseContext : DatabaseContext
{
    public BaseDatabaseContext()
    {
    }

    public BaseDatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Filename =Test.db");

        base.OnConfiguring(optionsBuilder);
    }
}
