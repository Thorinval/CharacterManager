using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class PersonnageService
{
    private readonly ApplicationDbContext _context;

    public PersonnageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<Personnage>> GetAllAsync()
    {
        return Task.FromResult(_context.Personnages.Include(p => p.Capacites).AsEnumerable());
    }
    
    public IEnumerable<Personnage> GetAll()
    {
        return _context.Personnages.Include(p => p.Capacites).ToList();
    }

    public int GetPuissanceEscouade()
    {
        return _context.Personnages
            .Where(p => p.Selectionne)
            .Sum(p => p.Puissance);
    }

    public int GetPuissanceMaxEscouade()
    {
        return GetTopMercenaires().Sum(p => p.Puissance) +
               (GetTopCommandant()?.Puissance ?? 0) +
               GetTopAndroides().Sum(p => p.Puissance);
    }

    public int GetPuissanceSeuilCommandantPourLvlUp()
    {
        return 1000 * (GetTopCommandant()?.Niveau + 1 ?? 58000) - 58000;
    }

    public async Task<IEnumerable<Personnage>> GetTopMercenairesAsync(int count = 8)
    {
        return await Task.FromResult(_context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList());
    }
    
    public IEnumerable<Personnage> GetTopMercenaires(int count = 8)
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList();
    }

    public async Task<Personnage?> GetTopCommandantAsync()
    {
        return await Task.FromResult(_context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant)
            .OrderByDescending(p => p.Puissance)
            .FirstOrDefault());
    }
    
    public Personnage? GetTopCommandant()
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant)
            .OrderByDescending(p => p.Puissance)
            .FirstOrDefault();
    }

    public async Task<IEnumerable<Personnage>> GetTopAndroidesAsync(int count = 3)
    {
        return await Task.FromResult(_context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androïde)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList());
    }
    
    public IEnumerable<Personnage> GetTopAndroides(int count = 3)
    {
        return _context.Personnages
            .AsNoTracking()
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androïde)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList();
    }

    public async Task<IEnumerable<Personnage>> GetEscouadeAsync()
    {
        return await Task.FromResult(_context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Selectionne)
            .ToList());
    }

    public async Task<IEnumerable<Personnage>> GetMercenairesAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return await Task.FromResult(query.ToList());
    }

    public async Task<IEnumerable<Personnage>> GetCommandantsAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return await Task.FromResult(query.ToList());
    }

    public async Task<IEnumerable<Personnage>> GetAndroïdesAsync(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androïde);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return await Task.FromResult(query.ToList());
    }
    
    public IEnumerable<Personnage> GetEscouade()
    {
        return _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Selectionne)
            .ToList();
    }
    public IEnumerable<Personnage> GetMercenaires(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return query.ToList();
    }
    public IEnumerable<Personnage> GetCommandants(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return query.ToList();
    }
    public IEnumerable<Personnage> GetAndroides(bool selectionneOnly = false)
    {
        var query = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androïde);

        if (selectionneOnly)
        {
            query = query.Where(p => p.Selectionne);
        }

        return query.ToList();
    }

    public Personnage? GetById(int id)
    {
        return _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefault(p => p.Id == id);
    }

    public async Task<Personnage?> GetByIdAsync(int id)
    {
        return await _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Personnage personnage)
    {
        // Remplir les colonnes stockées pour compatibilité base de données
        var slug = personnage.Nom?.ToLower().Replace(" ", "_") ?? string.Empty;
        personnage.ImageUrlDetailStored = $"/images/personnages/{slug}.png";
        personnage.ImageUrlPreviewStored = $"/images/personnages/{slug}_small_portrait.png";
        personnage.ImageUrlSelectedStored = $"/images/personnages/{slug}_small_select.png";
        _context.Personnages.Add(personnage);
        _context.SaveChanges();
    }

    public void Update(Personnage personnage)
    {
        var existing = _context.Personnages.Find(personnage.Id);
        if (existing != null)
        {
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
            existing.Description = personnage.Description;
            existing.Selectionne = personnage.Selectionne;
            existing.TypeAttaque = personnage.TypeAttaque;

            // Mettre à jour les colonnes stockées si le nom change
            var slug = existing.Nom?.ToLower().Replace(" ", "_") ?? string.Empty;
            existing.ImageUrlDetailStored = $"/images/personnages/{slug}.png";
            existing.ImageUrlPreviewStored = $"/images/personnages/{slug}_small_portrait.png";
            existing.ImageUrlSelectedStored = $"/images/personnages/{slug}_small_select.png";

            _context.SaveChanges();
        }
    }

    public void Delete(int id)
    {
        var personnage = _context.Personnages.Find(id);
        if (personnage != null)
        {
            _context.Personnages.Remove(personnage);
            _context.SaveChanges();
        }
    }

    // ===== Méthodes pour Templates =====

    public async Task<Template> CreateTemplateAsync(string nom, string description, List<int> personnageIds)
    {
        var personnages = _context.Personnages
            .Where(p => personnageIds.Contains(p.Id))
            .ToList();

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
        return [.. _context.Templates.OrderByDescending(t => t.DateModification)];
    }

    public async Task<bool> UpdateTemplateAsync(int templateId, string nom, string description, List<int> personnageIds)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null)
            return false;

        var personnages = _context.Personnages
            .Where(p => personnageIds.Contains(p.Id))
            .ToList();

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
}