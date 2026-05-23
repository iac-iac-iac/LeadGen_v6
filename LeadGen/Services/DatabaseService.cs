using LeadGen.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace LeadGen.Services;

/// <summary>
/// SQLite: история обработок, генерации ссылок, статистика дашборда.
/// </summary>
public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    private void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS processing_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                filename TEXT NOT NULL,
                process_date TEXT NOT NULL,
                rows_processed INTEGER DEFAULT 0,
                duplicates_removed INTEGER DEFAULT 0,
                processing_time_ms INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS link_generation_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                segment TEXT NOT NULL,
                links_count INTEGER DEFAULT 0,
                cities_count INTEGER DEFAULT 0,
                generated_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_processing_date ON processing_history(process_date);
            CREATE INDEX IF NOT EXISTS idx_link_gen_date ON link_generation_history(generated_at);
            """;
        cmd.ExecuteNonQuery();
    }

    public void SaveProcessingHistory(string filename, int rows, int duplicates, long timeMs)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO processing_history (filename, process_date, rows_processed, duplicates_removed, processing_time_ms)
            VALUES ($fn, $date, $rows, $dup, $time)
            """;
        cmd.Parameters.AddWithValue("$fn", filename);
        cmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$rows", rows);
        cmd.Parameters.AddWithValue("$dup", duplicates);
        cmd.Parameters.AddWithValue("$time", timeMs);
        cmd.ExecuteNonQuery();
    }

    public void SaveLinkGeneration(string segment, int linksCount, int citiesCount)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO link_generation_history (segment, links_count, cities_count, generated_at)
            VALUES ($seg, $links, $cities, $date)
            """;
        cmd.Parameters.AddWithValue("$seg", segment);
        cmd.Parameters.AddWithValue("$links", linksCount);
        cmd.Parameters.AddWithValue("$cities", citiesCount);
        cmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public DashboardStats GetDashboardStats(DateTime from, DateTime to)
    {
        var stats = new DashboardStats();
        var fromStr = from.ToString("O");
        var toStr = to.ToString("O");

        using var conn = OpenConnection();

        // Файлы и строки обработки
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT COUNT(*), COALESCE(SUM(rows_processed), 0), COALESCE(SUM(duplicates_removed), 0)
                FROM processing_history
                WHERE process_date >= $from AND process_date <= $to
                """;
            cmd.Parameters.AddWithValue("$from", fromStr);
            cmd.Parameters.AddWithValue("$to", toStr);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                stats.FilesProcessed = reader.GetInt32(0);
                stats.RowsProcessed = reader.GetInt32(1);
                stats.DuplicatesRemoved = reader.GetInt32(2);
            }
        }

        // Ссылки
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT COUNT(*), COALESCE(SUM(links_count), 0)
                FROM link_generation_history
                WHERE generated_at >= $from AND generated_at <= $to
                """;
            cmd.Parameters.AddWithValue("$from", fromStr);
            cmd.Parameters.AddWithValue("$to", toStr);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                stats.LinkSessions = reader.GetInt32(0);
                stats.LinksGenerated = reader.GetInt32(1);
            }
        }

        stats.RecentActivity = GetRecentActivity(conn, 15);
        stats.DailyActivity = GetDailyActivity(conn, from, to);
        return stats;
    }

    /// <summary>
    /// Агрегирует активность по дням для графиков дашборда.
    /// </summary>
    public List<DailyActivityPoint> GetDailyActivity(DateTime from, DateTime to)
    {
        using var conn = OpenConnection();
        return GetDailyActivity(conn, from, to);
    }

    private static List<DailyActivityPoint> GetDailyActivity(SqliteConnection conn, DateTime from, DateTime to)
    {
        var fromStr = from.ToString("O");
        var toStr = to.ToString("O");
        var map = new Dictionary<DateTime, DailyActivityPoint>();

        void EnsureDay(DateTime day)
        {
            var key = day.Date;
            if (!map.ContainsKey(key))
                map[key] = new DailyActivityPoint { Date = key };
        }

        // Заполняем все дни периода (макс. 365 дней)
        var start = from.Date;
        var end = to.Date;
        if (start == DateTime.MinValue.Date || start > end)
            start = end.AddDays(-30);

        var dayCount = (end - start).Days + 1;
        if (dayCount > 365)
            start = end.AddDays(-364);

        for (var d = start; d <= end; d = d.AddDays(1))
            EnsureDay(d);

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT date(process_date) AS d,
                       COUNT(*) AS files,
                       COALESCE(SUM(rows_processed), 0) AS rows
                FROM processing_history
                WHERE process_date >= $from AND process_date <= $to
                GROUP BY date(process_date)
                """;
            cmd.Parameters.AddWithValue("$from", fromStr);
            cmd.Parameters.AddWithValue("$to", toStr);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!TryParseDate(reader.GetString(0), out var day))
                    continue;
                EnsureDay(day);
                map[day.Date].FilesProcessed = reader.GetInt32(1);
                map[day.Date].RowsProcessed = reader.GetInt32(2);
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT date(generated_at) AS d,
                       COALESCE(SUM(links_count), 0) AS links
                FROM link_generation_history
                WHERE generated_at >= $from AND generated_at <= $to
                GROUP BY date(generated_at)
                """;
            cmd.Parameters.AddWithValue("$from", fromStr);
            cmd.Parameters.AddWithValue("$to", toStr);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!TryParseDate(reader.GetString(0), out var day))
                    continue;
                EnsureDay(day);
                map[day.Date].LinksGenerated = reader.GetInt32(1);
            }
        }

        return map.Values.OrderBy(p => p.Date).ToList();
    }

    private static List<ActivityItem> GetRecentActivity(SqliteConnection conn, int limit)
    {
        var items = new List<ActivityItem>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT 'processing' AS type, filename, process_date, rows_processed FROM processing_history
            UNION ALL
            SELECT 'links' AS type, segment, generated_at, links_count FROM link_generation_history
            ORDER BY process_date DESC, generated_at DESC
            LIMIT {limit}
            """;

        // Упрощённый запрос — два отдельных SELECT и merge
        cmd.CommandText = """
            SELECT 'processing', filename, process_date, rows_processed
            FROM processing_history ORDER BY process_date DESC LIMIT 10
            """;

        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                items.Add(new ActivityItem
                {
                    Type = "processing",
                    Description = reader.GetString(1),
                    Timestamp = TryParseDate(reader.GetString(2), out var ts) ? ts : DateTime.Now,
                    Count = reader.GetInt32(3)
                });
            }
        }

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = """
            SELECT 'links', segment, generated_at, links_count
            FROM link_generation_history ORDER BY generated_at DESC LIMIT 10
            """;
        using var reader2 = cmd2.ExecuteReader();
        while (reader2.Read())
        {
            items.Add(new ActivityItem
            {
                Type = "links",
                Description = reader2.GetString(1),
                Timestamp = TryParseDate(reader2.GetString(2), out var ts) ? ts : DateTime.Now,
                Count = reader2.GetInt32(3)
            });
        }

        return items.OrderByDescending(i => i.Timestamp).Take(limit).ToList();
    }

    private static bool TryParseDate(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return DateTime.TryParse(value, out result)
               || DateTime.TryParseExact(value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out result);
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }
}
