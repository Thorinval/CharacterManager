namespace CharacterManager.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using CharacterManager.Resources.Interface;

/// <summary>
/// Contrôleur pour servir les ressources d'interface (images) depuis la DLL.
/// </summary>
[ApiController]
[Route("api/resources")]
public class ResourcesController : ControllerBase
{
    /// <summary>
    /// Récupère une image d'interface.
    /// </summary>
    /// <param name="fileName">Nom du fichier image (ex: fond_puissance.png)</param>
    /// <returns>Fichier image ou 404 si non trouvé</returns>
    [HttpGet("interface/{fileName}")]
    public IActionResult GetInterfaceImage(string fileName)
    {
        try
        {
            var imageBytes = InterfaceResourceManager.GetImageBytes(fileName);
            if (imageBytes == null)
                return NotFound($"Image '{fileName}' not found");

            var contentType = GetContentType(fileName);
            return File(imageBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error loading image: {ex.Message}");
        }
    }

    /// <summary>
    /// Liste toutes les images d'interface disponibles.
    /// </summary>
    /// <returns>Collection des noms de fichiers images</returns>
    [HttpGet("interface")]
    public IActionResult ListInterfaceImages()
    {
        try
        {
            var images = InterfaceResourceManager.GetAvailableImages()
                .OrderBy(x => x)
                .ToList();
            
            return Ok(new { count = images.Count, images });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error listing images: {ex.Message}");
        }
    }

    /// <summary>
    /// Détermine le type MIME basé sur l'extension du fichier.
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
