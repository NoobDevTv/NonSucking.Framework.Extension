// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NonSucking.Framework.Extension.Database.Sqlite;
using NonSucking.Framework.Extension.EntityFrameworkCore;

using TestCoreExtension.Bothamster;

DatabaseFactory.DatabaseConfigurators.Add(new SqLiteConfigurator());

using (var ctx = new RightsDbContext())
    ctx.Migrate();

using (var ctx = new UserConnectionContext())
    ctx.Migrate();

var b = typeof(A.B);
;
public class A
{
    public class B
    {

    }
}


class DbContext2 : DatabaseContext
{
    public DbSet<Example> Examples { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Filename =Test.db");
        base.OnConfiguring(optionsBuilder);
    }
}

public class Example : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Id2 { get; set; }
}