using Xunit;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CharacterManager.Tests;

public class VerifyPowerCalculationTest
{
    [Fact]
    public void VerifyDecember4PowerCalculation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        using var db = new ApplicationDbContext(options);

        // Rechercher le classement du 04/12
        var classements = db.HistoriquesClassement
            .Where(h => h.DateEnregistrement.Month == 12 && h.DateEnregistrement.Day == 4)
            .OrderByDescending(h => h.DateEnregistrement)
            .Take(1)
            .ToList();

        if (!classements.Any())
        {
            Console.WriteLine("Aucun classement trouvé pour le 04/12");
            return;
        }

        var classement = classements.First();
        Console.WriteLine($"\n=== Classement du {classement.DateEnregistrement:yyyy-MM-dd} ===");
        Console.WriteLine($"Puissance totale enregistrée: {classement.PuissanceTotale}");

        // Rechercher les modifications du même jour
        var date = classement.DateEnregistrement;
        var dateDebut = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        var dateFin = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);

        var modifications = db.HistoriquesModifications
            .Where(h => h.DateModification >= dateDebut && h.DateModification <= dateFin)
            .OrderBy(h => h.TypeEntite)
            .ThenBy(h => h.NomEntite)
            .ToList();

        Console.WriteLine($"\n=== Modifications du {date:yyyy-MM-dd} ({modifications.Count} entrées) ===");

        // Calculer la puissance des personnages
        int puissancePersonnages = 0;
        int puissanceLucie = 0;

        var personnagesPuissance = modifications
            .Where(m => m.TypeEntite == TypeEntite.Personnage && m.ChampModifie == "Puissance")
            .ToList();

        Console.WriteLine($"\nPersonnages avec puissance ({personnagesPuissance.Count}):");
        foreach (var modif in personnagesPuissance)
        {
            if (int.TryParse(modif.NouvelleValeur, out int puissance))
            {
                puissancePersonnages += puissance;
                Console.WriteLine($"  {modif.NomEntite}: {puissance}");
            }
        }

        // Vérifier les modifications Lucie
        var lucieModifs = modifications
            .Where(m => m.TypeEntite == TypeEntite.Piece)
            .ToList();

        Console.WriteLine($"\nModifications Lucie ({lucieModifs.Count}):");
        foreach (var modif in lucieModifs)
        {
            Console.WriteLine($"  EntiteId={modif.EntiteId}, Champ={modif.ChampModifie}, Valeur={modif.NouvelleValeur}");
            
            if (modif.ChampModifie == "PuissanceLucieSelectionnee" || modif.ChampModifie == "PuissanceLucieMax")
            {
                if (int.TryParse(modif.NouvelleValeur, out int p))
                {
                    puissanceLucie = p;
                }
            }
        }

        Console.WriteLine($"\n=== Résultats ===");
        Console.WriteLine($"Puissance personnages: {puissancePersonnages}");
        Console.WriteLine($"Puissance Lucie: {puissanceLucie}");
        Console.WriteLine($"Total calculé: {puissancePersonnages + puissanceLucie}");
        Console.WriteLine($"Total attendu: {classement.PuissanceTotale}");
        Console.WriteLine($"Écart: {classement.PuissanceTotale - (puissancePersonnages + puissanceLucie)}");
        
        // Vérifier si l'écart est de 2825
        var ecart = classement.PuissanceTotale - (puissancePersonnages + puissanceLucie);
        if (Math.Abs(ecart - 2825) < 10)
        {
            Console.WriteLine("\n⚠️ PROBLÈME DÉTECTÉ: L'écart de ~2825 confirme que la puissance de Lucie n'est pas prise en compte!");
            Console.WriteLine($"\n📌 DIAGNOSTIC:");
            Console.WriteLine($"   - Aucune modification Lucie trouvée pour le {date:yyyy-MM-dd}");
            Console.WriteLine($"   - Le code devrait créer une entrée avec EntiteId=-1 (Sélectionnée) et EntiteId=-2 (Max)");
            Console.WriteLine($"   - Il faut vérifier si EnregistrerPuissanceLucieAsync() est appelée lors de l'import");
            Console.WriteLine($"   - Ou si le flag EstImportation devrait être true dans cette méthode");
        }
    }
}
