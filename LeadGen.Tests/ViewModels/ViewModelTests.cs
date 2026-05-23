using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;
using LeadGen.Tests.Helpers;
using LeadGen.ViewModels;

namespace LeadGen.Tests.ViewModels;

public class MainViewModelTests
{
    private static MainViewModel CreateVm()
    {
        var dir = TestPaths.CreateTempDirectory();
        return new MainViewModel(
            Path.Combine(dir, "config.json"),
            Path.Combine(dir, "db.db"),
            null,
            null,
            null,
            null,
            new AppConfig());
    }

    [Fact]
    public void Navigate_ToProcessing_SwitchesCurrentViewModel()
    {
        using var _ = WpfApplicationFixture.EnsureApplication();
        var vm = CreateVm();

        vm.NavigateCommand.Execute("Processing");

        vm.CurrentPage.Should().Be(AppPage.Processing);
        vm.CurrentViewModel.Should().BeSameAs(vm.Processing);
    }

    [Fact]
    public void Navigate_InvalidParameter_DoesNotChangePage()
    {
        using var _ = WpfApplicationFixture.EnsureApplication();
        var vm = CreateVm();

        vm.NavigateCommand.Execute("Invalid");

        vm.CurrentPage.Should().Be(AppPage.Dashboard);
    }

    [Fact]
    public void Navigate_ToDashboard_DoesNotThrow()
    {
        using var _ = WpfApplicationFixture.EnsureApplication();
        var vm = CreateVm();

        var act = () => vm.NavigateCommand.Execute("Dashboard");
        act.Should().NotThrow();
        vm.CurrentPage.Should().Be(AppPage.Dashboard);
    }
}

public class DashboardViewModelTests
{
    [Theory]
    [InlineData(DashboardPeriod.Today, 0)]
    [InlineData(DashboardPeriod.Week, 7)]
    [InlineData(DashboardPeriod.Month, 30)]
    public void GetPeriodRange_ReturnsExpectedSpan(DashboardPeriod period, int minDaysBack)
    {
        var (from, to) = DashboardViewModel.GetPeriodRange(period);
        (to - from).TotalDays.Should().BeGreaterThanOrEqualTo(minDaysBack - 1);
    }

    [Fact]
    public void GetPeriodRange_AllTime_CapsAt365Days()
    {
        var (from, to) = DashboardViewModel.GetPeriodRange(DashboardPeriod.All);
        (to - from).TotalDays.Should().BeLessThanOrEqualTo(365);
    }
}
