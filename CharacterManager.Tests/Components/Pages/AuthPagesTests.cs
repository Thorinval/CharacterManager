using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class AuthPagesTests : TestContext
{
    private readonly TestAuthorizationContext _auth;

    public AuthPagesTests()
    {
        _auth = this.AddTestAuthorization();

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "CharacterManager", "wwwroot"));
        Services.AddSingleton(env.Object);

        var languageContext = new LanguageContextService();
        Services.AddSingleton<ILanguageContextService>(languageContext);

        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        Services.AddSingleton<IHttpContextAccessor>(httpAccessor);

        var localizationService = new ClientLocalizationService(env.Object, NullLogger<ClientLocalizationService>.Instance, languageContext, httpAccessor);
        Services.AddSingleton<IClientLocalizationService>(localizationService);

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var profileService = new ProfileService(db, config, NullLogger<ProfileService>.Instance);
        Services.AddSingleton(db);
        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(profileService);
        Services.AddSingleton<IProfileService>(profileService);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Error_shows_request_id_when_present()
    {
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" };

        var cut = RenderComponent<CascadingValue<HttpContext?>>(parameters =>
            parameters.Add(p => p.Value, httpContext)
                      .AddChildContent<Error>());

        Assert.Contains("trace-123", cut.Markup);
    }

    [Fact]
    public void Login_displays_error_message_from_query()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?error=locked&minutes=5");

        var cut = RenderComponent<Login>();

        cut.WaitForAssertion(() => Assert.Contains("login.error.locked", cut.Markup));
    }

    [Fact]
    public void ChangePassword_not_authorized_shows_prompt()
    {
        _auth.SetNotAuthorized();

        var cut = RenderComponent<ChangePassword>();

        cut.WaitForAssertion(() => Assert.Contains("changePassword.signInPrompt", cut.Markup));
    }

    [Fact]
    public void ManageUsers_not_admin_shows_access_denied()
    {
        _auth.SetAuthorized("user");
        _auth.SetRoles("utilisateur");

        var cut = RenderComponent<ManageUsers>();

        cut.WaitForAssertion(() => Assert.Contains("manageUsers.accessDenied", cut.Markup));
    }
}