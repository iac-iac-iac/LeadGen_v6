namespace LeadGen.Models;

/// <summary>
/// Город в диалоге управления (список слева).
/// </summary>
public class ManagedCityItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isSelected;

    public string Name { get; init; } = string.Empty;
    public string Timezone
    {
        get => _timezone;
        set
        {
            if (SetProperty(ref _timezone, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _timezone = "UTC+3";

    public string DisplayName => $"{Name} ({Timezone})";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
