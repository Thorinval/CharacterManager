namespace CharacterManager.Server.Services;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

/// <summary>
/// Service client pour fournir les traductions aux composants Blazor
/// </summary>
public class ClientLocalizationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientLocalizationService> _logger;
    private Dictionary<string, object>? _currentResources;
    private string _currentLanguage = "fr";

    public ClientLocalizationService(IJSRuntime jsRuntime, ILogger<ClientLocalizationService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initialise le service avec les ressources de traduction
    /// </summary>
    public async Task InitializeAsync(string languageCode)
    {
        _currentLanguage = languageCode;
        await LoadResourcesAsync(languageCode);
    }

    /// <summary>
    /// Charge les ressources de traduction depuis le fichier JSON
    /// </summary>
    private async Task LoadResourcesAsync(string languageCode)
    {
        try
        {
            var jsonContent = await _jsRuntime.InvokeAsync<string>(
                "fetch",
                $"i18n/{languageCode}.json"
            );

            // Pour Blazor WebAssembly/InteractiveServer, on utilise une approche différente
            // On fait un appel HTTP au serveur pour récupérer le fichier JSON
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"i18n/{languageCode}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _currentResources = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors du chargement des ressources: {ex.Message}");
            _currentResources = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Récupère une chaîne de traduction par sa clé
    /// Utilise la notation pointée : "section.key"
    /// </summary>
    public string T(string key)
    {
        if (_currentResources == null)
        {
            return key;
        }

        var keys = key.Split('.');
        object? current = _currentResources;

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
                    return key;
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
    /// Retourne la langue actuelle
    /// </summary>
    public string GetCurrentLanguage() => _currentLanguage;

    /// <summary>
    /// Change de langue
    /// </summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        _currentLanguage = languageCode;
        await LoadResourcesAsync(languageCode);
    }

    /// <summary>
    /// Retourne les ressources complètes (pour debug ou accès direct)
    /// </summary>
    public Dictionary<string, object>? GetResources() => _currentResources;
}
