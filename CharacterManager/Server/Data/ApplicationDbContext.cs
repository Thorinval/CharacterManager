using System.Text.Json;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CharacterManager.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Personnage> Personnages { get; set; }
    public DbSet<Capacite> Capacites { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<HistoriqueEscouade> HistoriquesEscouade { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<LucieHouse> LucieHouses { get; set; }
    public DbSet<Piece> Pieces { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}