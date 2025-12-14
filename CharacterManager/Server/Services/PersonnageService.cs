using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class PersonnageService
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageImageConfigService _imageConfigService;

    public PersonnageService(ApplicationDbContext context, PersonnageImageConfigService imageConfigService)
    {
        _context = context;
        _imageConfigService = imageConfigService;
    }

    private bool IsAdultModeEnabled()
    {
        var settings = _context.AppSettings.FirstOrDefault();
        return settings?.IsAdultModeEnabled ?? true;
    }

    private string ApplyAdultModeFilter(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return imagePath;

        var isAdultMode = IsAdultModeEnabled();
        return _imageConfigService.GetDisplayPath(imagePath, isAdultMode);
    }

    private void ApplyAdultModeToPersonnage(Personnage personnage)
    {
        if (personnage == null) return;

        personnage.ImageUrlDetail = ApplyAdultModeFilter(personnage.ImageUrlDetail);
        personnage.ImageUrlPreview = ApplyAdultModeFilter(personnage.ImageUrlPreview);
        personnage.ImageUrlSelected = ApplyAdultModeFilter(personnage.ImageUrlSelected);
    }

    private void ApplyAdultModeToPersonnages(IEnumerable<Personnage> personnages)
    {
        foreach (var personnage in personnages)
        {
            ApplyAdultModeToPersonnage(personnage);
        }
    }

    public IEnumerable<Personnage> GetAll()
    {
        var personnages = _context.Personnages.Include(p => p.Capacites).ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
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

    public IEnumerable<Personnage> GetTopMercenaires(int count = 8)
    {
        var personnages = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
    }

    public Personnage? GetTopCommandant()
    {
        var personnage = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant)
            .OrderByDescending(p => p.Puissance)
            .FirstOrDefault();
        if (personnage != null)
            ApplyAdultModeToPersonnage(personnage);
        return personnage;
    }

    public IEnumerable<Personnage> GetTopAndroides(int count = 3)
    {
        var personnages = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androïde)
            .OrderByDescending(p => p.Puissance)
            .Take(count)
            .ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
    }

    public IEnumerable<Personnage> GetEscouade()
    {
        var personnages = _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Selectionne)
            .ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
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

        var personnages = query.ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
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

        var personnages = query.ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
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

        var personnages = query.ToList();
        ApplyAdultModeToPersonnages(personnages);
        return personnages;
    }

    public Personnage? GetById(int id)
    {
        var personnage = _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefault(p => p.Id == id);
        if (personnage != null)
            ApplyAdultModeToPersonnage(personnage);
        return personnage;
    }

    public void Add(Personnage personnage)
    {
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
            existing.ImageUrlDetail = personnage.ImageUrlDetail;
            existing.ImageUrlPreview = personnage.ImageUrlPreview;
            existing.ImageUrlSelected = personnage.ImageUrlSelected;
            existing.Description = personnage.Description;
            existing.Selectionne = personnage.Selectionne;
            existing.TypeAttaque = personnage.TypeAttaque;

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