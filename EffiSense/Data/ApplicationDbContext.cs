using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Home> Homes { get; set; }
    public DbSet<Appliance> Appliances { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

       
        modelBuilder.Entity<Home>()
            .HasIndex(h => h.UserId)
            .HasDatabaseName("IX_UserId");

        
        modelBuilder.Entity<Appliance>()
            .HasIndex(a => a.HomeId)
            .HasDatabaseName("IX_HomeId");

       
    }
}

