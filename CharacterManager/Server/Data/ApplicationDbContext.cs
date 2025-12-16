using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Personnage> Personnages { get; set; }
    public DbSet<Capacite> Capacites { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<HistoriqueEscouade> HistoriquesEscouade { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Personnage>()
            .HasMany(p => p.Capacites)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}