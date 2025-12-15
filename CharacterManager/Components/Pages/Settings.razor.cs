namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;

public partial class Settings
{
    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public AppVersionService AppVersionService { get; set; } = null!;

    [Inject]
    public ClientLocalizationService LocalizationService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private bool isAdultModeEnabled = true;
    private string currentLanguage = "fr";
    private string appVersion = "Unknown";
    private List<LanguageOption> availableLanguages = new();

    protected override async Task OnInitializedAsync()
    {
        var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            isAdultModeEnabled = settings.IsAdultModeEnabled;
            currentLanguage = settings.Language ?? "fr";
        }

        appVersion = AppVersionService.GetAppVersion();
        
        // Charger les ressources pour la langue courante
        await LocalizationService.InitializeAsync(currentLanguage);
        
        // Obtenir les langues disponibles
        var locService = new LocalizationService(new HttpClient(), null!);
        availableLanguages = locService.GetAvailableLanguages();
    }

    private async Task OnAdultModeChanged()
    {
        var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            settings.IsAdultModeEnabled = isAdultModeEnabled;
            DbContext.AppSettings.Update(settings);
            await DbContext.SaveChangesAsync();
            StateHasChanged();
        }
    }

    private async Task OnLanguageChanged(ChangeEventArgs e)
    {
        if (e.Value is string newLanguage)
        {
            currentLanguage = newLanguage;
            
            // Mettre à jour la base de données
            var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                settings.Language = newLanguage;
                DbContext.AppSettings.Update(settings);
                await DbContext.SaveChangesAsync();
            }

            // Mettre à jour le service de localisation
            await LocalizationService.SetLanguageAsync(newLanguage);
            
            // Recharger la page pour appliquer les changements
            await JSRuntime.InvokeVoidAsync("location.reload");
        }
    }
}
