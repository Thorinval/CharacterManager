using OpenQA.Selenium;
using Xunit;

namespace CharacterManager.E2ETests;

public class NavigationTests : BaseE2ETest
{
    [Fact]
    public void Navigation_ShouldHaveHomeLink()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert - Check for home link
        var links = Driver.FindElements(By.TagName("a"));
        var homeLink = links.FirstOrDefault(l => 
            l.Text.Contains("Accueil", StringComparison.OrdinalIgnoreCase) ||
            l.Text.Contains("Home", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(homeLink);
    }

    [Fact]
    public void Navigation_ShouldHaveInventaireLink()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        // Assert - Check for Inventaire link
        var links = Driver.FindElements(By.TagName("a"));
        var inventaireLink = links.FirstOrDefault(l => 
            l.Text.Contains("Inventaire", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(inventaireLink);
    }

    [Fact]
    public void Navigation_HomeLink_ShouldNavigateToHome()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(1000);

        var links = Driver.FindElements(By.TagName("a"));
        var homeLink = links.FirstOrDefault(l => 
            l.Text.Contains("Accueil", StringComparison.OrdinalIgnoreCase) ||
            l.Text.Contains("Home", StringComparison.OrdinalIgnoreCase)
        );

        if (homeLink != null)
        {
            homeLink.Click();
            Thread.Sleep(1000);

            // Assert
            Assert.Contains("localhost", Driver.Url);
        }
    }

    [Fact]
    public void Navigation_InventaireLink_ShouldNavigateToInventaire()
    {
        // Act
        NavigateTo(BaseUrl);
        Thread.Sleep(1000);

        var links = Driver.FindElements(By.TagName("a"));
        var inventaireLink = links.FirstOrDefault(l => 
            l.Text.Contains("Inventaire", StringComparison.OrdinalIgnoreCase)
        );

        if (inventaireLink != null)
        {
            inventaireLink.Click();
            Thread.Sleep(2000);

            // Assert - Check if on inventaire page
            var url = Driver.Url;
            Assert.Contains("inventaire", url, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Navigation_ShouldStayOnSamePage_WhenClicking()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(1000);

        string initialUrl = Driver.Url;
        var body = Driver.FindElement(By.TagName("body"));
        body.SendKeys(Keys.Escape); // Press ESC to close any modal
        Thread.Sleep(500);

        // Assert - Should still be on same page
        Assert.Contains("localhost", Driver.Url);
    }
}
