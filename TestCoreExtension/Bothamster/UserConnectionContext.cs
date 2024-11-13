using Microsoft.EntityFrameworkCore;

namespace TestCoreExtension.Bothamster;

public class UserConnectionContext : BaseDatabaseContext
{
    public DbSet<UserConnection> UserConnections => Set<UserConnection>();
    public DbSet<PlattformUser> PlattformUsers => Set<PlattformUser>();
    public DbSet<User> Users => Set<User>();

    public UserConnectionContext()
    {
    }

    public UserConnectionContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
