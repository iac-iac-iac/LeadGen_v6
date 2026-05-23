using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadGen.Models;
using LeadGen.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace LeadGen.ViewModels;

/// <summary>
/// ViewModel диалога управления городами и районами (как DistrictManagerDialog в v5).
/// </summary>
public partial class CityManagerViewModel : ObservableObject
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;

    [ObservableProperty] private ManagedCityItem? _selectedCity;
    [ObservableProperty] private string _newCityName = string.Empty;
    [ObservableProperty] private string _newCityTimezone = "UTC+3";
    [ObservableProperty] private string _editCityTimezone = string.Empty;
    [ObservableProperty] private string _newDistrictName = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isErrorStatus;

    public ObservableCollection<ManagedCityItem> Cities { get; } = [];
    public ObservableCollection<string> Districts { get; } = [];

    public event Action<bool>? CloseRequested;

    public CityManagerViewModel(AppConfig config, ConfigService configService)
    {
        _config = config;
        _configService = configService;
        ReloadCities();
    }

    partial void OnSelectedCityChanged(ManagedCityItem? value)
    {
        EditCityTimezone = value?.Timezone ?? string.Empty;
        ReloadDistricts();
    }

    [RelayCommand]
    private void AddCity()
    {
        if (!CityConfigEditor.AddCity(_config, NewCityName, NewCityTimezone, out var error))
        {
            SetStatus(error!, true);
            return;
        }

        NewCityName = string.Empty;
        ReloadCities();
        SetStatus("Город добавлен", false);
    }

    [RelayCommand]
    private void UpdateTimezone()
    {
        if (SelectedCity is null)
        {
            SetStatus("Выберите город", true);
            return;
        }

        if (!CityConfigEditor.UpdateCityTimezone(_config, SelectedCity.Name, EditCityTimezone, out var error))
        {
            SetStatus(error!, true);
            return;
        }

        SelectedCity.Timezone = EditCityTimezone.Trim();
        ReloadCities();
        SetStatus("Часовой пояс обновлён", false);
    }

    [RelayCommand]
    private void DeleteSelectedCities()
    {
        var toDelete = Cities.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        if (toDelete.Count == 0)
        {
            SetStatus("Отметьте города галочками для удаления", true);
            return;
        }

        var preview = string.Join(", ", toDelete.Take(5));
        if (toDelete.Count > 5)
            preview += $" и ещё {toDelete.Count - 5}";

        var confirm = MessageBox.Show(
            $"Удалить города:\n{preview}?\n\nРайоны этих городов также будут удалены.",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        CityConfigEditor.RemoveCities(_config, toDelete);
        SelectedCity = null;
        ReloadCities();
        ReloadDistricts();
        SetStatus($"Удалено городов: {toDelete.Count}", false);
    }

    [RelayCommand]
    private void AddDistrict()
    {
        if (SelectedCity is null)
        {
            SetStatus("Выберите один город для добавления района", true);
            return;
        }

        if (!CityConfigEditor.AddDistrict(_config, SelectedCity.Name, NewDistrictName, out var error))
        {
            SetStatus(error!, true);
            return;
        }

        NewDistrictName = string.Empty;
        ReloadDistricts();
        SetStatus("Район добавлен", false);
    }

    [RelayCommand]
    private void DeleteSelectedDistricts(object? parameter)
    {
        if (SelectedCity is null)
        {
            SetStatus("Выберите город", true);
            return;
        }

        var selected = ExtractSelectedDistricts(parameter);
        if (selected.Count == 0)
        {
            SetStatus("Выберите районы для удаления", true);
            return;
        }

        var confirm = MessageBox.Show(
            $"Удалить районы ({selected.Count}) города «{SelectedCity.Name}»?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        CityConfigEditor.RemoveDistricts(_config, SelectedCity.Name, selected);
        ReloadDistricts();
        SetStatus($"Удалено районов: {selected.Count}", false);
    }

    [RelayCommand]
    private void RestoreDefaults()
    {
        var confirm = MessageBox.Show(
            "Восстановить стандартный список из 43 городов и районов Москвы/СПб?\n\nТекущий список будет заменён.",
            "Восстановление",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        CityConfigEditor.RestoreDefaults(_config);
        SelectedCity = null;
        ReloadCities();
        ReloadDistricts();
        SetStatus("Стандартный список восстановлен", false);
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            _configService.Save(_config);
            CloseRequested?.Invoke(true);
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка сохранения: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private void ReloadCities()
    {
        var selectedName = SelectedCity?.Name;
        Cities.Clear();
        foreach (var name in _config.Regions.OrderBy(n => n))
        {
            var item = new ManagedCityItem
            {
                Name = name,
                Timezone = CityConfigEditor.GetTimezone(_config, name)
            };
            Cities.Add(item);
            if (name == selectedName)
                SelectedCity = item;
        }
    }

    private void ReloadDistricts()
    {
        Districts.Clear();
        if (SelectedCity is null)
            return;

        if (_config.CityDistricts.TryGetValue(SelectedCity.Name, out var list))
        {
            foreach (var d in list.OrderBy(x => x))
                Districts.Add(d);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsErrorStatus = isError;
    }

    private static List<string> ExtractSelectedDistricts(object? parameter) => parameter switch
    {
        IList<string> strings => strings.ToList(),
        System.Collections.IList collection => collection.Cast<object>().OfType<string>().ToList(),
        _ => []
    };
}
