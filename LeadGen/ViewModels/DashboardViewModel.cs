using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadGen.Helpers;
using LeadGen.Models;
using LeadGen.Services;
using OxyPlot;

namespace LeadGen.ViewModels;

/// <summary>
/// Дашборд со статистикой за выбранный период.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly MainViewModel _main;

    [ObservableProperty] private DashboardPeriod _selectedPeriod = DashboardPeriod.Month;
    [ObservableProperty] private int _filesProcessed;
    [ObservableProperty] private int _rowsProcessed;
    [ObservableProperty] private int _linksGenerated;
    [ObservableProperty] private int _linkSessions;
    [ObservableProperty] private int _duplicatesRemoved;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private List<ActivityItem> _recentActivity = [];
    [ObservableProperty] private PlotModel? _activityChart;
    [ObservableProperty] private string? _errorMessage;

    public DashboardViewModel(DatabaseService db, MainViewModel main)
    {
        _db = db;
        _main = main;
        ActivityChart = DashboardChartBuilder.Build([]);
    }

    /// <summary>
    /// Вызывается из View при Loaded — не из конструктора.
    /// </summary>
    public void Initialize() => _ = Refresh();

    partial void OnSelectedPeriodChanged(DashboardPeriod value) => _ = Refresh();

    [RelayCommand]
    private async Task Refresh()
    {
        if (IsRefreshing)
            return;

        IsRefreshing = true;
        ErrorMessage = null;

        try
        {
            await _main.RunWithLoadingAsync("Обновление статистики...", async () =>
            {
                var period = SelectedPeriod;
                var stats = await Task.Run(() =>
                {
                    var (from, to) = GetPeriodRange(period);
                    return _db.GetDashboardStats(from, to);
                });

                await UiDispatcher.RunOnUiThreadAsync(() => ApplyStats(stats));
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки статистики: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void ApplyStats(DashboardStats stats)
    {
        AnimateStat(v => FilesProcessed = v, FilesProcessed, stats.FilesProcessed);
        AnimateStat(v => RowsProcessed = v, RowsProcessed, stats.RowsProcessed);
        AnimateStat(v => LinksGenerated = v, LinksGenerated, stats.LinksGenerated);
        AnimateStat(v => LinkSessions = v, LinkSessions, stats.LinkSessions);
        AnimateStat(v => DuplicatesRemoved = v, DuplicatesRemoved, stats.DuplicatesRemoved);
        RecentActivity = stats.RecentActivity;
        ActivityChart = DashboardChartBuilder.Build(stats.DailyActivity);
    }

    [RelayCommand]
    private void SetPeriod(string? period)
    {
        if (period is not null && Enum.TryParse<DashboardPeriod>(period, out var p))
            SelectedPeriod = p;
    }

    private static void AnimateStat(Action<int> setter, int from, int to) =>
        AnimationHelper.AnimateCounter(setter, from, to);

    internal static (DateTime from, DateTime to) GetPeriodRange(DashboardPeriod period)
    {
        var to = DateTime.Now;
        var from = period switch
        {
            DashboardPeriod.Today => to.Date,
            DashboardPeriod.Week => to.AddDays(-7),
            DashboardPeriod.Month => to.AddDays(-30),
            DashboardPeriod.Quarter => to.AddDays(-90),
            DashboardPeriod.All => to.AddDays(-365),
            _ => to.AddDays(-30)
        };
        return (from, to);
    }
}
