using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

public interface IPmlImportService
{
    Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName = "",
        bool importInventory = true, bool importTemplates = true,
        bool importBestSquad = true, bool importHistories = true, bool importLeagueHistory = false);
    
    Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName, PmlImportOptions options);
    
    Task<string?> GetLastImportedFileName();
    
    Task<DateTime?> GetLastImportedDateAsync();
    
    Task<DateTime?> GetLastExportDate();
    
    Task<ImportResult> ImportCapacitesAsync(Stream pmlStream, string fileName = "");
    
    Task<ImportPreviewResult> PreviewPmlClassementsAsync(Stream pmlStream);
    
    Task<ImportResult> ImportPmlWithConflictResolution(Stream pmlStream, string fileName, 
        Dictionary<string, bool> conflictResolutions, List<ImportConflict>? originalConflicts = null);
}
