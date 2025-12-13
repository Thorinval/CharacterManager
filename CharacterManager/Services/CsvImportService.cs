using CharacterManager.Data;
using CharacterManager.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CharacterManager.Services;

public class CsvImportService
{
    private readonly PersonnageService _personnageService;

    public CsvImportService(PersonnageService personnageService)
    {
        _personnageService = personnageService;
    }

    public async Task<ImportResult> ImportCsvAsync(Stream csvStream)
    {
        var result = new ImportResult();
        var lines = new List<string>();

        try
        {
            using (var reader = new StreamReader(csvStream))
            {
                string line;
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
            existing.PAMax = Math.Max(existing.PAMax, nouveauPersonnage.PA);
            existing.PV = nouveauPersonnage.PV;
            existing.PVMax = Math.Max(existing.PVMax, nouveauPersonnage.PV);
            existing.Niveau = nouveauPersonnage.Niveau;
            existing.Rang = nouveauPersonnage.Rang;
            existing.Role = nouveauPersonnage.Role;
            existing.Faction = nouveauPersonnage.Faction;
            existing.Selectionne = nouveauPersonnage.Selectionne;

            _personnageService.Update(existing);
        }
        else
        {
            // Create new
            _personnageService.Add(nouveauPersonnage);
        }
    }

    private Personnage? ParsePersonnage(List<string> values, Dictionary<string, int> mapping)
    {
        var personnage = new Personnage();

        // Nom (required)
        if (!mapping.ContainsKey("Personnage") || mapping["Personnage"] >= values.Count)
            return null;

        personnage.Nom = values[mapping["Personnage"]].Trim();
        if (string.IsNullOrEmpty(personnage.Nom))
            return null;

        // Rareté
        if (mapping.ContainsKey("Rareté") && mapping["Rareté"] < values.Count)
        {
            var raretéStr = values[mapping["Rareté"]].Trim();
            if (Enum.TryParse<Rareté>(raretéStr, true, out var rarete))
                personnage.Rarete = rarete;
            else
                personnage.Rarete = Rareté.R; // Default
        }

        // Type
        if (mapping.ContainsKey("Type") && mapping["Type"] < values.Count)
        {
            var typeStr = values[mapping["Type"]].Trim();
            if (typeStr.Contains("Mercenaire", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Mercenaire;
            else if (typeStr.Contains("Commandant", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Commandant;
            else if (typeStr.Contains("Androïde", StringComparison.OrdinalIgnoreCase) || 
                     typeStr.Contains("Android", StringComparison.OrdinalIgnoreCase))
                personnage.Type = TypePersonnage.Androide;
            else
                personnage.Type = TypePersonnage.Mercenaire; // Default
        }

        // Puissance
        if (mapping.ContainsKey("Puissance") && mapping["Puissance"] < values.Count)
        {
            if (int.TryParse(values[mapping["Puissance"]].Trim(), out var puissance))
                personnage.Puissance = puissance;
        }

        // PA
        if (mapping.ContainsKey("PA") && mapping["PA"] < values.Count)
        {
            if (int.TryParse(values[mapping["PA"]].Trim(), out var pa))
                personnage.PA = pa;
        }
        personnage.PAMax = Math.Max(personnage.PA, 10);

        // PV
        if (mapping.ContainsKey("PV") && mapping["PV"] < values.Count)
        {
            if (int.TryParse(values[mapping["PV"]].Trim(), out var pv))
                personnage.PV = pv;
        }
        personnage.PVMax = Math.Max(personnage.PV, 10);

        // Santé (si disponible)
        if (mapping.ContainsKey("Sante") && mapping["Sante"] < values.Count)
        {
            if (int.TryParse(values[mapping["Sante"]].Trim(), out var sante))
                personnage.Sante = sante;
        }
        personnage.SanteMax = Math.Max(personnage.Sante, 10);

        // Niveau
        if (mapping.ContainsKey("Niveau") && mapping["Niveau"] < values.Count)
        {
            if (int.TryParse(values[mapping["Niveau"]].Trim(), out var niveau))
                personnage.Niveau = niveau;
        }

        // Rang
        if (mapping.ContainsKey("Rang") && mapping["Rang"] < values.Count)
        {
            if (int.TryParse(values[mapping["Rang"]].Trim(), out var rang))
                personnage.Rang = rang;
        }

        // Role
        if (mapping.ContainsKey("Role") && mapping["Role"] < values.Count)
        {
            var roleStr = values[mapping["Role"]].Trim();
            if (Enum.TryParse<Role>(roleStr, true, out var role))
                personnage.Role = role;
            else
                personnage.Role = Role.Sentinelle; // Default
        }

        // Faction
        if (mapping.ContainsKey("Faction") && mapping["Faction"] < values.Count)
        {
            var factionStr = values[mapping["Faction"]].Trim();
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
                personnage.Faction = Faction.Syndicat; // Default
        }

        // Sélection (Oui/Non)
        if (mapping.ContainsKey("Selection") && mapping["Selection"] < values.Count)
        {
            var selectionStr = values[mapping["Selection"]].Trim().ToLower();
            personnage.Selectionne = selectionStr == "oui" || selectionStr == "yes";
        }

        // Default values for optional fields
        personnage.ImageUrl = $"https://via.placeholder.com/150?text={Uri.EscapeDataString(personnage.Nom)}";
        personnage.Description = $"Personnage {personnage.Nom} importé";
        personnage.Localisation = "Non assignée";

        return personnage;
    }

    private Dictionary<string, int> MapColumns(List<string> headers)
    {
        var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim().ToLower();
            
            if (header.Contains("personnage") || header.Contains("nom"))
                mapping["Personnage"] = i;
            else if (header.Contains("rareté") || header.Contains("rarete"))
                mapping["Rareté"] = i;
            else if (header.Contains("type"))
                mapping["Type"] = i;
            else if (header.Contains("puissance"))
                mapping["Puissance"] = i;
            else if (header.Contains("pa"))
                mapping["PA"] = i;
            else if (header.Contains("pv"))
                mapping["PV"] = i;
            else if (header.Contains("sante"))
                mapping["Sante"] = i;
            else if (header.Contains("niveau"))
                mapping["Niveau"] = i;
            else if (header.Contains("rang"))
                mapping["Rang"] = i;
            else if (header.Contains("role"))
                mapping["Role"] = i;
            else if (header.Contains("faction"))
                mapping["Faction"] = i;
            else if (header.Contains("selection") || header.Contains("selectionne"))
                mapping["Selection"] = i;
        }

        return mapping;
    }

    private List<string> ParseCsvLine(string? line)
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
}

public class ImportResult
{
    public bool IsSuccess { get; set; }
    public int SuccessCount { get; set; }
    public string? Error { get; set; }
    public List<string> Errors { get; set; } = new();
}
