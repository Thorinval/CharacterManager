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
        // Register scoped services (order matters: ProfileService before PersonnageService)
        services.AddScoped<ProfileService>();
        services.AddScoped<HistoriqueModificationService>();  // BEFORE PersonnageService
        services.AddScoped<PersonnageService>();
        services.AddScoped(provider => new PmlImportService(
            provider.GetRequiredService<ApplicationDbContext>(),
            provider.GetRequiredService<HistoriqueModificationService>()));
        services.AddScoped<PmlExportService>();
        services.AddScoped<HistoriqueClassementService>();
        services.AddScoped<HistoriqueLigueService>();
        services.AddScoped<CapaciteService>();
        services.AddScoped<ClientLocalizationService>();
        services.AddScoped<DatabaseInitializationService>();
        services.AddScoped<StatistiquesService>();

        // Register singleton services
        services.AddSingleton<AppVersionService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<LanguageContextService>();
        services.AddSingleton<AdultModeNotificationService>();
        services.AddSingleton<IModalService, ModalService>();

        // Register HTTP clients
        services.AddHttpClient<UpdateService>();
        services.AddHttpClient();

        return services;
    }
}
