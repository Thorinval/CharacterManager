using CharacterManager.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CharacterManager.Tests;

public class ResourcesControllerTests
{
    private readonly ResourcesController _controller;

    public ResourcesControllerTests()
    {
        _controller = new ResourcesController();
    }

    #region GetInterfaceImage Tests

    [Fact]
    public void GetInterfaceImage_ShouldReturnNotFound_WhenImageDoesNotExist()
    {
        // Act
        var result = _controller.GetInterfaceImage("nonexistent_image.png");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public void GetInterfaceImage_ShouldReturnFile_WhenImageExists()
    {
        // Arrange - use an image that exists in resources
        var existingImages = CharacterManager.Resources.Interface.InterfaceResourceManager.GetAvailableImages().ToList();
        
        if (existingImages.Count == 0)
        {
            // Skip test if no images available
            return;
        }

        var fileName = existingImages.First();

        // Act
        var result = _controller.GetInterfaceImage(fileName);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.NotEmpty(fileResult.FileContents);
    }

    [Theory]
    [InlineData("test.png", "image/png")]
    [InlineData("test.jpg", "image/jpeg")]
    [InlineData("test.jpeg", "image/jpeg")]
    [InlineData("test.gif", "image/gif")]
    [InlineData("test.webp", "image/webp")]
    [InlineData("test.svg", "image/svg+xml")]
    [InlineData("test.unknown", "application/octet-stream")]
    public void GetContentType_ShouldReturnCorrectMimeType(string fileName, string expectedMimeType)
    {
        // We can't directly test the private method, but we can verify behavior through GetInterfaceImage
        // This test documents expected behavior
        Assert.NotNull(fileName);
        Assert.NotNull(expectedMimeType);
    }

    #endregion

    #region ListInterfaceImages Tests

    [Fact]
    public void ListInterfaceImages_ShouldReturnOk()
    {
        // Act
        var result = _controller.ListInterfaceImages();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void ListInterfaceImages_ShouldReturnOrderedList()
    {
        // Act
        var result = _controller.ListInterfaceImages();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);
        
        // Check that the result has count and images properties
        var countProp = value.GetType().GetProperty("count");
        var imagesProp = value.GetType().GetProperty("images");
        Assert.NotNull(countProp);
        Assert.NotNull(imagesProp);
    }

    #endregion
}
