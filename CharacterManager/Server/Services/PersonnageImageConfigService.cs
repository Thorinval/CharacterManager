using CharacterManager.Server.Models;
using System.Text.Json;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service de gestion du fichier de configuration des images personnages
/// </summary>
public class PersonnageImageConfigService
{
    private readonly string _configFilePath;
    private readonly string _personnagesDirectoryPath;
    private PersonnagesImagesConfiguration? _configuration;

    public PersonnageImageConfigService()
    {
        // Utiliser wwwroot pour stocker la config
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;
        
        if (!string.IsNullOrEmpty(projectRoot) && Directory.Exists(Path.Combine(projectRoot, "wwwroot")))
        {
            _configFilePath = Path.Combine(projectRoot, "wwwroot", "personnages-config.json");
            _personnagesDirectoryPath = Path.Combine(projectRoot, "wwwroot", "images", "personnages");
        }
        else
        {
            _configFilePath = Path.Combine(baseDir, "wwwroot", "personnages-config.json");
            _personnagesDirectoryPath = Path.Combine(baseDir, "wwwroot", "images", "personnages");
        }

        // S'assurer que les répertoires existent pour éviter les erreurs pendant les tests ou en CI
        var configDir = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrWhiteSpace(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        Directory.CreateDirectory(_personnagesDirectoryPath);
    }

    /// <summary>
    /// Charge la configuration depuis le fichier JSON
    /// </summary>
    public PersonnagesImagesConfiguration LoadConfiguration()
    {
        if (_configuration != null)
            return _configuration;

        if (!File.Exists(_configFilePath))
        {
            _configuration = new PersonnagesImagesConfiguration();
            InitializeConfiguration();
            SaveConfiguration(_configuration);
            return _configuration;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            _configuration = JsonSerializer.Deserialize<PersonnagesImagesConfiguration>(json) 
                ?? new PersonnagesImagesConfiguration();
            
            // Synchroniser avec les fichiers présents dans le dossier
            SynchronizeWithFileSystem();
            
            return _configuration;
        }
        catch
        {
            _configuration = new PersonnagesImagesConfiguration();
            return _configuration;
        }
    }

    /// <summary>
    /// Sauvegarde la configuration dans le fichier JSON
    /// </summary>
    public void SaveConfiguration(PersonnagesImagesConfiguration config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_configFilePath, json);
            _configuration = config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Config] Erreur lors de la sauvegarde: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour l'attribut IsAdult d'une image spécifique
    /// </summary>
    public void UpdateImageAdultStatus(string cheminImage, bool isAdult)
    {
        var config = LoadConfiguration();
        var image = config.Images.FirstOrDefault(i => i.CheminImage == cheminImage);
        
        if (image != null)
        {
            image.IsAdult = isAdult;
            SaveConfiguration(config);
        }
    }

    /// <summary>
    /// Retourne le chemin à afficher en fonction du mode adulte
    /// </summary>
    public string GetDisplayPath(string cheminImage, bool isAdultModeEnabled)
    {
        var config = LoadConfiguration();
        var image = config.Images.FirstOrDefault(i => i.CheminImage == cheminImage);
        
        if (image != null && image.IsAdult && !isAdultModeEnabled)
        {
            return "/images/interface/default_portrait.png";
        }
        
        return cheminImage;
    }

    /// <summary>
    /// Initialise la configuration en scannant le répertoire des personnages
    /// </summary>
    private void InitializeConfiguration()
    {
        if (_configuration == null || !Directory.Exists(_personnagesDirectoryPath))
            return;

        var files = Directory.GetFiles(_personnagesDirectoryPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) 
                     || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) 
                     || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var relativePath = "/images/personnages/" + fileName;
            
            if (!_configuration.Images.Any(i => i.CheminImage == relativePath))
            {
                _configuration.Images.Add(new PersonnageImageConfig
                {
                    CheminImage = relativePath,
                    NomFichier = fileName,
                    IsAdult = false
                });
            }
        }
    }

    /// <summary>
    /// Synchronise la configuration avec les fichiers actuellement présents
    /// </summary>
    private void SynchronizeWithFileSystem()
    {
        if (_configuration == null || !Directory.Exists(_personnagesDirectoryPath))
            return;

        var files = Directory.GetFiles(_personnagesDirectoryPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) 
                     || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) 
                     || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            .Select(f => "/images/personnages/" + Path.GetFileName(f))
            .ToHashSet();

        // Ajouter les nouveaux fichiers
        foreach (var filePath in files)
        {
            if (!_configuration.Images.Any(i => i.CheminImage == filePath))
            {
                _configuration.Images.Add(new PersonnageImageConfig
                {
                    CheminImage = filePath,
                    NomFichier = Path.GetFileName(filePath),
                    IsAdult = false
                });
            }
        }

        // Retirer les fichiers qui n'existent plus
        _configuration.Images.RemoveAll(i => !files.Contains(i.CheminImage));

        // Sauvegarder si des changements ont été faits
        if (_configuration.Images.Any())
        {
            SaveConfiguration(_configuration);
        }
    }
}
