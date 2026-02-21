using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

public class TeamPowerTimelineServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStatistiquesService> _statistiquesServiceMock;
    private readonly TeamPowerTimelineService _service;
    private bool _disposed = false;

    public TeamPowerTimelineServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _statistiquesServiceMock = new Mock<IStatistiquesService>();
        _service = new TeamPowerTimelineService(_context, _statistiquesServiceMock.Object);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }

    ~TeamPowerTimelineServiceTests()
    {
        Dispose(false);
    }

    #region RecomputeAllAsync Tests

    [Fact]
    public async Task RecomputeAllAsync_ShouldPopulateTimelineWithAllData()
    {
        // Arrange
        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-2)), TotalPower = 1000 },
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), TotalPower = 1100 },
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now), TotalPower = 1200 }
        };

        var bestData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-2)), TotalPower = 1500 },
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), TotalPower = 1600 },
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now), TotalPower = 1700 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(bestData);

        // Act
        await _service.RecomputeAllAsync();

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Equal(6, records.Count); // 3 selected + 3 best
        Assert.Equal(3, records.Count(r => r.Type == TeamPowerTimelineType.Selected));
        Assert.Equal(3, records.Count(r => r.Type == TeamPowerTimelineType.Best));
    }

    [Fact]
    public async Task RecomputeAllAsync_ShouldClearPreviousData()
    {
        // Arrange
        var oldRecord = new TeamPowerTimelineRecord
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 500,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(oldRecord);
        await _context.SaveChangesAsync();

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now), TotalPower = 1000 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeAllAsync();

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Single(records);
        Assert.DoesNotContain(records, r => r.Id == oldRecord.Id);
    }

    [Fact]
    public async Task RecomputeAllAsync_ShouldSetCorrectPowerValues()
    {
        // Arrange
        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now), TotalPower = 1234 }
        };

        var bestData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = DateOnly.FromDateTime(DateTime.Now), TotalPower = 5678 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(bestData);

        // Act
        await _service.RecomputeAllAsync();

        // Assert
        var selectedRecord = await _context.TeamPowerTimelineRecords
            .FirstAsync(r => r.Type == TeamPowerTimelineType.Selected);
        var bestRecord = await _context.TeamPowerTimelineRecords
            .FirstAsync(r => r.Type == TeamPowerTimelineType.Best);

        Assert.Equal(1234, selectedRecord.TotalPower);
        Assert.Equal(5678, bestRecord.TotalPower);
    }

    [Fact]
    public async Task RecomputeAllAsync_WithEmptyData_ShouldNotCreateRecords()
    {
        // Arrange
        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeAllAsync();

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Empty(records);
    }

    #endregion

    #region RecomputeFromDateAsync Tests

    [Fact]
    public async Task RecomputeFromDateAsync_ShouldAddNewRecordsOnOrAfterDate()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        var oldDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5));
        var newDate = DateOnly.FromDateTime(DateTime.Now);

        var oldRecord = new TeamPowerTimelineRecord
        {
            Date = oldDate,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 500,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(oldRecord);
        await _context.SaveChangesAsync();

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = oldDate, TotalPower = 500 },
            new TeamPowerEvolutionData { Date = newDate, TotalPower = 1000 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeFromDateAsync(startDate);

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.NotNull(records.FirstOrDefault(r => r.Date == newDate && r.Type == TeamPowerTimelineType.Selected));
    }

    [Fact]
    public async Task RecomputeFromDateAsync_ShouldUpdateExistingRecordsWithChangedValues()
    {
        // Arrange
        var updateDate = DateOnly.FromDateTime(DateTime.Now);

        var existingRecord = new TeamPowerTimelineRecord
        {
            Date = updateDate,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 1000,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(existingRecord);
        await _context.SaveChangesAsync();

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = updateDate, TotalPower = 2000 }  // Updated value
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeFromDateAsync(updateDate);

        // Assert
        var updatedRecord = await _context.TeamPowerTimelineRecords
            .FirstAsync(r => r.Date == updateDate && r.Type == TeamPowerTimelineType.Selected);
        Assert.Equal(2000, updatedRecord.TotalPower);
    }

    [Fact]
    public async Task RecomputeFromDateAsync_ShouldRemoveRecordsNoLongerInData()
    {
        // Arrange
        var removeDate = DateOnly.FromDateTime(DateTime.Now);

        var oldRecord = new TeamPowerTimelineRecord
        {
            Date = removeDate,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 1000,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(oldRecord);
        await _context.SaveChangesAsync();

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());  // No data

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeFromDateAsync(removeDate);

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.DoesNotContain(records, r => r.Id == oldRecord.Id);
    }

    [Fact]
    public async Task RecomputeFromDateAsync_ShouldKeepOlderRecords()
    {
        // Arrange
        var oldDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5));
        var updateDate = DateOnly.FromDateTime(DateTime.Now);

        var oldRecord = new TeamPowerTimelineRecord
        {
            Date = oldDate,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 500,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(oldRecord);
        await _context.SaveChangesAsync();

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = oldDate, TotalPower = 500 },
            new TeamPowerEvolutionData { Date = updateDate, TotalPower = 1000 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.RecomputeFromDateAsync(updateDate);

        // Assert
        var oldRecordStillExists = await _context.TeamPowerTimelineRecords
            .FirstOrDefaultAsync(r => r.Date == oldDate);
        Assert.NotNull(oldRecordStillExists);
        Assert.Equal(500, oldRecordStillExists.TotalPower);
    }

    [Fact]
    public async Task RecomputeFromDateAsync_ShouldHandleBothSelectedAndBestTypes()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now);
        var startDate = date;

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date, TotalPower = 1000 }
        };

        var bestData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date, TotalPower = 1500 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(bestData);

        // Act
        await _service.RecomputeFromDateAsync(startDate);

        // Assert
        var selectedRecord = await _context.TeamPowerTimelineRecords
            .FirstOrDefaultAsync(r => r.Date == date && r.Type == TeamPowerTimelineType.Selected);
        var bestRecord = await _context.TeamPowerTimelineRecords
            .FirstOrDefaultAsync(r => r.Date == date && r.Type == TeamPowerTimelineType.Best);

        Assert.NotNull(selectedRecord);
        Assert.NotNull(bestRecord);
        Assert.Equal(1000, selectedRecord.TotalPower);
        Assert.Equal(1500, bestRecord.TotalPower);
    }

    #endregion

    #region SeedIfEmptyAsync Tests

    [Fact]
    public async Task SeedIfEmptyAsync_WithEmptyDatabase_ShouldPopulate()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now);
        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date, TotalPower = 1000 }
        };

        var bestData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date, TotalPower = 1500 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(bestData);

        // Act
        await _service.SeedIfEmptyAsync();

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_WithExistingData_ShouldNotDuplicate()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now);
        var existingRecord = new TeamPowerTimelineRecord
        {
            Date = date,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = 1000,
            DateInsertion = DateTime.Now
        };
        _context.TeamPowerTimelineRecords.Add(existingRecord);
        await _context.SaveChangesAsync();

        var selectedData = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date, TotalPower = 1000 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.SeedIfEmptyAsync();

        // Assert
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_ShouldNotThrowOnError()
    {
        // Arrange - Mock service to return data
        var date = DateOnly.FromDateTime(DateTime.Now);
        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData> { new TeamPowerEvolutionData { Date = date, TotalPower = 1000 } });

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act
        await _service.SeedIfEmptyAsync();

        // Assert - Should not throw and should create the record
        var records = await _context.TeamPowerTimelineRecords.ToListAsync();
        Assert.Single(records);
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task MultipleRecomputes_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var date1 = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        var date2 = DateOnly.FromDateTime(DateTime.Now);

        // Initial data
        var selectedData1 = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date1, TotalPower = 1000 },
            new TeamPowerEvolutionData { Date = date2, TotalPower = 1100 }
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData1);

        _statistiquesServiceMock
            .Setup(s => s.GetBestTeamPowerEvolutionData())
            .Returns(new List<TeamPowerEvolutionData>());

        // Act - First recompute
        await _service.RecomputeAllAsync();

        // Modify data
        var selectedData2 = new List<TeamPowerEvolutionData>
        {
            new TeamPowerEvolutionData { Date = date2, TotalPower = 1200 }  // Only date2, with updated value
        };

        _statistiquesServiceMock
            .Setup(s => s.GetSelectedTeamPowerEvolutionData())
            .Returns(selectedData2);

        // Act - Recompute from date2
        await _service.RecomputeFromDateAsync(date2);

        // Assert
        var records = await _context.TeamPowerTimelineRecords
            .OrderBy(r => r.Date)
            .ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.Equal(date1, records[0].Date);
        Assert.Equal(1000, records[0].TotalPower);
        Assert.Equal(date2, records[1].Date);
        Assert.Equal(1200, records[1].TotalPower);
    }

    #endregion
}
