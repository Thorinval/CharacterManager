using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service pour exporter les données au format PML (XML personnalisé)
/// Extension .pml pour les fichiers d'export
/// Supporte les sections : HistoriqueClassements, inventaire, template, capacités
/// </summary>
public class PmlExportService(ApplicationDbContext context) : PmlServiceBase(context)
{
    /// <summary>
    /// Exporte les données sélectionnées au format PML
    /// </summary>
    public async Task<byte[]> ExportPmlAsync(PmlExportOptions? options = null)
    {
        // Rétro-compatibilité: créer des options par défaut
        options ??= new PmlExportOptions(
            PmlExportOptions.EXPORT_TYPE_INVENTORY,
            PmlExportOptions.EXPORT_TYPE_TEMPLATES,
            PmlExportOptions.EXPORT_TYPE_BEST_SQUAD
        );

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(AppConstants.XmlElements.CharacterManagerPML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            // Export inventaire
            await ExporterInventairePmlAsync(options, writer);

            // Export templates
            await ExporterTemplatesPmlAsync(options, writer);

            // Export meilleure escouade
            await ExporterBestSquadPmlAsync(options, writer);

            // Export historiques de classement
            await ExporterHistoriquesClassementPmlAsync(options, writer);

            // Export historiques de ligue
            await ExporterHistoriquesLiguePmlAsync(options, writer);

            // Export capacités
            await ExporterCapacitesPmlAsync(options, writer);

            writer.WriteEndDocument();
        }

        var bytes = memoryStream.ToArray();
        await SaveLastExportDate();
        return bytes;
    }

    private async Task ExporterHistoriquesLiguePmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY))
        {
            var historiquesLigue = await _context.HistoriquesLigue
                .OrderByDescending(h => h.DateMontee)
                .Take(100)
                .ToListAsync();

            foreach (var historiqueLigue in historiquesLigue)
            {
                writer.WriteStartElement(AppConstants.XmlElements.HistoriqueLigue);
                writer.WriteElementString(AppConstants.XmlElements.DateMontee, historiqueLigue.DateMontee.ToString("yyyy-MM-dd"));
                writer.WriteElementString(AppConstants.XmlElements.Ligue, historiqueLigue.Ligue.ToString());
                if (!string.IsNullOrWhiteSpace(historiqueLigue.Notes))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Notes, historiqueLigue.Notes);
                }
                writer.WriteEndElement();
            }
        }
    }

    private async Task ExporterCapacitesPmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES))
        {
            var capacites = await _context.Capacites.ToListAsync();
            if (capacites.Count > 0)
            {
                writer.WriteStartElement("Capacites");
                WriteCapacites(writer, capacites);
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void WriteCapacites(XmlWriter writer, IEnumerable<Capacite> capacites)
    {
        foreach (var capacite in capacites)
        {
            writer.WriteStartElement("Capacite");
            if (capacite.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, capacite.Id.ToString());
            }
            writer.WriteElementString("Nom", capacite.Nom);
            writer.WriteElementString("Description", capacite.Description ?? "");
            writer.WriteElementString("Icon", capacite.Icon ?? "");
            writer.WriteEndElement();
        }
    }

    private static void ExportListClassements(XmlWriter writer, List<Classement> listClassements)
    {
        if (listClassements.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Classements);
            foreach (var classement in listClassements)
            {
                writer.WriteStartElement(AppConstants.XmlElements.ClassementItem);
                writer.WriteElementString(AppConstants.XmlElements.Nom, classement.Nom);
                writer.WriteElementString(AppConstants.XmlElements.TypeClassement, classement.Type.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Valeur, classement.Valeur.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private async Task ExporterHistoriquesClassementPmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES))
        {
            // Export historiques de classement (version structurée complète)
            var historiquesClassement = await _context.HistoriquesClassement
                .Include(h => h.Mercenaires)
                .Include(h => h.Commandant)
                .Include(h => h.Androides)
                .Include(h => h.Pieces)
                .Include(h => h.Classements)
                .OrderByDescending(h => h.DateEnregistrement)
                .Take(50)
                .ToListAsync();

            foreach (var historiqueClassement in historiquesClassement)
            {
                ExportHistoriqueClassementElement(writer, historiqueClassement);
            }
        }
    }

    private static void ExportHistoriqueClassementElement(XmlWriter writer, HistoriqueClassement historiqueClassement)
    {
        writer.WriteStartElement(AppConstants.XmlElements.HistoriqueClassement);
        writer.WriteElementString(AppConstants.XmlElements.DateEnregistrement, historiqueClassement.DateEnregistrement.ToString("yyyy-MM-dd"));
        writer.WriteElementString(AppConstants.XmlElements.Ligue, historiqueClassement.Ligue.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Score, historiqueClassement.Score.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceTotal, historiqueClassement.PuissanceTotale.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceCommandant, historiqueClassement.PuissanceCommandant.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceMercenaires, historiqueClassement.PuissanceMercenaires.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceLucie, historiqueClassement.PuissanceLucie.ToString());

        // Export des classements
        ExportListClassements(writer, historiqueClassement.Classements);

        // Export des mercenaires
        ExportListMercenaires(writer, historiqueClassement.Mercenaires);

        // Export du commandant
        ExportHistoriqueCommandantElement(writer, historiqueClassement.Commandant);

        // Export des androïdes
        ExportListAndroides(writer, historiqueClassement.Androides);

        // Export des pièces
        ExportHistoriquePiecesElement(writer, historiqueClassement.Pieces);

        writer.WriteEndElement();
    }

    private static void ExportHistoriqueCommandantElement(XmlWriter writer, PersonnageClassement? commandant)
    {
        if (commandant != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Commandant);
            if (commandant.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, commandant.Id.ToString());
            }
            WritePersonnageClassementData(writer, commandant);
            writer.WriteEndElement();
        }
    }

    private static void ExportHistoriquePiecesElement(XmlWriter writer, List<PieceHistorique> pieces)
    {
        if (pieces.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Pieces);
            foreach (var piece in pieces)
            {
                WriteLuciePieceData(writer, piece, avecBonus: false);
            }
            writer.WriteEndElement();
        }
    }

    private static void ExportListAndroides(XmlWriter writer, List<PersonnageClassement> listAndroides)
    {
        if (listAndroides.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Androides);
            foreach (var androide in listAndroides)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                if (androide.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, androide.Id.ToString());
                }
                WritePersonnageClassementData(writer, androide);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private static void ExportListMercenaires(XmlWriter writer, List<PersonnageClassement> listMercenaires)
    {
        if (listMercenaires.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Mercenaires);
            foreach (var mercenaire in listMercenaires)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                if (mercenaire.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, mercenaire.Id.ToString());
                }
                WritePersonnageClassementData(writer, mercenaire);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private async Task ExporterBestSquadPmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD))
        {
            writer.WriteStartElement(AppConstants.XmlElements.MeilleurEscouade);

            await ExportBestSquadMercenairesAsync(writer);
            await ExportBestSquadCommandantAsync(writer);
            await ExportBestSquadAndroidesAsync(writer);

            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadMercenairesAsync(XmlWriter writer)
    {
        var topMercenaires = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire)
            .OrderByDescending(p => p.Puissance)
            .Take(10)
            .ToListAsync();

        foreach (var merc in topMercenaires)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Mercenaire);
            if (merc.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, merc.Id.ToString());
            }
            WritePersonnageData(writer, merc);
            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadCommandantAsync(XmlWriter writer)
    {
        var topCommandant = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant)
            .OrderByDescending(p => p.Puissance)
            .FirstOrDefaultAsync();

        if (topCommandant != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Commandant);
            if (topCommandant.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, topCommandant.Id.ToString());
            }
            WritePersonnageData(writer, topCommandant);
            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadAndroidesAsync(XmlWriter writer)
    {
        var topAndroides = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androide)
            .OrderByDescending(p => p.Puissance)
            .Take(5)
            .ToListAsync();

        foreach (var android in topAndroides)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Androide);
            if (android.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, android.Id.ToString());
            }
            WritePersonnageData(writer, android);
            writer.WriteEndElement();
        }
    }

    private async Task ExporterTemplatesPmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES))
        {
            var templates = await _context.Templates.ToListAsync();
            foreach (var template in templates)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Template);
                writer.WriteElementString(AppConstants.XmlElements.Nom, template.Nom);
                writer.WriteElementString(AppConstants.XmlElements.Description, template.Description ?? "");

                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = await _context.Personnages
                        .Include(p => p.Capacites)
                        .FirstOrDefaultAsync(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                        if (personnage.Id > 0)
                        {
                            writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
                        }
                        WritePersonnageData(writer, personnage, isTemplate: true);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }
    }

    private async Task ExporterInventairePmlAsync(PmlExportOptions options, XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY))
        {
            writer.WriteStartElement(AppConstants.XmlElements.Inventaire);

            await WritePersonnageDatas(writer);

            // Export Lucie House
            await WriteLucieHouseDatas(writer);
        }
    }

    private async Task WriteLucieHouseDatas(XmlWriter writer)
    {
        var lucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
        if (lucieHouse != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
            writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

            foreach (var piece in lucieHouse.Pieces)
            {
                WriteLuciePieceData(writer, piece);
            }

            writer.WriteEndElement();
        }
    }

    private async Task WritePersonnageDatas(XmlWriter writer)
    {
        var personnages = await _context.Personnages
            .Include(static p => p.Capacites)
            .ToListAsync();
        WritePersonnagesDatas(personnages, writer);
    }

    /// <summary>
    /// Helper method to write personnage data to XML
    /// </summary>
    private static void WritePersonnageData(XmlWriter writer, Personnage personnage, bool isTemplate = false)
    {
        writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.HasRelation, personnage.HasRelation.ToString());
        writer.WriteElementString(AppConstants.XmlElements.NivRelation, personnage.NivRelation.ToString());

        if (personnage.Capacites?.Count > 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Capacites);
            foreach (var capacite in personnage.Capacites)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Capacite);
                if (capacite.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, capacite.Id.ToString());
                }
                writer.WriteElementString(AppConstants.XmlElements.Nom, capacite.Nom);

                if (!string.IsNullOrWhiteSpace(capacite.Description))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Description, capacite.Description);
                }

                if (!string.IsNullOrWhiteSpace(capacite.Icon))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Icon, capacite.Icon);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        if (!isTemplate)
        {
            writer.WriteElementString(AppConstants.XmlElements.PA, personnage.PA.ToString());
            writer.WriteElementString(AppConstants.XmlElements.PV, personnage.PV.ToString());

            writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Role, personnage.Role.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Faction, personnage.Faction.ToString());
            writer.WriteElementString(AppConstants.XmlElements.TypeAttaque, personnage.TypeAttaque.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Selectionne, personnage.Selectionne.ToString());
        }
    }

    /// <summary>
    /// Helper method to write PersonnageClassement data to XML
    /// </summary>
    private static void WritePersonnageClassementData(XmlWriter writer, PersonnageClassement personnage)
    {
        writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
    }

    /// <summary>
    /// Exporte les données d'inventaire au format PML
    /// </summary>
    public Task<byte[]> ExporterInventairePmlAsync(IEnumerable<Personnage> personnages)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(AppConstants.XmlElements.InventairePML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            writer.WriteStartElement(AppConstants.XmlElements.Inventaire);

            WritePersonnagesDatas(personnages, writer);

            // Export Lucie House as part of the inventory payload (no extra checkbox/UI toggle)
            var lucieHouse = _context.LucieHouses.Include(l => l.Pieces).FirstOrDefault();
            if (lucieHouse != null)
            {
                writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
                writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

                foreach (var piece in lucieHouse.Pieces)
                {
                    WriteLuciePieceData(writer, piece);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    private static void WriteLuciePieceData(XmlWriter writer, Piece piece, bool avecBonus = true)
    {
        writer.WriteStartElement(AppConstants.XmlElements.Piece);
        if (piece.Id > 0)
        {
            writer.WriteAttributeString(AppConstants.XmlElements.Id, piece.Id.ToString());
        }
        writer.WriteElementString(AppConstants.XmlElements.Nom, piece.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Niveau, piece.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceTactique, piece.AspectsTactiques.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceStrategique, piece.AspectsStrategiques.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Selectionne, piece.Selectionnee.ToString());

        if (avecBonus)
        {
            if (piece.AspectsTactiques.Bonus.Count > 0)
            {
                writer.WriteStartElement(AppConstants.XmlElements.BonusTactiques);
                foreach (var bonus in piece.AspectsTactiques.Bonus)
                {
                    writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                }
                writer.WriteEndElement();
            }

            if (piece.AspectsStrategiques.Bonus.Count > 0)
            {
                writer.WriteStartElement(AppConstants.XmlElements.BonusStrategiques);
                foreach (var bonus in piece.AspectsStrategiques.Bonus)
                {
                    writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                }
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void WritePersonnagesDatas(IEnumerable<Personnage> personnages, XmlWriter writer)
    {
        foreach (var personnage in personnages)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Personnage);
            if (personnage.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
            }
            WritePersonnageData(writer, personnage);

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// Exporte les templates au format PML
    /// </summary>
    public Task<byte[]> ExporterTemplatesPmlAsync(IEnumerable<Template> templates)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(AppConstants.XmlElements.TemplatesPML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            foreach (var template in templates)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Template);
                writer.WriteElementString(AppConstants.XmlElements.Nom, template.Nom);
                writer.WriteElementString(AppConstants.XmlElements.Description, template.Description ?? "");

                // Récupérer les personnages du template via les IDs stockés
                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = _context.Personnages
                        .Include(p => p.Capacites)
                        .FirstOrDefault(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                        if (personnage.Id > 0)
                        {
                            writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
                        }
                        WritePersonnageData(writer, personnage, isTemplate: true);
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    /// <summary>
    /// Exporte uniquement les capacités au format PML
    /// </summary>
    public Task<byte[]> ExporterCapacitesPmlAsync(IEnumerable<Capacite> capacites)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("CapacitesPML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            writer.WriteStartElement("Capacites");
            WriteCapacites(writer, capacites);
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }
}
