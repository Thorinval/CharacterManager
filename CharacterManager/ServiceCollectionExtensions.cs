using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterManager.Server;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with the dependency injection container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IHostEnvironment environment)
    {
        // Enable in-memory caching for computed statistics and other features
        services.AddMemoryCache();

        // Register scoped services (order matters: ProfileService before PersonnageService)
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAuthenticationHelper, AuthenticationHelper>();
        
        // Register HistoriqueModificationService with lazy timeline service to break circular dependency
        services.AddScoped<IHistoriqueModificationService>(provider =>
        {
            var context = provider.GetRequiredService<ApplicationDbContext>();
            var timelineServiceLazy = new Lazy<ITeamPowerTimelineService>(() => provider.GetRequiredService<ITeamPowerTimelineService>());
            return new HistoriqueModificationService(context, timelineServiceLazy);
        });
        
        services.AddScoped<IPersonnageService, PersonnageService>();
        services.AddScoped<IPmlImportService>(provider => new PmlImportService(
            provider.GetRequiredService<ApplicationDbContext>(),
            provider.GetRequiredService<IHistoriqueModificationService>()));
        services.AddScoped<IPmlExportService, PmlExportService>();
        services.AddScoped<IHistoriqueClassementService, HistoriqueClassementService>();
        services.AddScoped<IHistoriqueLigueService, HistoriqueLigueService>();
        services.AddScoped<ICapaciteService, CapaciteService>();
        services.AddScoped<IClientLocalizationService, ClientLocalizationService>();
        services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
        services.AddScoped<IStatistiquesService, StatistiquesService>();
        services.AddScoped<ITeamPowerTimelineService, TeamPowerTimelineService>();

        // Register singleton services
        services.AddSingleton<IAppVersionService, AppVersionService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ILanguageContextService, LanguageContextService>();
        services.AddSingleton<IAdultModeNotificationService, AdultModeNotificationService>();
        services.AddSingleton<IModalService, ModalService>();

        // Register HTTP clients
        services.AddHttpClient<IUpdateService, UpdateService>();
        services.AddHttpClient();

        return services;
    }
}
