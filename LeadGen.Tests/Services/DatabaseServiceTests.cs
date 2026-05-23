using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;
using LeadGen.Tests.Helpers;

namespace LeadGen.Tests.Services;

public class DatabaseServiceTests
{
    [Fact]
    public void GetDashboardStats_ReturnsAggregatedData()
    {
        var dir = TestPaths.CreateTempDirectory();
        var dbPath = Path.Combine(dir, "test.db");
        var db = new DatabaseService(dbPath);

        db.SaveProcessingHistory("file1.json", 100, 5, 120);
        db.SaveLinkGeneration("Металл", 10, 2);

        var stats = db.GetDashboardStats(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

        stats.FilesProcessed.Should().Be(1);
        stats.RowsProcessed.Should().Be(100);
        stats.LinksGenerated.Should().Be(10);
        stats.DailyActivity.Should().NotBeEmpty();
    }

    [Fact]
    public void GetDailyActivity_LimitsRangeTo365Days()
    {
        var dir = TestPaths.CreateTempDirectory();
        var db = new DatabaseService(Path.Combine(dir, "test.db"));

        var points = db.GetDailyActivity(DateTime.MinValue, DateTime.Now);

        points.Count.Should().BeLessThanOrEqualTo(365);
    }
}
