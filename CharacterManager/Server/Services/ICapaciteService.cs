using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion des capacités
/// </summary>
public interface ICapaciteService
{
    /// <summary>
    /// Récupère toutes les capacités
    /// </summary>
    Task<List<Capacite>> GetAllAsync();

    /// <summary>
    /// Récupère une capacité par son ID
    /// </summary>
    Task<Capacite?> GetByIdAsync(int id);

    /// <summary>
    /// Crée une nouvelle capacité
    /// </summary>
    Task<Capacite> CreateAsync(Capacite capacite);

    /// <summary>
    /// Met à jour une capacité existante
    /// </summary>
    Task<Capacite> UpdateAsync(int id, Capacite capacite);

    /// <summary>
    /// Supprime une capacité par son ID
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Obtient le nombre total de capacités
    /// </summary>
    int GetCount();
}
