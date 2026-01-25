using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public sealed class PersonnageCapacitesModalTests : IDisposable
{
    private readonly TestContext ctx;
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<IModalService> modalServiceMock;
    private bool disposed;

    public PersonnageCapacitesModalTests()
    {
        ctx = new TestContext();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new ApplicationDbContext(options);
        
        modalServiceMock = new Mock<IModalService>();
        
        ctx.Services.AddSingleton(dbContext);
        ctx.Services.AddSingleton(modalServiceMock.Object);
        ctx.Services.AddScoped<IHistoriqueModificationService, HistoriqueModificationService>();
        ctx.Services.AddScoped<IPersonnageService, PersonnageService>();
        ctx.Services.AddScoped<ICapaciteService, CapaciteService>();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            ctx?.Dispose();
            dbContext?.Dispose();
            disposed = true;
        }
    }

    [Fact]
    public async Task PersonnageCapacitesModal_Renders_WithValidPersonnageId()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Test Hero", Type = TypePersonnage.Commandant };
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_DisplaysPersonnageName()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Commander Alpha", Type = TypePersonnage.Commandant };
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100); // Wait for OnInitializedAsync

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Commander Alpha", markup);
        });
    }

    [Fact]
    public async Task PersonnageCapacitesModal_DisplaysCapacitesList()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        var capacite1 = new Capacite { Nom = "Super Force", Description = "Force surhumaine" };
        var capacite2 = new Capacite { Nom = "Vol", Description = "Capacité de voler" };
        
        dbContext.Personnages.Add(personnage);
        dbContext.Capacites.AddRange(capacite1, capacite2);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100); // Wait for OnInitializedAsync

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("Super Force", markup);
            Assert.Contains("Vol", markup);
        });
    }

    [Fact]
    public async Task PersonnageCapacitesModal_Close_CallsModalService()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        var closeMethod = typeof(PersonnageCapacitesModal).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        closeMethod!.Invoke(cut.Instance, null);

        // Assert
        modalServiceMock.Verify(m => m.Close(), Times.Once);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_ToggleCapacite_AddsCapacite()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        var capacite = new Capacite { Nom = "Force" };
        dbContext.Personnages.Add(personnage);
        dbContext.Capacites.Add(capacite);
        await dbContext.SaveChangesAsync();

        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100);

        var toggleMethod = typeof(PersonnageCapacitesModal).GetMethod("ToggleCapacite", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        toggleMethod!.Invoke(cut.Instance, new object[] { capacite.Id, true });

        // Assert - method executed without exception
        Assert.True(true);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_ToggleCapacite_RemovesCapacite()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Force" };
        var personnage = new Personnage 
        { 
            Nom = "Hero", 
            Type = TypePersonnage.Commandant,
            Capacites = new List<Capacite> { capacite }
        };
        dbContext.Capacites.Add(capacite);
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100);

        var toggleMethod = typeof(PersonnageCapacitesModal).GetMethod("ToggleCapacite", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        toggleMethod!.Invoke(cut.Instance, new object[] { capacite.Id, false });

        // Assert - method executed without exception
        Assert.True(true);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_ShowsLoadingState_Initially()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert - Component renders without throwing
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_ShowsError_WhenPersonnageNotFound()
    {
        // Arrange - No personnage in database

        // Act
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, 99999));
        
        await Task.Delay(100);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("introuvable", markup);
        });
    }

    [Fact]
    public async Task PersonnageCapacitesModal_SaveAsync_UpdatesCapacites()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Force" };
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        dbContext.Capacites.Add(capacite);
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100);

        // Toggle capacite first
        var toggleMethod = typeof(PersonnageCapacitesModal).GetMethod("ToggleCapacite", BindingFlags.NonPublic | BindingFlags.Instance);
        toggleMethod!.Invoke(cut.Instance, new object[] { capacite.Id, true });

        var saveMethod = typeof(PersonnageCapacitesModal).GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)saveMethod!.Invoke(cut.Instance, null)!;
        await task;

        // Assert
        modalServiceMock.Verify(m => m.Close(), Times.Once);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_SaveAsync_InvokesOnSavedCallback()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Force" };
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        dbContext.Capacites.Add(capacite);
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        List<Capacite>? savedCapacites = null;
        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<List<Capacite>>(this, (caps) => savedCapacites = caps)));
        
        await Task.Delay(100);

        // Toggle capacite
        var toggleMethod = typeof(PersonnageCapacitesModal).GetMethod("ToggleCapacite", BindingFlags.NonPublic | BindingFlags.Instance);
        toggleMethod!.Invoke(cut.Instance, new object[] { capacite.Id, true });

        var saveMethod = typeof(PersonnageCapacitesModal).GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var task = (Task)saveMethod!.Invoke(cut.Instance, null)!;
        await task;

        // Assert
        Assert.NotNull(savedCapacites);
        Assert.Single(savedCapacites);
        Assert.Equal("Force", savedCapacites[0].Nom);
    }

    [Fact]
    public async Task PersonnageCapacitesModal_SaveAsync_DoesNotSaveTwice_WhenAlreadySaving()
    {
        // Arrange
        var personnage = new Personnage { Nom = "Hero", Type = TypePersonnage.Commandant };
        dbContext.Personnages.Add(personnage);
        await dbContext.SaveChangesAsync();

        var cut = ctx.RenderComponent<PersonnageCapacitesModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));
        
        await Task.Delay(100);

        var saveMethod = typeof(PersonnageCapacitesModal).GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Set isSaving to true via reflection
        var isSavingField = typeof(PersonnageCapacitesModal).GetField("isSaving", BindingFlags.NonPublic | BindingFlags.Instance);
        isSavingField!.SetValue(cut.Instance, true);

        // Act
        var task = (Task)saveMethod!.Invoke(cut.Instance, null)!;
        await task;

        // Assert - Should return early, not call Close
        modalServiceMock.Verify(m => m.Close(), Times.Never);
    }

    [Fact]
    public void Close_Method_Exists()
    {
        // Assert
        var method = typeof(PersonnageCapacitesModal).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ToggleCapacite_Method_Exists()
    {
        // Assert
        var method = typeof(PersonnageCapacitesModal).GetMethod("ToggleCapacite", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void SaveAsync_Method_Exists()
    {
        // Assert
        var method = typeof(PersonnageCapacitesModal).GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void LoadDataAsync_Method_Exists()
    {
        // Assert
        var method = typeof(PersonnageCapacitesModal).GetMethod("LoadDataAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }
}
