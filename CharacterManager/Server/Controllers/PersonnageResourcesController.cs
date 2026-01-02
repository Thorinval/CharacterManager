namespace CharacterManager.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using CharacterManager.Resources.Personnages;

/// <summary>
/// Contrôleur API pour servir les images de personnages depuis les ressources embarquées.
/// Les images sont organisées par personnage dans la DLL CharacterManager.Resources.Personnages.
/// </summary>
[ApiController]
[Route("api/resources/personnages")]
public class PersonnageResourcesController : ControllerBase
{
    private readonly ILogger<PersonnageResourcesController> _logger;

    public PersonnageResourcesController(ILogger<PersonnageResourcesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Récupère une image de personnage depuis les ressources embarquées.
    /// </summary>
    /// <param name="personnage">Nom du dossier du personnage (ex: "Alexa", "Hunter")</param>
    /// <param name="fileName">Nom du fichier image (ex: "alexa_small_portrait.png")</param>
    /// <returns>L'image demandée ou 404 si non trouvée</returns>
    [HttpGet("{personnage}/{fileName}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult GetImage(string personnage, string fileName)
    {
        try
        {
            var imageStream = PersonnageResourceManager.GetImageStream(personnage, fileName);
            
            if (imageStream == null)
            {
                _logger.LogWarning("Image non trouvée: {Personnage}/{FileName}", personnage, fileName);
                return NotFound();
            }

            var contentType = fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                              fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : "image/png";

            return File(imageStream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'image {Personnage}/{FileName}", personnage, fileName);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Liste toutes les ressources d'images disponibles.
    /// Endpoint de débogage pour vérifier les ressources embarquées.
    /// </summary>
    [HttpGet("list")]
    public IActionResult ListResources()
    {
        try
        {
            var resources = PersonnageResourceManager.GetAllResourceNames();
            return Ok(new
            {
                Count = resources.Length,
                Resources = resources.OrderBy(r => r).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du listage des ressources");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Vérifie si une image de personnage existe.
    /// </summary>
    /// <param name="personnage">Nom du dossier du personnage</param>
    /// <param name="fileName">Nom du fichier image</param>
    [HttpHead("{personnage}/{fileName}")]
    public IActionResult CheckImage(string personnage, string fileName)
    {
        var exists = PersonnageResourceManager.ImageExists(personnage, fileName);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Récupère toutes les images d'un personnage spécifique.
    /// </summary>
    /// <param name="personnage">Nom du dossier du personnage</param>
    [HttpGet("{personnage}/all")]
    public IActionResult GetAllPersonnageImages(string personnage)
    {
        try
        {
            var images = PersonnageResourceManager.GetAllPersonnageImages(personnage);
            
            if (images.Count == 0)
            {
                return NotFound(new { Message = $"Aucune image trouvée pour le personnage: {personnage}" });
            }

            return Ok(new
            {
                Personnage = personnage,
                ImageCount = images.Count,
                Images = images.Keys.OrderBy(k => k).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des images du personnage {Personnage}", personnage);
            return StatusCode(500);
        }
    }
}
