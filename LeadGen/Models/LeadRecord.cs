namespace LeadGen.Models;

/// <summary>
/// Промежуточная запись лида после обработки.
/// </summary>
public class LeadRecord
{
    public string LeadTitle { get; set; } = string.Empty;
    public string WorkPhone { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Telegram { get; set; } = string.Empty;
    public string Vk { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PhoneSource { get; set; } = string.Empty;
    public string Manager { get; set; } = "Не назначен";
}

public class ProcessingResult
{
    public List<LeadRecord> Leads { get; set; } = [];
    public int FilesProcessed { get; set; }
    public int TotalRows { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int DuplicatesByPhone { get; set; }
    public int DuplicatesByName { get; set; }
    public int RowsRemovedNoLocation { get; set; }
    public long ProcessingTimeMs { get; set; }
}

public class GeneratedLink
{
    public string Segment { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}

public class DashboardStats
{
    public int FilesProcessed { get; set; }
    public int RowsProcessed { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int LinksGenerated { get; set; }
    public int LinkSessions { get; set; }
    public List<ActivityItem> RecentActivity { get; set; } = [];
    public List<DailyActivityPoint> DailyActivity { get; set; } = [];
}

public class ActivityItem
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}

public class CityItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isSelected = true;

    public string Name { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(Timezone) ? Name : $"{Name} · {Timezone}";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public enum AppPage
{
    Dashboard,
    Processing,
    LinkGenerator
}

public enum DashboardPeriod
{
    Today,
    Week,
    Month,
    Quarter,
    All
}
