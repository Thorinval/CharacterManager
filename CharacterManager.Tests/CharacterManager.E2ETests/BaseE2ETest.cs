using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace CharacterManager.E2ETests;

public abstract class BaseE2ETest : IDisposable
{
    protected IWebDriver Driver { get; private set; }
    protected WebDriverWait Wait { get; private set; }
    protected const string BaseUrl = "http://localhost:5269";
    protected const int WaitTimeoutSeconds = 10;

    protected BaseE2ETest()
    {
        InitializeDriver();
    }

    private void InitializeDriver()
    {
        try
        {
            // Setup ChromeDriver using WebDriverManager
            new DriverManager().SetUpDriver(new ChromeConfig());

            var options = new ChromeOptions();
            options.AddArguments(
                "--disable-notifications",
                "--disable-popup-blocking",
                "--start-maximized",
                "--no-sandbox",
                "--disable-dev-shm-usage"
            );

            Driver = new ChromeDriver(options);
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(WaitTimeoutSeconds));
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to initialize WebDriver", ex);
        }
    }

    protected void NavigateTo(string url)
    {
        Driver.Navigate().GoToUrl(url);
        Thread.Sleep(1000); // Wait for page to load
    }

    protected IWebElement WaitForElement(By locator)
    {
        return Wait.Until(d => d.FindElement(locator));
    }

    protected void WaitForElementToBeClickable(By locator)
    {
        Wait.Until(d =>
        {
            var element = d.FindElement(locator);
            return element.Displayed && element.Enabled;
        });
    }

    protected string GetPageTitle()
    {
        return Driver.Title;
    }

    protected string GetCurrentUrl()
    {
        return Driver.Url;
    }

    public virtual void Dispose()
    {
        try
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
        catch (Exception)
        {
            // Ignore exceptions during cleanup
        }
    }
}
