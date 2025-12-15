namespace CharacterManager.Server.Services;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service de localisation pour gérer les traductions multi-langues
/// </summary>
public class LocalizationService
{
    private readonly HttpClient _httpClient;
    private readonly string _basePath = "i18n";
    private readonly Dictionary<string, Dictionary<string, object>> _cache = new();
    private readonly ILogger<LocalizationService> _logger;

    public LocalizationService(HttpClient httpClient, ILogger<LocalizationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Charge les ressources de traduction pour une langue donnée
    /// </summary>
    public async Task<Dictionary<string, object>> LoadLanguageAsync(string languageCode)
    {
        // Vérifier si déjà en cache
        if (_cache.ContainsKey(languageCode))
        {
            return _cache[languageCode];
        }

        try
        {
            var fileName = $"{languageCode}.json";
            var filePath = Path.Combine("wwwroot", _basePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Fichier de langue non trouvé: {filePath}");
                return new Dictionary<string, object>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resources = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

            if (resources != null)
            {
                _cache[languageCode] = resources;
                return resources;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors du chargement de la langue {languageCode}: {ex.Message}");
        }

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Récupère une chaîne de traduction par sa clé
    /// </summary>
    public string GetString(Dictionary<string, object> resources, string key)
    {
        var keys = key.Split('.');
        object current = resources;

        foreach (var k in keys)
        {
            if (current is JsonElement element)
            {
                if (element.TryGetProperty(k, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return key; // Retourner la clé si non trouvée
                }
            }
            else if (current is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(k, out var value))
                {
                    current = value;
                }
                else
                {
                    return key;
                }
            }
            else
            {
                return key;
            }
        }

        // Convertir le résultat final en string
        if (current is JsonElement finalElement)
        {
            return finalElement.GetString() ?? key;
        }

        return current?.ToString() ?? key;
    }

    /// <summary>
    /// Obtient les langues disponibles
    /// </summary>
    public List<LanguageOption> GetAvailableLanguages()
    {
        return new List<LanguageOption>
        {
            new LanguageOption { Code = "fr", Name = "Français" },
            new LanguageOption { Code = "en", Name = "English" }
        };
    }

    /// <summary>
    /// Clearing le cache des traductions
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}

/// <summary>
/// Option de langue disponible
/// </summary>
public class LanguageOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
