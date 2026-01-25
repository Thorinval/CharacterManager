using Bunit;
using CharacterManager.Components;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace CharacterManager.Tests.Components;

public class EscouadePreviewEditorTests : IDisposable
{
    private readonly TestContext ctx;
    private readonly ApplicationDbContext dbContext;
    private bool disposed;

    public EscouadePreviewEditorTests()
    {
        ctx = new TestContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new ApplicationDbContext(options);
        ctx.Services.AddSingleton(dbContext);
        ctx.Services.AddSingleton<IAdultModeNotificationService, AdultModeNotificationService>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                ctx?.Dispose();
                dbContext?.Dispose();
            }
            disposed = true;
        }
    }

    [Fact]
    public void EscouadePreviewEditor_Renders_Successfully()
    {
        // Arrange & Act
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void EscouadePreviewEditor_InitializesWithEmptySelections()
    {
        // Arrange & Act
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();

        // Assert
        Assert.NotNull(cut.Instance);
        Assert.NotNull(cut.Instance.PersonnagesSelectionnes);
    }

    [Fact]
    public void EscouadePreviewEditor_PuissanceTotal_InitiallyZero()
    {
        // Arrange & Act
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();

        // Assert
        Assert.Equal(0, cut.Instance.PuissanceTotal);
    }

    [Fact]
    public void EscouadePreviewEditor_PuissanceTotal_SumsPersonnagePuissance()
    {
        // Arrange
        var personnages = new List<Personnage?>
        {
            new Personnage { Id = 1, Nom = "P1", Puissance = 10, Type = TypePersonnage.Commandant },
            new Personnage { Id = 2, Nom = "P2", Puissance = 20, Type = TypePersonnage.Mercenaire },
            null,
            new Personnage { Id = 3, Nom = "P3", Puissance = 30, Type = TypePersonnage.Mercenaire }
        };

        // Act
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.PersonnagesSelectionnes, personnages));

        // Assert
        Assert.Equal(60, cut.Instance.PuissanceTotal);
    }

    [Fact]
    public void EscouadePreviewEditor_ImplementsIDisposable()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();

        // Assert
        Assert.IsAssignableFrom<IDisposable>(cut.Instance);
    }

    [Fact]
    public void EscouadePreviewEditor_Dispose_DoesNotThrow()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();

        // Act & Assert
        var exception = Record.Exception(() => cut.Instance.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void IsTypeAllowed_AllowsCommandantInSlot0()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsTypeAllowed", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(cut.Instance, new object[] { 0, TypePersonnage.Commandant })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTypeAllowed_RejectsMercenaireInSlot0()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsTypeAllowed", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(cut.Instance, new object[] { 0, TypePersonnage.Mercenaire })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTypeAllowed_AllowsMercenaireInSlots1To8()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsTypeAllowed", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        for (int i = 1; i <= 8; i++)
        {
            var result = (bool)method!.Invoke(cut.Instance, new object[] { i, TypePersonnage.Mercenaire })!;
            Assert.True(result);
        }
    }

    [Fact]
    public void IsTypeAllowed_AllowsAndroideInSlots9To11()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsTypeAllowed", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        for (int i = 9; i <= 11; i++)
        {
            var result = (bool)method!.Invoke(cut.Instance, new object[] { i, TypePersonnage.Androide })!;
            Assert.True(result);
        }
    }

    [Fact]
    public void GetImageStyle_ReturnsBackgroundStyleForEmptyUrl()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("GetImageStyle", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(cut.Instance, new object[] { "" })!;

        // Assert
        Assert.Equal("background-color: lightblue; display: block;", result);
    }

    [Fact]
    public void GetImageStyle_ReturnsEmptyForNonEmptyUrl()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("GetImageStyle", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(cut.Instance, new object[] { "https://example.com/image.jpg" })!;

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void IsImageBlocked_ReturnsTrueForEmptyUrl()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsImageBlocked", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(cut.Instance, new object[] { "" })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsImageBlocked_ReturnsFalseForNonEmptyUrl()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("IsImageBlocked", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(cut.Instance, new object[] { "https://example.com/image.jpg" })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemovePersonnage_RemovesPersonnageFromList()
    {
        // Arrange
        var personnages = new List<Personnage?>
        {
            new Personnage { Id = 1, Nom = "P1", Puissance = 10, Type = TypePersonnage.Commandant },
            new Personnage { Id = 2, Nom = "P2", Puissance = 20, Type = TypePersonnage.Mercenaire }
        };
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.PersonnagesSelectionnes, personnages));

        var method = typeof(EscouadePreviewEditor).GetMethod("RemovePersonnage", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 1 })!;
        await task;

        // Assert
        Assert.Null(cut.Instance.PersonnagesSelectionnes[1]);
    }

    [Fact]
    public void GetSlotStyle_ReturnsEmptyString()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>();
        var method = typeof(EscouadePreviewEditor).GetMethod("GetSlotStyle", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(cut.Instance, new object[] { 0 })!;

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task HandleDrop_ReturnsEarly_WhenDraggedPersonnageIdIsNull()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.DraggedPersonnageId, null));

        var method = typeof(EscouadePreviewEditor).GetMethod("HandleDrop", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 0 })!;
        await task;

        // Assert - no exception thrown, method returned early
        Assert.True(true);
    }

    [Fact]
    public async Task HandleDrop_ReturnsEarly_WhenOnPersonnageRequestedIsNull()
    {
        // Arrange
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.DraggedPersonnageId, 1)
            .Add(p => p.OnPersonnageRequested, null));

        var method = typeof(EscouadePreviewEditor).GetMethod("HandleDrop", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 0 })!;
        await task;

        // Assert - no exception thrown, method returned early
        Assert.True(true);
    }

    [Fact]
    public async Task HandleDrop_ReturnsEarly_WhenPersonnageNotFound()
    {
        // Arrange
        Func<int, Task<Personnage?>> requestFunc = async (id) => await Task.FromResult<Personnage?>(null);

        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.DraggedPersonnageId, 1)
            .Add(p => p.OnPersonnageRequested, requestFunc));

        var method = typeof(EscouadePreviewEditor).GetMethod("HandleDrop", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 0 })!;
        await task;

        // Assert
        Assert.Empty(cut.Instance.PersonnagesSelectionnes);
    }

    [Fact]
    public async Task HandleDrop_AddsPersonnageToSlot_WhenTypeIsAllowed()
    {
        // Arrange
        var commandant = new Personnage { Id = 1, Nom = "Commander", Type = TypePersonnage.Commandant, Puissance = 50 };
        Func<int, Task<Personnage?>> requestFunc = async (id) => await Task.FromResult<Personnage?>(commandant);

        var selectionChanged = false;
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.DraggedPersonnageId, 1)
            .Add(p => p.OnPersonnageRequested, requestFunc)
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<List<int>>(this, (ids) => selectionChanged = true)));

        var method = typeof(EscouadePreviewEditor).GetMethod("HandleDrop", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 0 })!;
        await task;

        // Assert
        Assert.Single(cut.Instance.PersonnagesSelectionnes);
        Assert.Equal(commandant, cut.Instance.PersonnagesSelectionnes[0]);
        Assert.True(selectionChanged);
    }

    [Fact]
    public async Task HandleDrop_InvokesOnInvalidDrop_WhenTypeNotAllowed()
    {
        // Arrange
        var mercenaire = new Personnage { Id = 1, Nom = "Merc", Type = TypePersonnage.Mercenaire, Puissance = 30 };
        Func<int, Task<Personnage?>> requestFunc = async (id) => await Task.FromResult<Personnage?>(mercenaire);

        string? errorMessage = null;
        var cut = ctx.RenderComponent<EscouadePreviewEditor>(parameters => parameters
            .Add(p => p.DraggedPersonnageId, 1)
            .Add(p => p.OnPersonnageRequested, requestFunc)
            .Add(p => p.OnInvalidDrop, EventCallback.Factory.Create<string>(this, (msg) => errorMessage = msg)));

        var method = typeof(EscouadePreviewEditor).GetMethod("HandleDrop", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)method!.Invoke(cut.Instance, new object[] { 0 })!; // Slot 0 requires Commandant
        await task;

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("Commandant", errorMessage);
        Assert.Contains("Mercenaire", errorMessage);
    }

    [Fact]
    public void DragData_HasPersonnageIdProperty()
    {
        // Arrange
        var dragDataType = typeof(EscouadePreviewEditor).GetNestedType("DragData");

        // Assert
        Assert.NotNull(dragDataType);
        var personnageIdProperty = dragDataType.GetProperty("PersonnageId");
        Assert.NotNull(personnageIdProperty);
    }

    [Fact]
    public void DragData_HasNomProperty()
    {
        // Arrange
        var dragDataType = typeof(EscouadePreviewEditor).GetNestedType("DragData");

        // Assert
        Assert.NotNull(dragDataType);
        var nomProperty = dragDataType.GetProperty("Nom");
        Assert.NotNull(nomProperty);
    }
}
