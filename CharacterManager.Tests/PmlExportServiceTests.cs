using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Tests
{
    public class PmlExportServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PmlExportService _service;
        private bool _disposed;

        public PmlExportServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var loggerMock = new Mock<ILogger<PmlExportService>>();
            _service = new PmlExportService(_context, loggerMock.Object);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _context?.Dispose();
            }

            _disposed = true;
        }

        [Fact]
        public async Task ExportPmlAsync_WithDefaultOptions_ReturnsNonEmptyByteArray()
        {
            // Arrange
            _context.Personnages.Add(new Personnage
            {
                Nom = "TestChar",
                Type = TypePersonnage.Mercenaire,
                Rarete = Rarete.SSR,
                Puissance = 1000,
                Niveau = 1,
                Rang = 1
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExportPmlAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportPmlAsync_WithNoData_ReturnsValidXml()
        {
            // Act
            var result = await _service.ExportPmlAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            var xml = System.Text.Encoding.UTF8.GetString(result);
            Assert.Contains("CharacterManagerPML", xml);
            Assert.Contains("Version", xml);
        }

        [Fact]
        public async Task ExportPmlAsync_WithInventoryOption_IncludesPersonnages()
        {
            // Arrange
            _context.Personnages.Add(new Personnage
            {
                Nom = "Alice",
                Type = TypePersonnage.Mercenaire,
                Rarete = Rarete.SR,
                Puissance = 500,
                Niveau = 1,
                Rang = 1
            });
            await _context.SaveChangesAsync();

            var options = new PmlExportOptions(PmlExportOptions.EXPORT_TYPE_INVENTORY);

            // Act
            var result = await _service.ExportPmlAsync(options);

            // Assert
            Assert.NotNull(result);
            var xml = System.Text.Encoding.UTF8.GetString(result);
            Assert.Contains("Alice", xml);
        }

        [Fact]
        public async Task ExportPmlAsync_WithCustomExportOption_ExecutesSuccessfully()
        {
            // Arrange
            var options = new PmlExportOptions();
            options.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY);
            options.AddCustomExport("custom", new { data = "test" });

            // Act
            var result = await _service.ExportPmlAsync(options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }
    }
}
