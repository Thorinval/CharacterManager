using CharacterManager.Data;
using CharacterManager.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Services;

public class PersonnageService
{
    private readonly ApplicationDbContext _context;

    public PersonnageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Personnage> GetAll()
    {
        return _context.Personnages
            .Include(p => p.Capacites)
            .ToList();
    }

    public Personnage? GetById(int id)
    {
        return _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefault(p => p.Id == id);
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
            existing.Rareté = personnage.Rareté;
            existing.Niveau = personnage.Niveau;
            existing.Type = personnage.Type;
            existing.Rang = personnage.Rang;
            existing.Puissance = personnage.Puissance;
            existing.PA = personnage.PA;
            existing.PAMax = personnage.PAMax;
            existing.PV = personnage.PV;
            existing.PVMax = personnage.PVMax;
            existing.Sante = personnage.Sante;
            existing.SanteMax = personnage.SanteMax;
            existing.Role = personnage.Role;
            existing.Faction = personnage.Faction;
            existing.ImageUrl = personnage.ImageUrl;
            existing.Description = personnage.Description;
            existing.Localisation = personnage.Localisation;
            existing.Selectionné = personnage.Selectionné;

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
}