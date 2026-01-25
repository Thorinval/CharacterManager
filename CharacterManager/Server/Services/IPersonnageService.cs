using CharacterManager.Server.Constants;
using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

public interface IPersonnageService
{
    // Get All
    Task<IEnumerable<Personnage>> GetAllAsync();
    IEnumerable<Personnage> GetAll();
    (int Commandants, int Mercenaires, int Androides) GetInventoryCounts();
    
    // Puissance methods
    int GetPuissanceEscouade();
    int GetPuissanceLucieEscouade();
    int GetPuissanceMaxEscouade();
    int GetPuissanceMaxLucieEscouade();
    int GetPuissanceSeuilCommandantPourLvlUp();
    
    // Top methods
    Task<IEnumerable<Personnage>> GetTopMercenairesAsync(int count = 8);
    IEnumerable<Personnage> GetTopMercenaires(int count = 8);
    Task<Personnage?> GetTopCommandantAsync();
    Personnage? GetTopCommandant();
    IEnumerable<Piece> GetTopLucieRooms(int count = 2);
    Task<IEnumerable<Personnage>> GetTopAndroidesAsync(int count = 3);
    IEnumerable<Personnage> GetTopAndroides(int count = 3);
    
    // Get by type
    Task<IEnumerable<Personnage>> GetEscouadeAsync();
    Task<IEnumerable<Personnage>> GetMercenairesAsync(bool selectionneOnly = false);
    Task<IEnumerable<Personnage>> GetCommandantsAsync(bool selectionneOnly = false);
    Task<IEnumerable<Personnage>> GetAndroïdesAsync(bool selectionneOnly = false);
    IEnumerable<Personnage> GetEscouade();
    IEnumerable<Personnage> GetMercenaires(bool selectionneOnly = false);
    IEnumerable<Personnage> GetCommandants(bool selectionneOnly = false);
    IEnumerable<Personnage> GetAndroides(bool selectionneOnly = false);
    IEnumerable<Piece> GetPieces(bool selectionneOnly = false);
    
    // Get by ID
    Personnage? GetById(int id);
    Task<Personnage?> GetByIdAsync(int id);
    
    // CRUD operations
    Task AddAsync(Personnage personnage);
    Task UpdateAsync(Personnage personnage);
    Task DeleteAsync(int id);
    Task<bool> UpdateCapacitesAsync(int personnageId, IEnumerable<int> capaciteIds);
    void DeleteAll();
    
    // Template operations
    Task<Template> CreateTemplateAsync(string nom, string description, List<int> personnageIds);
    Task<Template?> GetTemplateAsync(int id);
    IEnumerable<Template> GetAllTemplates();
    Task<bool> UpdateTemplateAsync(int templateId, string nom, string description, List<int> personnageIds);
    Task<bool> DeleteTemplateAsync(int id);
    IEnumerable<Personnage> GetTemplatePersonnages(Template template);
    int GetTemplatePuissance(Template template);
    
    // Lucie House operations
    Task<LucieHouse?> GetLucieHouseAsync();
    Task<List<Piece>> GetLuciePiecesAsync();
    Task<int> UpdateLucieAffectionAsync(int affection);
    Task UpdatePieceAsync(int pieceId, string champModifie, object? ancienneValeur, object? nouvelleValeur, string nomPiece);
    Task UpdateLuciePieceAsync(Piece updatedPiece);
}

