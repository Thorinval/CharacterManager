using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CharacterManager.Server.Services;

public class CsvImportService(PersonnageService personnageService, ApplicationDbContext context)
{
    private readonly PersonnageService _personnageService = personnageService;
    private readonly ApplicationDbContext _context = context;

    public async Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName = "")
    {
        var result = new ImportResult();
        var lines = new List<string>();

        try
        {
            using (var reader = new StreamReader(csvStream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            if (lines.Count < 2)
            {
                result.Error = "Le fichier CSV est vide ou n'a pas de données";
                return result;
            }

            // Skip header
            var headerLine = lines[0];
            var headers = ParseCsvLine(headerLine);
            var columnMapping = MapColumns(headers);

            // Process data rows
            for (int i = 1; i < lines.Count; i++)
            {
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    
                    if (values.Count == 0 || string.IsNullOrEmpty(values[0]))
                        continue; // Skip empty lines

                    var personnage = ParsePersonnage(values, columnMapping);
                    if (personnage != null)
                    {
                        ImportOrUpdatePersonnage(personnage);
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Ligne {i + 1}: {ex.Message}");
                }
            }

            result.IsSuccess = true;
            
            // Enregistrer le nom du fichier importé
            if (!string.IsNullOrEmpty(fileName))
            {
                await SaveLastImportedFileName(fileName);
            }
        }
        catch (Exception ex)
        {
            result.Error = $"Erreur lors de la lecture du fichier: {ex.Message}";
            result.IsSuccess = false;
        }

        return result;
    }

    private void ImportOrUpdatePersonnage(Personnage nouveauPersonnage)
    {
        var existing = _personnageService.GetAll()
            .FirstOrDefault(p => p.Nom.Equals(nouveauPersonnage.Nom, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            // Update existing
            existing.Rarete = nouveauPersonnage.Rarete;
            existing.Type = nouveauPersonnage.Type;
            existing.Puissance = nouveauPersonnage.Puissance;
            existing.PA = nouveauPersonnage.PA;
            existing.PV = nouveauPersonnage.PV;
            existing.Niveau = nouveauPersonnage.Niveau;
            existing.Rang = nouveauPersonnage.Rang;
            existing.Role = nouveauPersonnage.Role;
            existing.Faction = nouveauPersonnage.Faction;
            existing.Selectionne = nouveauPersonnage.Selectionne;
            existing.TypeAttaque = nouveauPersonnage.TypeAttaque;

            _personnageService.Update(existing);
        }
        else
        {
            // Create new
            _personnageService.Add(nouveauPersonnage);
        }
    }

    private static Personnage? ParsePersonnage(List<string> values, Dictionary<string, int> mapping)
    {
        var personnage = new Personnage();

        // Nom (required)
        if (!mapping.TryGetValue("Personnage", out int nomIndex) || nomIndex >= values.Count)
            return null;

        personnage.Nom = values[nomIndex].Trim();
        if (string.IsNullOrEmpty(personnage.Nom))
            return null;

        // Rareté
        if (mapping.TryGetValue("Rarete", out int rareteIndex) && rareteIndex < values.Count)
        {
            var rareteStr = values[rareteIndex].Trim();
            if (rareteStr.Equals("SSR", StringComparison.OrdinalIgnoreCase))
                personnage.Rarete = Rarete.SSR;
            else if (rareteStr.Equals("SR", StringComparison.OrdinalIgnoreCase))
                personnage.Rarete = Rarete.SR;
            else if (rareteStr.Equals("R", StringComparison.OrdinalIgnoreCase))
                personnage.Rarete = Rarete.R;
            else
                personnage.Rarete = Rarete.Inconnu; // Default
        }

        // Type
        if (mapping.TryGetValue("Categorie", out int typeIndex) && typeIndex < values.Count)
        {
            var typeStr = values[typeIndex].Trim();
            if (typeStr.Contains("Mercenaire", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Mercenaire;
            else if (typeStr.Contains("Commandant", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Commandant;
            else if (typeStr.Contains("Androïde", StringComparison.OrdinalIgnoreCase) || 
                     typeStr.Contains("Androide", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Androïde;
            else
                personnage.Type = TypePersonnage.Inconnu; // Default
        }

        // Puissance
        if (mapping.TryGetValue("Puissance", out int puissanceIndex) && puissanceIndex < values.Count)
        {
            if (int.TryParse(values[puissanceIndex].Trim(), out var puissance))
                personnage.Puissance = puissance;
        }

        // PA
        if (mapping.TryGetValue("PA", out int paIndex) && paIndex < values.Count)
        {
            if (int.TryParse(values[paIndex].Trim(), out var pa))
                personnage.PA = pa;
        }

        // PV
        if (mapping.TryGetValue("PV", out int pvIndex) && pvIndex < values.Count)
        {
            if (int.TryParse(values[pvIndex].Trim(), out var pv))
                personnage.PV = pv;
        }

        // Niveau
        if (mapping.TryGetValue("Niveau", out int niveauIndex) && niveauIndex < values.Count)
        {
            if (int.TryParse(values[niveauIndex].Trim(), out var niveau))
                personnage.Niveau = niveau;
        }

        // Rang
        if (mapping.TryGetValue("Rang", out int rangIndex) && rangIndex < values.Count)
        {
            if (int.TryParse(values[rangIndex].Trim(), out var rang))
                personnage.Rang = rang;
        }

        // Role
        if (mapping.TryGetValue("Role", out int roleIndex) && roleIndex < values.Count)
        {
            var roleStr = values[roleIndex].Trim();
            if (Enum.TryParse<Role>(roleStr, true, out var role))
                personnage.Role = role;
            else
                personnage.Role = Role.Inconnu; // Default
        }

        // Faction
        if (mapping.TryGetValue("Faction", out int factionIndex) && factionIndex < values.Count)
        {
            var factionStr = values[factionIndex].Trim();
            if (Enum.TryParse<Faction>(factionStr, true, out var faction))
                personnage.Faction = faction;
            else if (factionStr.Contains("Syndicat", StringComparison.OrdinalIgnoreCase))
                personnage.Faction = Faction.Syndicat;
            else if (factionStr.Contains("Pacificateurs", StringComparison.OrdinalIgnoreCase))
                personnage.Faction = Faction.Pacificateurs;
            else if (factionStr.Contains("Hommes libres", StringComparison.OrdinalIgnoreCase) || 
                     factionStr.Contains("libres", StringComparison.OrdinalIgnoreCase))
                personnage.Faction = Faction.HommesLibres;
            else if (factionStr.Contains("Commandant", StringComparison.OrdinalIgnoreCase))
                personnage.Faction = Faction.Syndicat; // Default
            else
                personnage.Faction = Faction.Inconnu; // Default
        }

        // Sélection (Oui/Non)
        if (mapping.TryGetValue("Selection", out int selectionIndex) && selectionIndex < values.Count)
        {
            var selectionStr = values[selectionIndex].Trim().ToLower();
            personnage.Selectionne = selectionStr == "oui" || selectionStr == "yes";
        }

        // TypeAttaque (Mêlée, Distance, Androïde)
        if (mapping.TryGetValue("TypeAttaque", out int actionIndex) && actionIndex < values.Count)
        {
            var actionStr = values[actionIndex].Trim();
            if (Enum.TryParse<TypeAttaque>(actionStr, true, out var typeAttaque))
                personnage.TypeAttaque = typeAttaque;
            else if (actionStr.Contains("Mêlée", StringComparison.OrdinalIgnoreCase))
                personnage.TypeAttaque = TypeAttaque.Mêlée;
            else if (actionStr.Contains("Distance", StringComparison.OrdinalIgnoreCase))
                personnage.TypeAttaque = TypeAttaque.Distance;
            else if (actionStr.Contains("Androïde", StringComparison.OrdinalIgnoreCase))
                personnage.TypeAttaque = TypeAttaque.Androïde;
            else
                personnage.TypeAttaque = TypeAttaque.Inconnu; // Default
        }

        string nomLower = personnage.Nom.ToLower();
        personnage.ImageUrlDetail = $"/images/personnages/{nomLower}.png";
        personnage.ImageUrlPreview = $"/images/personnages/{nomLower}_small_portrait.png";
        personnage.ImageUrlSelected = $"/images/personnages/{nomLower}_small_select.png";
        personnage.Description = $"Personnage {personnage.Nom} importé";

        return personnage;
    }

    private static Dictionary<string, int> MapColumns(List<string> headers)
    {
        var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim().ToLower();
            
            if (header.Contains("personnage") || header.Contains("nom"))
                mapping["Personnage"] = i;
            else if (header.Contains("rareté") || header.Contains("rarete"))
                mapping["Rarete"] = i;
            else if (header.Contains("categorie"))
                mapping["Categorie"] = i;
            else if (header.Contains("puissance"))
                mapping["Puissance"] = i;
            else if (header.Contains("pa"))
                mapping["PA"] = i;
            else if (header.Contains("pv"))
                mapping["PV"] = i;
            else if (header.Contains("niveau"))
                mapping["Niveau"] = i;
            else if (header.Contains("rang"))
                mapping["Rang"] = i;
            else if (header.Contains("role"))
                mapping["Role"] = i;
            else if (header.Contains("faction"))
                mapping["Faction"] = i;
            else if (header.Contains("selection"))
                mapping["Selection"] = i;
            else if (header.Contains("typeattaque"))
                mapping["TypeAttaque"] = i;
        }

        return mapping;
    }

    private static List<string> ParseCsvLine(string? line)
    {
        var values = new List<string>();
        if (line == null) return values;
        
        var currentValue = new System.Text.StringBuilder();
        bool insideQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == ';' && !insideQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values;
    }

    public async Task<string?> GetLastImportedFileName()
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync();
        return settings?.LastImportedFileName;
    }

    private async Task SaveLastImportedFileName(string fileName)
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            _context.AppSettings.Add(settings);
        }
        
        settings.LastImportedFileName = fileName;
        settings.LastImportedDate = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public Task<byte[]> ExportToCsvAsync(IEnumerable<Personnage> personnages)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("Personnage;Rarete;Categorie;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction");
        
        // Data rows
        foreach (var p in personnages)
        {
            sb.Append($"{EscapeCsvValue(p.Nom)};");
            sb.Append($"{p.Rarete};");
            sb.Append($"{p.Type};");
            sb.Append($"{p.Puissance};");
            sb.Append($"{p.PA};");
            sb.Append($"{p.PV};");
            sb.Append($"{p.TypeAttaque};");
            sb.Append($"{p.Role};");
            sb.Append($"{p.Niveau};");
            sb.Append($"{p.Rang};");
            sb.Append($"{(p.Selectionne ? "Oui" : "Non")};");
            sb.AppendLine($"{p.Faction}");
        }
        
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private string EscapeCsvValue(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

public class ImportResult
{
    public bool IsSuccess { get; set; }
    public int SuccessCount { get; set; }
    public string? Error { get; set; }
    public List<string> Errors { get; set; } = [];
}
