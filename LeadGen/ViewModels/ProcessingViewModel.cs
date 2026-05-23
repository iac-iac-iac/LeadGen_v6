using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadGen.Models;
using LeadGen.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace LeadGen.ViewModels;

/// <summary>
/// ViewModel обработки лидов.
/// </summary>
public partial class ProcessingViewModel : ObservableObject
{
    private readonly LeadProcessingService _processor;
    private readonly DatabaseService _db;
    private readonly AppConfig _config;
    private readonly MainViewModel _main;

    private ProcessingResult? _lastResult;

    [ObservableProperty] private string _managersText = string.Empty;
    [ObservableProperty] private string _statusMessage = "Перетащите файлы или нажмите для выбора";
    [ObservableProperty] private bool _canExport;
    [ObservableProperty] private bool _isProcessing;

    public ObservableCollection<string> LoadedFiles { get; } = [];

    public ProcessingViewModel(
        LeadProcessingService processor,
        DatabaseService db,
        AppConfig config,
        MainViewModel main)
    {
        _processor = processor;
        _db = db;
        _config = config;
        _main = main;
        ManagersText = config.Managers.Count > 0
            ? string.Join('\n', config.Managers)
            : string.Empty;
    }

    [RelayCommand]
    private void AddFiles()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Файлы Webbee (*.json;*.tsv;*.csv)|*.json;*.tsv;*.csv|Все файлы|*.*"
            };

            if (dialog.ShowDialog() == true)
                AddFilePaths(dialog.FileNames);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка выбора файла: {ex.Message}";
        }
    }

    public void AddFilePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!LoadedFiles.Contains(path))
                LoadedFiles.Add(path);
        }

        StatusMessage = $"Загружено файлов: {LoadedFiles.Count}";
        CanExport = false;
    }

    [RelayCommand]
    private void RemoveFile(string? path)
    {
        if (path is not null && LoadedFiles.Contains(path))
            LoadedFiles.Remove(path);

        StatusMessage = LoadedFiles.Count == 0
            ? "Перетащите файлы или нажмите для выбора"
            : $"Загружено файлов: {LoadedFiles.Count}";
        CanExport = false;
    }

    [RelayCommand]
    private void ClearFiles()
    {
        LoadedFiles.Clear();
        CanExport = false;
        StatusMessage = "Перетащите файлы или нажмите для выбора";
    }

    [RelayCommand]
    private async Task ProcessFiles()
    {
        if (LoadedFiles.Count == 0)
        {
            StatusMessage = "Добавьте хотя бы один файл";
            return;
        }

        if (IsProcessing)
            return;

        IsProcessing = true;
        CanExport = false;

        try
        {
            var managers = ManagersText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var files = LoadedFiles.ToList();

            var stages = new List<(double UpToPercent, string Message)>
            {
                (20.0, "[PARSE] Анализ структуры и разбор файлов Webbee AI..."),
                (55.0, "[CLEANSE] Оптимизация контактов и дедупликация базы..."),
                (85.0, "[RESOLVE] Географическая верификация и очистка адресов..."),
                (100.0, "[COMPILE] Формирование датасета и распределение операторов...")
            };

            await _main.RunWithPremiumLoadingAsync("Обработка лидов...", stages, async () =>
            {
                var result = _processor.ProcessFiles(files, managers, _config.Processing);

                _lastResult = result;

                foreach (var file in files)
                {
                    _db.SaveProcessingHistory(
                        Path.GetFileName(file),
                        result.Leads.Count / Math.Max(1, files.Count),
                        result.DuplicatesRemoved / Math.Max(1, files.Count),
                        result.ProcessingTimeMs / Math.Max(1, files.Count));
                }

                _config.Managers = managers;

                await UiDispatcher.RunOnUiThreadAsync(() =>
                {
                    StatusMessage = $"Готово: {result.Leads.Count} лидов · {result.ProcessingTimeMs} мс · " +
                                    $"удалено дублей: {result.DuplicatesRemoved} · без адреса: {result.RowsRemovedNoLocation}";
                    CanExport = result.Leads.Count > 0;
                });

                try
                {
                    _main.ConfigService.Save(_config);
                }
                catch
                {
                    // ignore save errors after processing
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка обработки: {ex.Message}";
            CanExport = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ExportBitrix()
    {
        if (_lastResult is null || _lastResult.Leads.Count == 0)
            return;

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV для Битрикс (*.csv)|*.csv",
                FileName = $"leads_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            var rows = BitrixMapper.MapToBitrix(_lastResult.Leads, _config.Bitrix);
            BitrixMapper.ExportToCsv(dialog.FileName, rows);
            StatusMessage = $"Экспортировано {rows.Count} лидов → {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка экспорта: {ex.Message}";
        }
    }
}
