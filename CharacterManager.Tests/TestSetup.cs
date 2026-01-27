using System.Text;

namespace CharacterManager.Tests;

/// <summary>
/// Initialisation globale pour tous les tests
/// </summary>
public static class TestSetup
{
    static TestSetup()
    {
        // Configure l'encodage de la console en UTF-8 pour l'affichage correct des accents
        Console.OutputEncoding = Encoding.UTF8;
    }
}
