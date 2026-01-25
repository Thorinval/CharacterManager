using System.Security.Claims;
using Bunit;
using CharacterManager.Components;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components;

public class LocalizationProviderTests : TestContext
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IClientLocalizationService> _localizationServiceMock = new();
    private readonly Mock<IProfileService> _profileServiceMock = new();
    private readonly Mock<ILanguageContextService> _languageContextMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    public LocalizationProviderTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        Services.AddSingleton(_dbContext);
        Services.AddSingleton(_localizationServiceMock.Object);
        Services.AddSingleton(_profileServiceMock.Object);
        Services.AddSingleton(_languageContextMock.Object);
        Services.AddSingleton(_httpContextAccessorMock.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Authenticated_user_prefers_profile_language()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "tester") }, "TestAuth"));
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = user });

        _profileServiceMock
            .Setup(p => p.GetByUsernameAsync("tester"))
            .ReturnsAsync(new Profile { Username = "tester", Language = "en" });

        _localizationServiceMock.Setup(l => l.InitializeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var cut = RenderComponent<LocalizationProvider>(parameters => parameters.AddChildContent("<p>ready</p>"));

        cut.WaitForAssertion(() => _localizationServiceMock.Verify(l => l.InitializeAsync("en"), Times.Once));
        cut.WaitForAssertion(() => _languageContextMock.Verify(l => l.SetLanguageForUser("tester", "en"), Times.Once));
        cut.MarkupMatches("<p>ready</p>");
    }

    [Fact]
    public void Unauthenticated_user_uses_appsettings_language()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _dbContext.AppSettings.Add(new AppSettings { Id = 1, Language = "es" });
        _dbContext.SaveChanges();

        _localizationServiceMock.Setup(l => l.InitializeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var cut = RenderComponent<LocalizationProvider>(parameters => parameters.AddChildContent("<p>ready</p>"));

        cut.WaitForAssertion(() => _localizationServiceMock.Verify(l => l.InitializeAsync("es"), Times.Once));
        cut.WaitForAssertion(() => _languageContextMock.Verify(l => l.SetLanguageForUser(string.Empty, "es"), Times.Once));
        cut.MarkupMatches("<p>ready</p>");
    }

    [Fact]
    public void Initialization_errors_still_render_content()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _localizationServiceMock.Setup(l => l.InitializeAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("boom"));

        var cut = RenderComponent<LocalizationProvider>(parameters => parameters.AddChildContent("<p>ready</p>"));

        cut.WaitForAssertion(() => Assert.Equal("<p>ready</p>", cut.Markup.Trim()));
    }
}
