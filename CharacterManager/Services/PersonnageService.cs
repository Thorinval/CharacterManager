using CharacterManager.Data;
using CharacterManager.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace CharacterManager.Services;

public class PersonnageService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public IEnumerable<Personnage> GetAll()
    {
        return [.. _context.Personnages.Include(p => p.Capacites)];
    }

    public int GetPuissanceEscouade()
    {
        return _context.Personnages
            .Where(p => p.Selectionne)
            .Sum(p => p.Puissance);
    }

    public int GetPuissanceMaxEscouade()
    {
        return GetSommePuissanceMaxMercenaires() +
               GetPuissanceMaxCommandant() +
               GetSommePuissanceMaxAndroides(); 
    }

    public int GetSommePuissanceMaxMercenaires()
    {
        var mercenaires = _context.Personnages.Where(p => p.Type == TypePersonnage.Mercenaire).OrderByDescending(p => p.Puissance).Take(8);
        return mercenaires.Any() ? mercenaires.Sum(p => p.Puissance) : 0;
    }

      public int GetPuissanceMaxCommandant()
    {
        var commandants = _context.Personnages.Where(p => p.Type == TypePersonnage.Commandant);
        return commandants.Any() ? commandants.Max(p => p.Puissance) : 0;
    }  

       public int GetSommePuissanceMaxAndroides()
    {
        var androides = _context.Personnages.Where(p => p.Type == TypePersonnage.Androïde).OrderByDescending(p => p.Puissance).Take(3);
        return androides.Any() ? androides.Sum(p => p.Puissance) : 0;
    }     

    public IEnumerable<Personnage> GetEscouade()
    {
        return _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Selectionne);
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

        return query;
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

        return query;
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

        return query;
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
}