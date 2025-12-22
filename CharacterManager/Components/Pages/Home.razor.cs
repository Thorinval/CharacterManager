namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using CharacterManager.Server.Services;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;

public partial class Home : IAsyncDisposable
{
    [Inject]
    public PersonnageImageConfigService ImageConfigService { get; set; } = null!;

    [Inject]
    public PersonnageService PersonnageService { get; set; } = null!;

    [Inject]
    public AdultModeNotificationService AdultModeNotification { get; set; } = null!;

    [Inject]
    public ApplicationDbContext DbContext { get; set; } = null!;

    [Inject]
    public ProfileService ProfileService { get; set; } = null!;

    [Inject]
    public IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [Inject]
    public RoadmapService RoadmapService { get; set; } = null!;

    private string homeImageUrl = AppConstants.Paths.HomeDefaultBackground;
    private bool isAdultModeEnabled;

    protected override async Task OnInitializedAsync()
    {
        AdultModeNotification.Subscribe(OnAdultModeChanged);
        isAdultModeEnabled = await GetCurrentAdultModeAsync();
        await UpdateHomeImageAsync(isAdultModeEnabled);
    }

    private void OnAdultModeChanged(bool isAdultModeEnabled)
    {
        InvokeAsync(async () =>
        {
            this.isAdultModeEnabled = isAdultModeEnabled;
            await UpdateHomeImageAsync(isAdultModeEnabled);
            StateHasChanged();
        });
    }

    private async Task<bool> GetCurrentAdultModeAsync()
    {
        var isAdultMode = false;

        var user = HttpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var username = user.Identity?.Name ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    var profile = await ProfileService.GetByUsernameAsync(username);
                    if (profile != null)
                    {
                        isAdultMode = profile.AdultMode;
                    }
                    else
                    {
                        var settings = await DbContext.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                        if (settings != null)
                        {
                            isAdultMode = settings.IsAdultModeEnabled;
                        }
                    }
                }
                catch
                {
                    // ignore and keep default
                }
            }
        }
        else
        {
            try
            {
                var settings = await DbContext.AppSettings.FirstOrDefaultAsync();
                if (settings != null)
                {
                    isAdultMode = settings.IsAdultModeEnabled;
                }
            }
            catch
            {
                // ignore and keep default
            }
        }

        return isAdultMode;
    }

    private Task UpdateHomeImageAsync(bool isAdultMode)
    {
        const string adultImage = "/images/personnages/adult/accueil.png";
        var fallbackImage = AppConstants.Paths.HomeDefaultBackground;

        if (isAdultMode)
        {
            var candidate = ImageConfigService.GetDisplayPath(adultImage, isAdultMode);
            homeImageUrl = string.IsNullOrEmpty(candidate) ? fallbackImage : candidate;
        }
        else
        {
            homeImageUrl = fallbackImage;
        }

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        AdultModeNotification.Unsubscribe(OnAdultModeChanged);
        return ValueTask.CompletedTask;
    }
}
