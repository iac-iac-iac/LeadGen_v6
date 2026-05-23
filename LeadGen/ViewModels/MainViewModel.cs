using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadGen.Helpers;
using LeadGen.Models;
using LeadGen.Services;
using System.IO;

namespace LeadGen.ViewModels;

/// <summary>
/// Корневой ViewModel — навигация и общие сервисы.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public ConfigService ConfigService { get; }
    public DatabaseService DatabaseService { get; }
    public AppConfig Config { get; }

    public DashboardViewModel Dashboard { get; }
    public ProcessingViewModel Processing { get; }
    public LinkGeneratorViewModel LinkGenerator { get; }

    [ObservableProperty]
    private AppPage _currentPage = AppPage.Dashboard;

    public object CurrentViewModel => CurrentPage switch
    {
        AppPage.Processing => Processing,
        AppPage.LinkGenerator => LinkGenerator,
        _ => Dashboard
    };

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPremiumLoading;

    [ObservableProperty]
    private double _loadingProgress;

    [ObservableProperty]
    private string _loadingStageText = string.Empty;

    [ObservableProperty]
    private string _loadingMessage = "Загрузка...";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _animationsEnabled = true;

    public MainViewModel()
        : this(
            null,
            null,
            null,
            null,
            null,
            null,
            null)
    {
    }

    internal MainViewModel(
        string? configPath,
        string? databasePath,
        ConfigService? configService,
        DatabaseService? databaseService,
        LeadProcessingService? processingService,
        LinkGeneratorService? linkGeneratorService,
        AppConfig? config)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        ConfigService = configService ?? new ConfigService(configPath ?? Path.Combine(baseDir, "config.json"));
        Config = config ?? ConfigService.Load();
        AnimationSettings.Initialize(Config.Ui.AnimationsEnabled);

        var resolvedDb = databasePath ?? PathSettings.Resolve(baseDir, Config.Paths.Database);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedDb) ?? Path.Combine(baseDir, "data"));
        Directory.CreateDirectory(PathSettings.Resolve(baseDir, Config.Paths.InputDir));
        Directory.CreateDirectory(PathSettings.Resolve(baseDir, Config.Paths.OutputDir));

        DatabaseService = databaseService ?? new DatabaseService(resolvedDb);

        Dashboard = new DashboardViewModel(DatabaseService, this);
        Processing = new ProcessingViewModel(
            processingService ?? new LeadProcessingService(),
            DatabaseService,
            Config,
            this);
        LinkGenerator = new LinkGeneratorViewModel(
            linkGeneratorService ?? new LinkGeneratorService(),
            ConfigService,
            DatabaseService,
            Config,
            this);

        _animationsEnabled = Config.Ui.AnimationsEnabled;
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        Config.Ui.AnimationsEnabled = value;
        AnimationSettings.SetEnabled(value);
        try
        {
            ConfigService.Save(Config);
        }
        catch
        {
            // ignore save errors during toggle
        }
    }

    [RelayCommand]
    private void Navigate(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return;

        if (!Enum.TryParse<AppPage>(page, out var target))
            return;

        try
        {
            CurrentPage = target;
            OnPropertyChanged(nameof(CurrentViewModel));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка навигации: {ex.Message}";
        }
    }

    public async Task RunWithLoadingAsync(string message, Func<Task> action)
    {
        LoadingMessage = message;
        IsLoading = true;
        try
        {
            await Task.Delay(150);
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RunWithPremiumLoadingAsync(
        string initialMessage,
        List<(double UpToPercent, string Message)> stages,
        Func<Task> action,
        double totalDurationMs = 3200)
    {
        LoadingMessage = initialMessage;
        IsPremiumLoading = true;
        LoadingProgress = 0;
        LoadingStageText = stages.Count > 0 ? stages[0].Message : string.Empty;
        IsLoading = true;

        try
        {
            // Запускаем реальную фоновую операцию
            var realTask = Task.Run(action);

            // Параллельно запускаем плавное нелинейное нарастание прогресса на UI-потоке
            var start = DateTime.Now;
            
            while (true)
            {
                var elapsed = DateTime.Now - start;
                var progressRatio = Math.Min(1.0, elapsed.TotalMilliseconds / totalDurationMs);

                // Нелинейный закон нарастания: замедление в середине (30% - 75% на дедупликацию)
                double easedProgress;
                if (progressRatio < 0.3)
                {
                    // Быстрый старт (0% - 30%)
                    easedProgress = (progressRatio / 0.3) * 0.3;
                }
                else if (progressRatio < 0.8)
                {
                    // Медленная, тягучая середина (30% - 75%)
                    double midRatio = (progressRatio - 0.3) / 0.5; // от 0 до 1
                    double easedMid = Math.Sin(midRatio * Math.PI / 2); // плавная синусоида
                    easedProgress = 0.3 + (easedMid * 0.45); // от 30% до 75%
                }
                else
                {
                    // Финальное ускорение (75% - 100%)
                    double endRatio = (progressRatio - 0.8) / 0.2; // от 0 до 1
                    easedProgress = 0.75 + (endRatio * 0.25); // от 75% до 100%
                }

                double progressPercent = easedProgress * 100.0;
                LoadingProgress = Math.Min(100.0, progressPercent);

                // Подбираем текст текущей стадии на основе текущего прогресса
                foreach (var stage in stages)
                {
                    if (progressPercent <= stage.UpToPercent)
                    {
                        if (LoadingStageText != stage.Message)
                        {
                            LoadingStageText = stage.Message;
                        }
                        break;
                    }
                }

                if (progressRatio >= 1.0)
                {
                    break;
                }

                await Task.Delay(16); // ~60fps
            }

            // Ждем завершения реальной фоновой задачи
            await realTask;

            // Доводим до 100%
            LoadingProgress = 100;
            if (stages.Count > 0)
            {
                LoadingStageText = stages[^1].Message;
            }

            // Фиксация 100%
            await Task.Delay(150);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsLoading = false;
            // Ждем окончания анимации скрытия оверлея
            await Task.Delay(350);
            IsPremiumLoading = false;
            LoadingProgress = 0;
            LoadingStageText = string.Empty;
        }
    }
}
