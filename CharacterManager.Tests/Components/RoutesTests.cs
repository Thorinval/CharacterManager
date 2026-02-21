using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components;

public class RoutesTests : TestContext
{
    public RoutesTests()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        var localizationMock = new Mock<IClientLocalizationService>();
        localizationMock.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        localizationMock.SetupGet(l => l.CurrentLanguage).Returns("fr");
        Services.AddSingleton(localizationMock.Object);

        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddSingleton<IAuthenticationService>(Mock.Of<IAuthenticationService>());
        Services.AddSingleton<IModalService>(Mock.Of<IModalService>());
        Services.AddSingleton<IUpdateService>(Mock.Of<IUpdateService>(u => u.CheckForUpdatesAsync() == Task.FromResult<UpdateInfo?>(new UpdateInfo())));
        Services.AddSingleton<IAppVersionService>(Mock.Of<IAppVersionService>(v => v.GetAppVersion() == "1.0.0"));

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new ApplicationDbContext(options);
        db.Profiles.Add(new Profile { Username = "tester", Language = "fr" });
        db.SaveChanges();
        Services.AddSingleton(db);
        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<IProfileService>(new ProfileService(db, config, NullLogger<ProfileService>.Instance));
    }

    [Fact]
    public void Not_authenticated_shows_login_prompt()
    {
        var cut = RenderComponent<Routes>();

        cut.WaitForAssertion(() => Assert.Contains("Vous devez être connecté", cut.Markup));
    }
}
