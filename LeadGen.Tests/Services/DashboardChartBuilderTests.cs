using FluentAssertions;
using LeadGen.Helpers;
using LeadGen.Models;

namespace LeadGen.Tests.Services;

public class DashboardChartBuilderTests
{
    [Fact]
    public void Build_EmptyPoints_DoesNotThrow()
    {
        var act = () => DashboardChartBuilder.Build([]);
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_SinglePoint_DoesNotThrow()
    {
        var points = new List<DailyActivityPoint>
        {
            new() { Date = DateTime.Today, RowsProcessed = 5, LinksGenerated = 2, FilesProcessed = 1 }
        };

        var act = () => DashboardChartBuilder.Build(points);
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_MultiplePoints_CreatesThreeSeries()
    {
        var points = Enumerable.Range(0, 7).Select(i => new DailyActivityPoint
        {
            Date = DateTime.Today.AddDays(-i),
            RowsProcessed = i,
            LinksGenerated = i * 2,
            FilesProcessed = 1
        }).ToList();

        var model = DashboardChartBuilder.Build(points);
        model.Series.Count.Should().Be(3);
    }
}
