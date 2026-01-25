using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharacterManager.Server.Services;

public class PersonnageService
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueModificationService _historiqueService;
    private readonly ILogger<PersonnageService> _logger;

    public PersonnageService(ApplicationDbContext context, HistoriqueModificationService historiqueService, ILogger<PersonnageService> logger)
    {
        _context = context;
        _historiqueService = historiqueService;
        _logger = logger;
    }

    public Task<IEnumerable<Personnage>> GetAllAsync()
    {
        return Task.FromResult(_context.Personnages.Include(static p => p.Capacites).Where(static p => p.GetType() == typeof(Personnage)).AsEnumerable());
    }

    public IEnumerable<Personnage> GetAll()
    {
        return [.. _context.Personnages.Include(static p => p.Capacites).Where(static p => p.GetType() == typeof(Personnage))];
    }

    public (int Commandants, int Mercenaires, int Androides) GetInventoryCounts()
    {
        var commandants = _context.Personnages.Count(p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage));
        var mercenaires = _context.Personnages.Count(p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage));
        var androides = _context.Personnages.Count(p => p.Type == TypePersonnage.Androide && p.GetType() == typeof(Personnage));
        return (commandants, mercenaires, androides);
    }

    public int GetPuissanceEscouade()
    {
        var puissancePersos = _context.Personnages
            .Where(p => p.Selectionne && p.Type != TypePersonnage.Commandant)
            .Sum(p => p.Puissance);

        var puissancecommandantEscouade = GetPuissanceCommandantEscouade();

        return puissancecommandantEscouade + puissancePersos + GetPuissanceLucieEscouade();
    }

    public int GetPuissanceLucieEscouade()
    {
        var puissanceTactiqueLucie = _context.Pieces
            .Where(p => p.Selectionnee && p.GetType() == typeof(Piece))
            .AsEnumerable()
            .Sum(p => p.AspectsTactiques?.Puissance ?? 0);

        var puissanceStrategiqueLucie = GetPuissanceStrategiqueLucie();

        return puissanceTactiqueLucie + puissanceStrategiqueLucie;
    }

    private int GetPuissanceCommandantEscouade()
    {
        _logger.LogDebug("[PersonnageService.GetPuissanceCommandantEscouade] Exécution de la requête FirstOrDefault pour commandant sélectionné");
        return _context.Personnages
            .Where(p => p.Selectionne && p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage))
            .Select(p => p.Puissance + p.Rang * 20)
            .FirstOrDefault();
    }


    private int GetPuissanceTopCommandant()
    {
        return _context.Personnages
            .Where(p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage))
            .Select(p => p.Puissance + p.Rang * 20)
            .AsEnumerable()
            .DefaultIfEmpty(0)
            .Max();
    }

    private int GetPuissanceStrategiqueLucie()
    {
        return _context.Pieces?
            .AsEnumerable()
            .Where(p => p.GetType() == typeof(Piece))
            .Sum(p => p.AspectsStrategiques?.Puissance ?? 0) ?? 0;
    }

    public int GetPuissanceMaxEscouade()
    {
        var puissanceMax = GetTopMercenaires().Sum(static p => p.Puissance) +
               GetPuissanceTopCommandant() +
               GetTopAndroides().Sum(static p => p.Puissance);

        var puissanceLucie = GetPuissanceMaxLucieEscouade();

        return puissanceMax + puissanceLucie;
    }

    public int GetPuissanceMaxLucieEscouade()
    {
        var puissanceLucie = GetTopLucieRooms()
            .AsEnumerable()
            .Sum(p => p.AspectsTactiques?.Puissance ?? 0) +
            GetPuissanceStrategiqueLucie();

        return puissanceLucie;
    }

    public int GetPuissanceSeuilCommandantPourLvlUp()
    {
        return 1000 * (GetTopCommandant()?.Niveau + 1 ?? 58000) - 58000;
    }

    public async Task<IEnumerable<Personnage>> GetTopMercenairesAsync(int count = 8)
    {
        return await _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage))
            .OrderByDescending(static p => p.Puissance)
            .Take(count)
            .ToListAsync();
    }

    public IEnumerable<Personnage> GetTopMercenaires(int count = 8)
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage))
            .OrderByDescending(static p => p.Puissance)
            .Take(count)
            .ToList();
    }

    public async Task<Personnage?> GetTopCommandantAsync()
    {
        return await _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage))
            .OrderByDescending(static p => p.Puissance)
            .FirstOrDefaultAsync();
    }

    public Personnage? GetTopCommandant()
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage))
            .OrderByDescending(p => p.Puissance + p.Rang * 20)
            .FirstOrDefault();
    }

    public IEnumerable<Piece> GetTopLucieRooms(int count = 2)
    {
        // PuissanceTotale is a computed [NotMapped] property, so evaluate client-side
        return _context.Pieces
            .AsNoTracking()
            .AsEnumerable()
            .Where(p => p.GetType() == typeof(Piece))
            .OrderByDescending(static p => p.PuissanceTotale)
            .Take(count)
            .ToList();
    }

    public async Task<IEnumerable<Personnage>> GetTopAndroidesAsync(int count = 3)
    {
        return await _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Androide && p.GetType() == typeof(Personnage))
            .OrderByDescending(static p => p.Puissance)
            .Take(count)
            .ToListAsync();
    }

    public IEnumerable<Personnage> GetTopAndroides(int count = 3)
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Androide && p.GetType() == typeof(Personnage))
            .OrderByDescending(static p => p.Puissance)
            .Take(count)
            .ToList();
    }

    public async Task<IEnumerable<Personnage>> GetEscouadeAsync()
    {
        return await _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Selectionne && p.GetType() == typeof(Personnage))
            .ToListAsync();
    }

    public async Task<IEnumerable<Personnage>> GetMercenairesAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage));

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Personnage>> GetCommandantsAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Commandant);

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Personnage>> GetAndroïdesAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Androide);

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne);
        }

        return await query.ToListAsync();
    }

    public IEnumerable<Personnage> GetEscouade()
    {
        return _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Selectionne)
            .ToList();
    }
    public IEnumerable<Personnage> GetMercenaires(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage));

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne);
        }

        return query.ToList();
    }
    public IEnumerable<Personnage> GetCommandants(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage));

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne);
        }

        return query.ToList();
    }
    public IEnumerable<Personnage> GetAndroides(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(static p => p.Capacites)
            .Where(static p => p.Type == TypePersonnage.Androide);

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionne && p.GetType() == typeof(Personnage));
        }

        return query.ToList();
    }

    public IEnumerable<Piece> GetPieces(bool selectionneOnly = false)
    {
        // Filtrer seulement les Piece (pas PieceHistorique) avant AsEnumerable
        var query = _context.Pieces
            .AsNoTracking()
            .Where(p => p.GetType() == typeof(Piece));

        if (selectionneOnly)
        {
            query = query.Where(static p => p.Selectionnee);
        }

        return query.ToList();
    }

    public Personnage? GetById(int id)
    {
        _logger.LogDebug("[PersonnageService.GetById] Récupération du personnage avec ID: {PersonnageId}", id);
        return _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefault(p => p.Id == id);
    }

    public async Task<Personnage?> GetByIdAsync(int id)
    {
        _logger.LogDebug("[PersonnageService.GetByIdAsync] Récupération du personnage avec ID: {PersonnageId}", id);
        return await _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Personnage personnage)
    {
        // Remplir les colonnes stockées pour compatibilité base de données (v0.12.1+: API de ressources)
        personnage.ImageUrlDetailStored = PersonnageImageUrlHelper.GetImageDetailUrl(personnage.Nom);
        personnage.ImageUrlPreviewStored = PersonnageImageUrlHelper.GetImageSmallPortraitUrl(personnage.Nom);
        personnage.ImageUrlSelectedStored = PersonnageImageUrlHelper.GetImageSmallSelectUrl(personnage.Nom);
        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();

        // Enregistrer dans l'historique
        await _historiqueService.EnregistrerCreationAsync(
            TypeEntite.Personnage,
            personnage.Id,
            personnage.Nom,
            new { personnage.Nom, personnage.Type, personnage.Niveau, personnage.Puissance },
            "Création d'un personnage");
    }

    public async Task UpdateAsync(Personnage personnage)
    {
        // Récupérer les anciennes valeurs depuis la BDD (sans tracking pour éviter les conflits)
        var oldValues = await _context.Personnages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personnage.Id);
        
        if (oldValues == null) 
        {
            return;
        }

        // Maintenant récupérer l'entité trackée pour la mise à jour
        var existing = await _context.Personnages.FindAsync(personnage.Id);
        if (existing != null)
        {
            // Capturer les anciennes valeurs pour l'historique en comparant avec oldValues
            var modifications = DetectPersonnageModifications(oldValues, personnage);

            existing.Nom = personnage.Nom;
            existing.Rarete = personnage.Rarete;
            existing.Niveau = personnage.Niveau;
            existing.Type = personnage.Type;
            existing.Rang = personnage.Rang;
            existing.Puissance = personnage.Puissance;
            existing.PA = personnage.PA;
            existing.PV = personnage.PV;
            existing.Role = personnage.Role;
            existing.Faction = personnage.Faction;
            existing.Selectionne = personnage.Selectionne;
            existing.TypeAttaque = personnage.TypeAttaque;
            existing.HasRelation = personnage.HasRelation;
            existing.NivRelation = personnage.NivRelation;

            // Mettre à jour les colonnes stockées si le nom change (v0.12.1+: API de ressources)
            existing.ImageUrlDetailStored = PersonnageImageUrlHelper.GetImageDetailUrl(existing.Nom);
            existing.ImageUrlPreviewStored = PersonnageImageUrlHelper.GetImageSmallPortraitUrl(existing.Nom);
            existing.ImageUrlSelectedStored = PersonnageImageUrlHelper.GetImageSmallSelectUrl(existing.Nom);

            await _context.SaveChangesAsync();

            // Enregistrer chaque modification dans l'historique
            foreach (var (champ, ancienne, nouvelle) in modifications)
            {
                await _historiqueService.EnregistrerModificationAsync(
                    TypeEntite.Personnage,
                    personnage.Id,
                    personnage.Nom,
                    champ,
                    ancienne,
                    nouvelle,
                    $"Modification de {champ}");
            }
        }
    }

    private static List<(string champ, object? ancienne, object? nouvelle)> DetectPersonnageModifications(Personnage oldValues, Personnage newValues)
    {
        var modifications = new List<(string champ, object? ancienne, object? nouvelle)>();

        if (oldValues.Nom != newValues.Nom)
            modifications.Add(("Nom", oldValues.Nom, newValues.Nom));
        if (oldValues.Niveau != newValues.Niveau)
            modifications.Add(("Niveau", oldValues.Niveau, newValues.Niveau));
        if (oldValues.Rang != newValues.Rang)
            modifications.Add(("Rang", oldValues.Rang, newValues.Rang));
        if (oldValues.Puissance != newValues.Puissance)
            modifications.Add(("Puissance", oldValues.Puissance, newValues.Puissance));
        if (oldValues.PA != newValues.PA)
            modifications.Add(("PA", oldValues.PA, newValues.PA));
        if (oldValues.PV != newValues.PV)
            modifications.Add(("PV", oldValues.PV, newValues.PV));
        if (oldValues.Selectionne != newValues.Selectionne)
            modifications.Add(("Selectionne", oldValues.Selectionne, newValues.Selectionne));
        if (oldValues.NivRelation != newValues.NivRelation)
            modifications.Add(("NivRelation", oldValues.NivRelation, newValues.NivRelation));

        return modifications;
    }

    public async Task DeleteAsync(int id)
    {
        var personnage = await _context.Personnages.FindAsync(id);
        if (personnage != null)
        {
            // Enregistrer la suppression dans l'historique avant de supprimer
            await _historiqueService.EnregistrerSuppressionAsync(
                TypeEntite.Personnage,
                personnage.Id,
                personnage.Nom,
                new { personnage.Nom, personnage.Type, personnage.Niveau, personnage.Puissance },
                "Suppression d'un personnage");

            _context.Personnages.Remove(personnage);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UpdateCapacitesAsync(int personnageId, IEnumerable<int> capaciteIds)
    {
        var personnage = await _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefaultAsync(p => p.Id == personnageId);

        if (personnage == null)
        {
            return false;
        }

        var capacites = await _context.Capacites
            .Where(c => capaciteIds.Contains(c.Id))
            .ToListAsync();

        personnage.Capacites.Clear();
        foreach (var capacite in capacites)
        {
            personnage.Capacites.Add(capacite);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public void DeleteAll()
    {
        _context.Personnages.RemoveRange(_context.Personnages);
        _context.Pieces.RemoveRange(_context.Pieces);
        _context.SaveChanges();
    }

    // ===== Méthodes pour Templates =====

    public async Task<Template> CreateTemplateAsync(string nom, string description, List<int> personnageIds)
    {
        var personnages = await _context.Personnages
            .Where(p => personnageIds.Contains(p.Id))
            .ToListAsync();

        var template = new Template
        {
            Nom = nom,
            Description = description,
            PuissanceTotal = personnages.Sum(p => p.Puissance),
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };

        template.SetPersonnageIds(personnageIds);
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<Template?> GetTemplateAsync(int id)
    {
        return await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
    }

    public IEnumerable<Template> GetAllTemplates()
    {
        return [.. _context.Templates.OrderByDescending(static t => t.DateModification)];
    }

    public async Task<bool> UpdateTemplateAsync(int templateId, string nom, string description, List<int> personnageIds)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null)
            return false;

        var personnages = await _context.Personnages
            .Where(p => personnageIds.Contains(p.Id))
        .ToListAsync();

        template.Nom = nom;
        template.Description = description;
        template.PuissanceTotal = personnages.Sum(p => p.Puissance);
        template.DateModification = DateTime.UtcNow;
        template.SetPersonnageIds(personnageIds);

        _context.Templates.Update(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template == null)
            return false;

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public IEnumerable<Personnage> GetTemplatePersonnages(Template template)
    {
        var ids = template.GetPersonnageIds();
        return _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => ids.Contains(p.Id))
            .ToList();
    }

    public int GetTemplatePuissance(Template template)
    {
        var ids = template.GetPersonnageIds();
        return _context.Personnages
            .Where(p => ids.Contains(p.Id))
            .Sum(p => p.Puissance);
    }

    /// <summary>
    /// Récupère la Lucie House avec tous les détails
    /// </summary>
    public async Task<LucieHouse?> GetLucieHouseAsync()
    {
        return await _context.LucieHouses
            .Include(l => l.Pieces)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Récupère toutes les pièces de la Lucie House
    /// </summary>
    public async Task<List<Piece>> GetLuciePiecesAsync()
    {
        return await _context.Pieces
            .Where(p => p.GetType() == typeof(Piece))
            .ToListAsync();
    }

    /// <summary>
    /// Met à jour le niveau d'affection de la Lucie House (créé une entrée par défaut si absente).
    /// </summary>
    public async Task<int> UpdateLucieAffectionAsync(int affection)
    {
        var boundedAffection = affection < 0 ? 0 : affection;
        var lucieHouse = await _context.LucieHouses.Include(static l => l.Pieces).OrderBy(l => l.Id).FirstOrDefaultAsync();
        var ancienneAffection = lucieHouse?.Affection ?? 0;

        if (lucieHouse == null)
        {
            lucieHouse = LucieHouse.CreerDefaut();
            lucieHouse.Affection = boundedAffection;
            _context.LucieHouses.Add(lucieHouse);
        }
        else
        {
            lucieHouse.Affection = boundedAffection;
            _context.LucieHouses.Update(lucieHouse);
        }

        await _context.SaveChangesAsync();
        
        // Historiser la modification d'affection si elle a changé
        if (ancienneAffection != boundedAffection)
        {
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece,
                lucieHouse.Id,
                "Maison de Lucie",
                "Affection",
                ancienneAffection,
                boundedAffection,
                $"Modification de l'affection de la Maison de Lucie");
        }
        
        return lucieHouse.Affection;
    }

    /// <summary>
    /// Met à jour un champ d'une pièce de la Lucie House avec historisation.
    /// </summary>
    public async Task UpdatePieceAsync(int pieceId, string champModifie, object? ancienneValeur, object? nouvelleValeur, string nomPiece)
    {
        var piece = await _context.Pieces
            .Where(p => p.Id == pieceId && p.GetType() == typeof(Piece))
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (piece != null)
        {
            // Historiser la modification
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece,
                pieceId,
                nomPiece,
                champModifie,
                ancienneValeur,
                nouvelleValeur,
                $"Modification de {champModifie} pour {nomPiece}");

            // Sauvegarder les modifications
            _context.Pieces.Update(piece);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Met à jour une pièce de la Lucie House
    /// </summary>
    public async Task UpdateLuciePieceAsync(Piece updatedPiece)
    {
        _logger.LogDebug("[PersonnageService.UpdateLuciePieceAsync] Mise à jour de la pièce: {PieceName} (ID: {PieceId})", updatedPiece.Nom, updatedPiece.Id);
        
        var piece = await _context.Pieces
            .Where(p => p.Id == updatedPiece.Id && p.GetType() == typeof(Piece))
            .FirstOrDefaultAsync();

        if (piece == null)
        {
            throw new InvalidOperationException($"Pièce avec ID {updatedPiece.Id} introuvable");
        }

        // Capturer les anciennes valeurs pour l'historique
        var ancienNiveau = piece.Niveau;
        var ancienneSelection = piece.Selectionnee;
        var anciennePuissanceTactique = piece.AspectsTactiques?.Puissance;
        var anciennePuissanceStrategique = piece.AspectsStrategiques?.Puissance;

        // Appliquer les modifications
        piece.Niveau = updatedPiece.Niveau;
        piece.Selectionnee = updatedPiece.Selectionnee;

        if (piece.AspectsTactiques != null && updatedPiece.AspectsTactiques != null)
        {
            piece.AspectsTactiques.Puissance = updatedPiece.AspectsTactiques.Puissance;
        }

        if (piece.AspectsStrategiques != null && updatedPiece.AspectsStrategiques != null)
        {
            piece.AspectsStrategiques.Puissance = updatedPiece.AspectsStrategiques.Puissance;
        }

        _context.Pieces.Update(piece);
        await _context.SaveChangesAsync();

        // Historiser les modifications
        if (ancienNiveau != piece.Niveau)
        {
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece, piece.Id, piece.Nom, "Niveau", ancienNiveau, piece.Niveau, 
                $"Modification du niveau de {piece.Nom}");
        }
        if (ancienneSelection != piece.Selectionnee)
        {
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece, piece.Id, piece.Nom, "Selectionnee", ancienneSelection, piece.Selectionnee,
                $"Modification de la sélection de {piece.Nom}");
        }
        if (anciennePuissanceTactique != piece.AspectsTactiques?.Puissance)
        {
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece, piece.Id, piece.Nom, "AspectsTactiques.Puissance", 
                anciennePuissanceTactique, piece.AspectsTactiques?.Puissance,
                $"Modification de la puissance tactique de {piece.Nom}");
        }
        if (anciennePuissanceStrategique != piece.AspectsStrategiques?.Puissance)
        {
            await _historiqueService.EnregistrerModificationAsync(
                TypeEntite.Piece, piece.Id, piece.Nom, "AspectsStrategiques.Puissance", 
                anciennePuissanceStrategique, piece.AspectsStrategiques?.Puissance,
                $"Modification de la puissance stratégique de {piece.Nom}");
        }
    }
}



