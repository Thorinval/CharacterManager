using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion des statistiques
/// </summary>
public interface IStatistiquesService
{
    List<LevelEvolutionData> GetLevelEvolutionData();
    List<ClassementEvolutionData> GetClassementEvolutionData();
    List<TeamPowerEvolutionData> GetSelectedTeamPowerEvolutionData();
    List<TeamPowerEvolutionData> GetBestTeamPowerEvolutionData();
    
    // Méthodes utilitaires statiques exposées via l'interface
    string FormatDateWithDay(DateTime date);
    string FormatDateForClassement(DateOnly date);
    string ColorWithAlpha(string hexColor, double alpha = StatisticsConstants.ChartFormatting.AlphaFill);
    List<string> GetPersonnagesWithHistory(List<LevelEvolutionData> dailyData);
    List<object> CreateChartDatasets(List<LevelEvolutionData> dailyData, List<string> personnages, out int minLevel);
}
