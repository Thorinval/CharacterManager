using CharacterManager.Models;

namespace CharacterManager.Services;

public class PersonnageService
{
    private readonly List<Personnage> _personnages = new();
    private int _nextId = 1;

    public IEnumerable<Personnage> GetAll() => _personnages;

    public Personnage? GetById(int id) => _personnages.FirstOrDefault(p => p.Id == id);

    public void Add(Personnage personnage)
    {
        personnage.Id = _nextId++;
        _personnages.Add(personnage);
    }

    public void Update(Personnage personnage)
    {
        var existing = GetById(personnage.Id);
        if (existing != null)
        {
            existing.Nom = personnage.Nom;
            existing.Classe = personnage.Classe;
            existing.Niveau = personnage.Niveau;
            existing.Race = personnage.Race;
        }
    }

    public void Delete(int id)
    {
        var personnage = GetById(id);
        if (personnage != null)
        {
            _personnages.Remove(personnage);
        }
    }
}