namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;

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

    [Inject]
    public ProfileService ProfileService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    private Profile? currentProfile;
    private string userRole = "utilisateur";
    public bool IsAdmin => string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase);

    private bool isAdultModeEnabled = true;
    private string currentLanguage = "fr";
    private string appVersion = "Unknown";
    private List<LanguageOption> availableLanguages = new();

    protected override async Task OnInitializedAsync()
    {
        // Load profile if authenticated
        var authState = await AuthStateTask;
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            var username = authState.User.Identity!.Name ?? "";
            currentProfile = await ProfileService.GetOrCreateAsync(username);
            isAdultModeEnabled = currentProfile.AdultMode;
            currentLanguage = currentProfile.Language ?? "fr";
            userRole = authState.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "utilisateur";
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
        if (currentProfile != null)
        {
            currentProfile.AdultMode = isAdultModeEnabled;
            await ProfileService.UpdateAsync(currentProfile);
            StateHasChanged();
        }
    }

    private async Task OnLanguageChanged()
    {
        var newLanguage = currentLanguage;

        // Mettre à jour le profil utilisateur
        if (currentProfile != null)
        {
            currentProfile.Language = newLanguage;
            await ProfileService.UpdateAsync(currentProfile);
        }

        // Mettre à jour le service de localisation
        await LocalizationService.SetLanguageAsync(newLanguage);

        StateHasChanged();

        // Recharger la page pour appliquer les changements
        await JSRuntime.InvokeVoidAsync("eval", "window.location.reload(true);");
    }
}
