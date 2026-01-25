namespace CharacterManager.Tests.Constants;

/// <summary>
/// Constantes de données de test spécifiques aux tests unitaires
/// Contient les noms de personnages, descriptions et autres valeurs utilisées uniquement dans les tests
/// </summary>
public static class TestDataConstants
{
    /// <summary>
    /// Noms de personnages utilisés dans les tests
    /// </summary>
    public static class PersonnageNames
    {
        public const string Regina = "REGINA";
        public const string Isabella = "ISABELLA";
        public const string Nouveau = "NOUVEAU";
        public const string Alpha = "ALPHA";
        public const string Belle = "BELLE";
        public const string Katara = "KATARA";
        public const string Alya = "ALYA";
        public const string Commandra = "COMMANDRA";
        public const string Omega = "OMEGA";
        public const string Nova = "NOVA";
        public const string Test = "TEST";
    }

    /// <summary>
    /// Descriptions de personnages utilisées dans les tests
    /// </summary>
    public static class PersonnageDescriptions
    {
        public const string SSRCharacter = "Personnage SSR";
        public const string SRMercenary = "SR Mercenaire";
        public const string TestPersonnage = "Test Personnage";
    }

    /// <summary>
    /// Noms de templates/équipes utilisés dans les tests
    /// </summary>
    public static class TemplateNames
    {
        public const string MonEquipe = "Mon Équipe";
        public const string MonEquipeDescription = "Ma première équipe";
        public const string TestTeam = "Test Team";
        public const string TestTeamDescription = "Équipe de test";
        public const string ExportTest = "Export Test";
        public const string ExportTestDescription = "Template for export";
    }

    /// <summary>
    /// Noms de pièces Lucie House utilisés dans les tests
    /// </summary>
    public static class LucieHousePieceNames
    {
        public const string Hall = "Hall";
        public const string Bibliotheque = "Bibliothèque";
        public const string SalleduTrone = "Salle du Trône";
        public const string Atelier = "Atelier";
    }

    /// <summary>
    /// Rôles de personnages utilisés dans les tests
    /// </summary>
    public static class PersonnageRoles
    {
        public const string Guerriere = "Guerrière";
        public const string Sentinelle = "Sentinelle";
        public const string Combattante = "Combattante";
        public const string Commandant = "Commandant";
        public const string Androide = "Androide";
    }

    /// <summary>
    /// Valeurs numériques communes dans les tests
    /// </summary>
    public static class NumericValues
    {
        // Puissance
        public const int PuissanceNiveau1500 = 1500;
        public const int PuissanceNiveau900 = 900;
        public const int PuissanceNiveau3090 = 3090;
        public const int PuissanceNiveau3320 = 3320;
        public const int PuissanceNiveau835 = 835;
        public const int PuissanceNiveau2000 = 2000;
        public const int PuissanceNiveau1200 = 1200;
        public const int PuissanceNiveau1500Squad = 1500;
        public const int PuissanceNiveau4200 = 4200;
        public const int PuissanceNiveau3100 = 3100;
        public const int PuissanceNiveau1000 = 1000;

        // PA (Points d'Action)
        public const int PAValue100 = 100;
        public const int PAValue50 = 50;
        public const int PAValue143 = 143;
        public const int PAValue60 = 60;
        public const int PAValue90 = 90;
        public const int PAValue200 = 200;

        // PV (Points de Vie)
        public const int PVValue200 = 200;
        public const int PVValue120 = 120;
        public const int PVValue330 = 330;
        public const int PVValue150 = 150;
        public const int PVValue220 = 220;
        public const int PVValue800 = 800;
        public const int PVValue180 = 180;
        public const int PVValue100 = 100;

        // Niveaux
        public const int NiveauLevel5 = 5;
        public const int NiveauLevel3 = 3;
        public const int NiveauLevel8 = 8;
        public const int NiveauLevel6 = 6;
        public const int NiveauLevel7 = 7;
        public const int NiveauLevel20 = 20;
        public const int NiveauLevel10 = 10;
        public const int NiveauLevel2 = 2;
        public const int NiveauLevel14 = 14;

        // Rangs
        public const int RangValue1 = 1;
        public const int RangValue2 = 2;
        public const int RangValue3 = 3;
        public const int RangValue4 = 4;
        public const int RangValue0 = 0;

        // LucieHouse - Niveaux
        public const int LucieHouseNiveauLevel4 = 4;
        public const int LucieHouseNiveauLevel2 = 2;
        public const int LucieHouseNiveauLevel3 = 3;

        // LucieHouse - Puissance
        public const int LuciePuissanceTactique120 = 120;
        public const int LuciePuissanceStrategique30 = 30;
        public const int LuciePuissanceTactique10 = 10;
        public const int LuciePuissanceTactique12 = 12;
        public const int LuciePuissanceStrategique7 = 7;
        public const int LuciePuissanceTactique5 = 5;
        public const int LuciePuissanceStrategique3 = 3;
    }

    /// <summary>
    /// Noms de fichiers utilisés dans les tests
    /// </summary>
    public static class FileNames
    {
        public const string TestImportFile = "test-import.pml";
        public const string ConfigTestFile = "config_test.pml";
        public const string TestExportFile = "test_export.pml";
    }

    /// <summary>
    /// Aspects de bonus Lucie House
    /// </summary>
    public static class LucieHouseAspects
    {
        public const string Degats = "Dégâts";
        public const string PV = "PV";
        public const string Crit = "Crit";
    }

    /// <summary>
    /// Messages d'erreur attendus dans les tests
    /// </summary>
    public static class ExpectedErrorMessages
    {
        public const string InventoryImportBlocked = "Import d'inventaire impossible";
    }

    /// <summary>
    /// Dates utilisées dans les tests
    /// </summary>
    public static class TestDates
    {
        public const string ExportDate1 = "2025-12-20T15:30:00Z";
        public const string ExportDate2 = "2026-01-24T00:00:00Z";
    }
}
