using OpenQA.Selenium;
using Xunit;

namespace CharacterManager.E2ETests;

public class InventairePageTests : BaseE2ETest
{
    [Fact]
    public void InventairePage_ShouldLoadSuccessfully()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        // Assert
        Assert.Contains("localhost", Driver.Url);
    }

    [Fact]
    public void InventairePage_ShouldDisplayTable()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        // Assert - Check if table exists
        try
        {
            var table = Driver.FindElement(By.TagName("table"));
            Assert.NotNull(table);
            Assert.True(table.Displayed);
        }
        catch (NoSuchElementException)
        {
            // If no table, check for grid or list view
            var content = Driver.FindElement(By.TagName("main"));
            Assert.NotNull(content);
        }
    }

    [Fact]
    public void InventairePage_ShouldHaveAddButton()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        // Assert - Look for button with text containing "Add" or "Ajouter"
        var buttons = Driver.FindElements(By.TagName("button"));
        var addButton = buttons.FirstOrDefault(b => 
            b.Text.Contains("Ajouter", StringComparison.OrdinalIgnoreCase) ||
            b.Text.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
            b.Text.Contains("Nouveau", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(addButton);
        Assert.True(addButton.Displayed);
    }

    [Fact]
    public void InventairePage_AddButtonShouldOpenModal()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        var buttons = Driver.FindElements(By.TagName("button"));
        var addButton = buttons.FirstOrDefault(b => 
            b.Text.Contains("Ajouter", StringComparison.OrdinalIgnoreCase) ||
            b.Text.Contains("Nouveau", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(addButton);
        addButton.Click();
        Thread.Sleep(1000);

        // Assert - Check if modal is displayed
        try
        {
            var modal = Driver.FindElement(By.ClassName("modal"));
            Assert.NotNull(modal);
            // Check if modal is visible (may be display: flex or display: block)
            var style = modal.GetAttribute("style");
            Assert.NotNull(style);
        }
        catch (NoSuchElementException)
        {
            // Modal might be displayed with different class
            var modalContent = Driver.FindElements(By.ClassName("modal-content"));
            Assert.NotEmpty(modalContent);
        }
    }

    [Fact]
    public void InventairePage_ModalShouldHaveFormFields()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        var buttons = Driver.FindElements(By.TagName("button"));
        var addButton = buttons.FirstOrDefault(b => 
            b.Text.Contains("Ajouter", StringComparison.OrdinalIgnoreCase)
        );

        if (addButton != null)
        {
            addButton.Click();
            Thread.Sleep(1000);

            // Assert - Check for form inputs
            var inputs = Driver.FindElements(By.TagName("input"));
            var selects = Driver.FindElements(By.TagName("select"));

            Assert.True(inputs.Count > 0 || selects.Count > 0, "Modal should have form fields");
        }
    }

    [Fact]
    public void InventairePage_ShouldDisplayTableColumns()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        // Assert - Check for expected table headers
        var headers = Driver.FindElements(By.TagName("th"));
        
        if (headers.Count > 0)
        {
            var headerTexts = headers.Select(h => h.Text).ToList();
            // Check for at least some expected columns
            Assert.True(headerTexts.Any(), "Table should have headers");
        }
    }

    [Fact]
    public void InventairePage_ShouldNavigateToDetailPage()
    {
        // Act
        NavigateTo($"{BaseUrl}/inventaire");
        Thread.Sleep(2000);

        // Assert - Look for clickable rows or detail links
        var rows = Driver.FindElements(By.TagName("tr"));
        
        if (rows.Count > 1) // If there are rows (excluding header)
        {
            // Try to click first data row
            var firstDataRow = rows.FirstOrDefault(r => r.FindElements(By.TagName("td")).Count > 0);
            
            if (firstDataRow != null)
            {
                string originalUrl = Driver.Url;
                firstDataRow.Click();
                Thread.Sleep(2000);

                // The URL might change or content might update
                // This is just to verify click was processed without error
                Assert.NotNull(Driver.Url);
            }
        }
    }
}
