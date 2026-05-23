namespace LeadGen.Models;

/// <summary>
/// Точка данных для графика активности по дням.
/// </summary>
public class DailyActivityPoint
{
    public DateTime Date { get; set; }
    public string Label => Date.ToString("dd.MM");
    public int RowsProcessed { get; set; }
    public int LinksGenerated { get; set; }
    public int FilesProcessed { get; set; }
}
