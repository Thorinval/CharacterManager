using CharacterManager.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Personnage> Personnages { get; set; }
    public DbSet<Capacite> Capacites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Personnage>()
            .HasMany(p => p.Capacites)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}