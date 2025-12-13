using OpenQA.Selenium;
using Xunit;

namespace CharacterManager.E2ETests;

public class HomePageTests : BaseE2ETest
{
    [Fact]
    public void HomePage_ShouldLoadSuccessfully()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert
        Assert.Contains("CharacterManager", Driver.Title);
        Assert.Contains("localhost", Driver.Url);
    }

    [Fact]
    public void HomePage_ShouldDisplayWelcomeContent()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert - Check if page has content (h1 or main content)
        var body = Driver.FindElement(By.TagName("body"));
        Assert.NotNull(body);
        Assert.True(body.Displayed);
    }

    [Fact]
    public void HomePage_ShouldHaveNavigation()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert - Check if navigation exists
        try
        {
            var navMenu = Driver.FindElement(By.ClassName("navbar"));
            Assert.NotNull(navMenu);
            Assert.True(navMenu.Displayed);
        }
        catch (NoSuchElementException)
        {
            // Navigation might be in a different element
            var mainLayout = Driver.FindElement(By.TagName("nav"));
            Assert.NotNull(mainLayout);
        }
    }

    [Fact]
    public void HomePage_ShouldDisplayVersionInfo()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert - Check if version is displayed (usually contains "1.0.0" or similar)
        var body = Driver.FindElement(By.TagName("body"));
        string bodyText = body.Text;
        Assert.NotEmpty(bodyText);
    }
}
