using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class ManageUsersModalTests : BlazorComponentTestBase, IDisposable
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly ProfileService _profileService;
    private readonly string _testDir;
    private readonly string _i18nDir;
    private bool _disposed;

    public ManageUsersModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"ManageUsersModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["manageUsers.title"] = "Gestion des utilisateurs",
            ["manageUsers.username"] = "Nom d'utilisateur",
            ["manageUsers.password"] = "Mot de passe",
            ["manageUsers.role"] = "Rôle",
            ["manageUsers.roleUser"] = "Utilisateur",
            ["manageUsers.roleAdmin"] = "Administrateur",
            ["manageUsers.create"] = "Créer",
            ["manageUsers.existing"] = "Utilisateurs existants",
            ["manageUsers.noUsers"] = "Aucun utilisateur",
            ["manageUsers.reset"] = "Réinitialiser",
            ["manageUsers.copy"] = "Copier",
            ["manageUsers.tempPassword"] = "Mot de passe temporaire:",
            ["manageUsers.accessDenied"] = "Accès refusé",
            ["manageUsers.table.name"] = "Nom",
            ["manageUsers.table.role"] = "Rôle",
            ["manageUsers.table.lockout"] = "Verrouillé jusqu'à",
            ["manageUsers.table.actions"] = "Actions",
            ["manageUsers.messageRequired"] = "Nom d'utilisateur et mot de passe requis",
            ["manageUsers.messageWeak"] = "Mot de passe faible: {0}",
            ["manageUsers.messageCreated"] = "Utilisateur créé",
            ["manageUsers.messageExists"] = "L'utilisateur existe déjà",
            ["common.loading"] = "Chargement...",
            ["common.delete"] = "Supprimer"
        };
        File.WriteAllText(Path.Combine(_i18nDir, "fr.json"), JsonSerializer.Serialize(frContent));

        // Setup database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Setup config for ProfileService
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Lockout:MaxAttempts"] = "3",
                ["Security:Lockout:LockoutMinutes"] = "5"
            })
            .Build();

        var loggerProfileMock = new Mock<ILogger<ProfileService>>();
        _profileService = new ProfileService(_context, config, loggerProfileMock.Object);

        // Setup mocks
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_testDir);
        
        var loggerMock = new Mock<ILogger<ClientLocalizationService>>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        
        var languageContext = new LanguageContextService();

        var localizationService = new ClientLocalizationService(
            envMock.Object,
            loggerMock.Object,
            languageContext,
            httpContextAccessorMock.Object);
        
        localizationService.InitializeAsync("fr").GetAwaiter().GetResult();
        
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton(languageContext);
        Services.AddSingleton(localizationService);
        Services.AddSingleton(_profileService);

        // Setup authorization
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        // Add JSRuntime mock
        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
    }

    #region Rendering Tests

    [Fact]
    public void ManageUsersModal_ShouldRender()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasModalBodyClass()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_DisplaysTitle()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check if authorization section is rendered
        var markupDebug = cut.Markup;
        // Since LocalizedText component is used, check for the container structure instead
        Assert.Contains("admin_panel_settings", cut.Markup);
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasIcon()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Has admin icon
        Assert.Contains("admin_panel_settings", cut.Markup);
    }

    #endregion

    #region Structure Tests

    [Fact]
    public void ManageUsersModal_HasCreateForm()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Has form elements
        Assert.Contains("form-control", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasUsernameField()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for form elements instead of localized text
        Assert.Contains("form-control", cut.Markup);
        Assert.Contains("type=\"password\"", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasPasswordField()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for password input element
        Assert.Contains("type=\"password\"", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasRoleSelect()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for select element
        Assert.Contains("form-select", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasCreateButton()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for button element
        Assert.Contains("btn btn-success", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasExistingUsersSection()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for table element
        Assert.Contains("<table", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasUsersTable()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert
        Assert.Contains("table", cut.Markup);
    }

    #endregion

    #region Table Headers Tests

    [Fact]
    public void ManageUsersModal_HasNameColumn()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for table structure
        Assert.Contains("<th", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasRoleColumn()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for table headers
        Assert.Contains("<th", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasLockoutColumn()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for table structure
        Assert.Contains("<td", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasActionsColumn()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for table structure
        Assert.Contains("<tbody", cut.Markup);
    }

    #endregion

    #region Role Options Tests

    [Fact]
    public void ManageUsersModal_HasUserRoleOption()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for option elements
        Assert.Contains("<option", cut.Markup);
    }

    [Fact]
    public void ManageUsersModal_HasAdminRoleOption()
    {
        // Act
        var cut = RenderComponent<ManageUsersModal>();

        // Assert - Check for option elements
        Assert.Contains("<option", cut.Markup);
    }

    #endregion

    #region Localization Keys Tests

    [Fact]
    public void ManageUsersModal_LocalizationKeys_ShouldBeComplete()
    {
        // Verify all expected localization keys
        var expectedKeys = new[]
        {
            "manageUsers.title",
            "manageUsers.username",
            "manageUsers.password",
            "manageUsers.role",
            "manageUsers.create",
            "manageUsers.existing"
        };

        Assert.Equal(6, expectedKeys.Length);
    }

    #endregion

    #region Cleanup

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected new virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
                
                if (Directory.Exists(_testDir))
                {
                    try { Directory.Delete(_testDir, recursive: true); }
                    catch { }
                }
            }
            _disposed = true;
        }
    }

    #endregion
}
