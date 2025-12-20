namespace CharacterManager.Server.Services;

/// <summary>
/// Service de gestion des chemins d'images selon le mode adulte.
/// 
/// Système basé sur les répertoires (filesystem-based):
/// - Les images dans /images/personnages/adult/ sont filtrées en mode non-adulte
/// - Les images en dehors de /adult/ sont toujours affichées
/// - Aucune dépendance à un fichier de configuration externe
/// 
/// Cette approche est plus simple et plus maintenable :
/// - Pas de JSON à synchroniser
/// - La structure du dossier définit le contenu adulte
/// - Auto-documentée (le dossier /adult/ indique clairement l'intention)
/// </summary>
public class PersonnageImageConfigService
{
    public PersonnageImageConfigService()
    {
        // Constructeur vide - le service n'utilise pas de fichiers de configuration externes
    }

    /// <summary>
    /// Retourne le chemin d'image à afficher selon le mode adulte.
    /// Utilise la détection de répertoire pour identifier les images adultes.
    /// </summary>
    /// <param name="cheminImage">Chemin complet de l'image (ex: "/images/personnages/adult/hunter.png")</param>
    /// <param name="isAdultModeEnabled">État du mode adulte de l'utilisateur</param>
    /// <returns>
    /// - Chemin complet si image non-adulte OU mode adulte activé
    /// - Chaîne vide "" si image adulte ET mode adulte désactivé (signal pour UI d'afficher lightblue)
    /// </returns>
    public string GetDisplayPath(string cheminImage, bool isAdultModeEnabled)
    {
        if (string.IsNullOrEmpty(cheminImage))
            return cheminImage;

        // Détection: si le chemin contient "/adult/" ET le mode adulte est désactivé
        // → retourner une chaîne vide (signal pour la UI d'afficher un placeholder lightblue)
        if (cheminImage.Contains("/adult/", StringComparison.OrdinalIgnoreCase) && !isAdultModeEnabled)
        {
            return string.Empty;
        }

        // Sinon retourner le chemin inchangé
        return cheminImage;
    }
}
