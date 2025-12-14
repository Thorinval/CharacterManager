namespace CharacterManager.Server.Models;

/// <summary>
/// Configuration pour une image de personnage
/// </summary>
public class PersonnageImageConfig
{
    /// <summary>
    /// Chemin relatif de l'image (ex: /images/personnages/character1.png)
    /// </summary>
    public string CheminImage { get; set; } = string.Empty;

    /// <summary>
    /// Nom du fichier (sans le chemin)
    /// </summary>
    public string NomFichier { get; set; } = string.Empty;

    /// <summary>
    /// Indique si l'image est réservée aux adultes
    /// </summary>
    public bool IsAdult { get; set; } = false;
}

/// <summary>
/// Configuration globale des images de personnages
/// </summary>
public class PersonnagesImagesConfiguration
{
    public List<PersonnageImageConfig> Images { get; set; } = new();
}
