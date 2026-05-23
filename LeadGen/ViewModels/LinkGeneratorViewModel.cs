using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadGen.Models;
using LeadGen.Services;
using LeadGen.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace LeadGen.ViewModels;

/// <summary>
/// ViewModel генератора ссылок Яндекс.Карт.
/// </summary>
public partial class LinkGeneratorViewModel : ObservableObject
{
    private readonly LinkGeneratorService _linkService;
    private readonly ConfigService _configService;
    private readonly DatabaseService _db;
    private readonly AppConfig _config;
    private readonly MainViewModel _main;

    private List<GeneratedLink> _lastLinks = [];

    [ObservableProperty] private string _segment = string.Empty;
    [ObservableProperty] private bool _includeDistricts;
    [ObservableProperty] private string _statusMessage = "Введите сегмент и выберите города";
    [ObservableProperty] private string _resultsText = string.Empty;
    [ObservableProperty] private int _linksCount;
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private int _selectedCitiesCount;
    [ObservableProperty] private string _cityFilter = string.Empty;
    [ObservableProperty] private bool _canCopyLinks;

    public ObservableCollection<CityItem> Cities { get; } = [];

    public ICollectionView CitiesView { get; private set; } = null!;

    public string CoverageSummary =>
        IncludeDistricts && DistrictCoverageCount > 0
            ? $"{SelectedCitiesCount} городов · {DistrictCoverageCount} районов"
            : $"{SelectedCitiesCount} из {Cities.Count} выбрано";

    public int DistrictCoverageCount =>
        IncludeDistricts
            ? Cities.Where(c => c.IsSelected).Sum(c =>
                _config.CityDistricts.TryGetValue(c.Name, out var districts) ? districts.Count : 0)
            : 0;

    public LinkGeneratorViewModel(
        LinkGeneratorService linkService,
        ConfigService configService,
        DatabaseService db,
        AppConfig config,
        MainViewModel main)
    {
        _linkService = linkService;
        _configService = configService;
        _db = db;
        _config = config;
        _main = main;
        EnsureCitiesExist();
        LoadCities();
    }

    private void EnsureCitiesExist()
    {
        if (_config.Regions.Count > 0)
            return;

        var answer = MessageBox.Show(
            "Список городов пуст. Восстановить стандартный список из 43 городов?",
            "LeadGen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
        {
            CityConfigEditor.RestoreDefaults(_config);
            _configService.Save(_config);
        }
    }

    public void LoadCities()
    {
        foreach (var city in Cities)
            city.PropertyChanged -= OnCityItemPropertyChanged;

        Cities.Clear();
        foreach (var city in _config.Regions)
        {
            var item = new CityItem
            {
                Name = city,
                Timezone = CityTimezones.GetTimezone(city, _config.CityTimezones) ?? "UTC+3",
                IsSelected = true
            };
            item.PropertyChanged += OnCityItemPropertyChanged;
            Cities.Add(item);
        }

        CitiesView = CollectionViewSource.GetDefaultView(Cities);
        CitiesView.Filter = FilterCity;
        UpdateSelectionStats();

        if (Cities.Count == 0)
            StatusMessage = "Нет городов — нажмите «Управление» для добавления или восстановления";
    }

    partial void OnCityFilterChanged(string value) => CitiesView?.Refresh();

    partial void OnIncludeDistrictsChanged(bool value)
    {
        OnPropertyChanged(nameof(CoverageSummary));
        OnPropertyChanged(nameof(DistrictCoverageCount));
    }

    partial void OnLinksCountChanged(int value) => CanCopyLinks = value > 0;

    private void OnCityItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CityItem.IsSelected))
            UpdateSelectionStats();
    }

    private bool FilterCity(object obj)
    {
        if (obj is not CityItem city)
            return false;

        return string.IsNullOrWhiteSpace(CityFilter)
               || city.Name.Contains(CityFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSelectionStats()
    {
        SelectedCitiesCount = Cities.Count(c => c.IsSelected);
        OnPropertyChanged(nameof(CoverageSummary));
        OnPropertyChanged(nameof(DistrictCoverageCount));
    }

    [RelayCommand]
    private void OpenCityManager()
    {
        try
        {
            var window = new CityManagerWindow(_config, _configService)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                LoadCities();
                StatusMessage = $"Загружено городов: {Cities.Count}";
            }
        }
        catch (Exception ex)
        {
            var details = ex.InnerException?.Message ?? ex.Message;
            StatusMessage = $"Ошибка окна управления: {details}";
            MessageBox.Show(
                $"Не удалось открыть окно управления:\n{details}",
                "LeadGen",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void SelectAllCities()
    {
        foreach (var city in Cities)
            city.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAllCities()
    {
        foreach (var city in Cities)
            city.IsSelected = false;
    }

    [RelayCommand]
    private async Task GenerateLinks()
    {
        if (string.IsNullOrWhiteSpace(Segment))
        {
            StatusMessage = "Введите сегмент (например: Металлоконструкции)";
            return;
        }

        var selected = Cities.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Выберите хотя бы один город";
            return;
        }

        if (IsGenerating)
            return;

        IsGenerating = true;

        try
        {
            await _main.RunWithLoadingAsync("Генерация ссылок...", async () =>
            {
                var segment = Segment.Trim();
                var includeDistricts = IncludeDistricts;
                var links = await Task.Run(() =>
                {
                    var regions = _linkService
                        .ExpandRegions(selected, _config.CityDistricts, includeDistricts)
                        .ToList();

                    var batch = _linkService.GenerateBatch(segment, regions);
                    _db.SaveLinkGeneration(segment, batch.Count, selected.Count);
                    return batch;
                });

                _lastLinks = links;

                await UiDispatcher.RunOnUiThreadAsync(() =>
                {
                    ResultsText = string.Join(Environment.NewLine,
                        links.Select(l => $"{l.Region}: {l.Link}"));
                    LinksCount = links.Count;
                    StatusMessage = $"Сгенерировано {links.Count} ссылок для «{segment}»";
                    _main.Dashboard.RefreshCommand.Execute(null);
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка генерации: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void CopyLinks()
    {
        if (_lastLinks.Count == 0)
            return;

        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, _lastLinks.Select(l => l.Link)));
            StatusMessage = $"Скопировано {_lastLinks.Count} ссылок";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось скопировать: {ex.Message}";
        }
    }
}
