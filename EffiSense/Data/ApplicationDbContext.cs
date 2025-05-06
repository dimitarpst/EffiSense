using EffiSense.Models;
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
    public DbSet<Usage> Usages { get; set; }
    public DbSet<ChatMessageLog> ChatMessageLogs { get; set; } 

}
