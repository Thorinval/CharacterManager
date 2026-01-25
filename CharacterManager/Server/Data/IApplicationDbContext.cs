using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Data;

/// <summary>
/// Interface for ApplicationDbContext to support dependency injection
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Personnage> Personnages { get; }
    DbSet<Capacite> Capacites { get; }
    DbSet<AppSettings> AppSettings { get; }
    DbSet<Template> Templates { get; }
    DbSet<Profile> Profiles { get; }
    DbSet<LucieHouse> LucieHouses { get; }
    DbSet<Piece> Pieces { get; }
    DbSet<HistoriqueClassement> HistoriquesClassement { get; }
    DbSet<HistoriqueLigue> HistoriquesLigue { get; }
    DbSet<HistoriqueModification> HistoriquesModifications { get; }
    DbSet<PersonnageClassement> PersonnagesClassement { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

