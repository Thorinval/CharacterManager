using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service pour exporter les données au format PML (XML personnalisé)
/// </summary>
public interface IPmlExportService
{
    /// <summary>
    /// Exporte les données sélectionnées au format PML
    /// </summary>
    Task<byte[]> ExportPmlAsync(PmlExportOptions? options = null);

    /// <summary>
    /// Exporte l'inventaire au format PML
    /// </summary>
    Task<byte[]> ExporterInventairePmlAsync(IEnumerable<Personnage> personnages);

    /// <summary>
    /// Exporte les templates au format PML
    /// </summary>
    Task<byte[]> ExporterTemplatesPmlAsync(IEnumerable<Template> templates);

    /// <summary>
    /// Exporte les capacités au format PML
    /// </summary>
    Task<byte[]> ExporterCapacitesPmlAsync(IEnumerable<Capacite> capacites);

    /// <summary>
    /// Obtient la date du dernier export
    /// </summary>
    Task<DateTime?> GetLastExportDate();
}
