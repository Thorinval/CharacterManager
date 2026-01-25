using System.Text.Json;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CharacterManager.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Personnage> Personnages { get; set; }
    public DbSet<Capacite> Capacites { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<LucieHouse> LucieHouses { get; set; }
    public DbSet<Piece> Pieces { get; set; }
    public DbSet<RoadmapNote> RoadmapNotes => Set<RoadmapNote>();

    public DbSet<HistoriqueClassement> HistoriquesClassement { get; set; }
    public DbSet<HistoriqueLigue> HistoriquesLigue { get; set; }
    public DbSet<HistoriqueModification> HistoriquesModifications { get; set; }
    public DbSet<PersonnageClassement> PersonnagesClassement { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Héritage TPH pour Personnage
        modelBuilder.Entity<Personnage>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<Personnage>("Personnage")
            .HasValue<PersonnageHistorique>("PersonnageHistorique");

        // Héritage TPH pour Piece
        modelBuilder.Entity<Piece>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<Piece>("Piece")
            .HasValue<PieceHistorique>("PieceHistorique");

        var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        var aspectConverter = new ValueConverter<Aspect, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? new Aspect() : JsonSerializer.Deserialize<Aspect>(v, jsonOptions) ?? new Aspect());

        var aspectComparer = new ValueComparer<Aspect>(
            (l, r) => JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions),
            v => JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<Aspect>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions)!);

        modelBuilder.Entity<Personnage>()
            .HasMany(p => p.Capacites)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LucieHouse>()
            .HasMany(l => l.Pieces)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Piece>()
            .Property(p => p.AspectsTactiques)
            .HasConversion(aspectConverter)
            .Metadata.SetValueComparer(aspectComparer);

        modelBuilder.Entity<Piece>()
            .Property(p => p.AspectsStrategiques)
            .HasConversion(aspectConverter)
            .Metadata.SetValueComparer(aspectComparer);

        // Configuration de PuissanceLegacy comme propriété calculée côté application
        // Ne pas utiliser ValueGeneratedOnAddOrUpdate car SQLite ne peut pas calculer automatiquement
        modelBuilder.Entity<Piece>()
            .Property(p => p.PuissanceLegacy)
            .ValueGeneratedNever();

        modelBuilder.Entity<Classement>().HasKey(c => c.Id);

        modelBuilder.Entity<HistoriqueClassement>()
            .HasMany(h => h.Classements)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration de PersonnageClassement
        modelBuilder.Entity<PersonnageClassement>()
            .HasKey(p => p.Id);

        // Relations HistoriqueClassement - PersonnageClassement
        // Utilisez des shadow properties avec des noms explicites pour distinguer les relations
        
        // Mercenaires (one-to-many)
        modelBuilder.Entity<HistoriqueClassement>()
            .HasMany(h => h.Mercenaires)
            .WithOne()
            .HasForeignKey("HistoriqueClassementMercenaireId")
            .OnDelete(DeleteBehavior.Cascade);

        // Androïdes (one-to-many)
        modelBuilder.Entity<HistoriqueClassement>()
            .HasMany(h => h.Androides)
            .WithOne()
            .HasForeignKey("HistoriqueClassementAndroideId")
            .OnDelete(DeleteBehavior.Cascade);

        // Commandant (one-to-one)
        modelBuilder.Entity<HistoriqueClassement>()
            .HasOne(h => h.Commandant)
            .WithOne()
            .HasForeignKey<PersonnageClassement>("HistoriqueClassementCommandantId")
            .OnDelete(DeleteBehavior.Cascade);

        // HistoriqueModification - Index sur DateModification pour recherches rapides
        modelBuilder.Entity<HistoriqueModification>()
            .HasIndex(h => h.DateModification);
    }
}
