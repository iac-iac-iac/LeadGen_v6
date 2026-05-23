using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using LeadGen.Models;

namespace LeadGen.Helpers;

/// <summary>
/// Построение линейного графика активности для дашборда (OxyPlot).
/// </summary>
public static class DashboardChartBuilder
{
    private static readonly OxyColor BgTransparent = OxyColor.FromArgb(0, 0, 0, 0);
    private static readonly OxyColor TextColor = OxyColor.FromRgb(142, 151, 176);
    private static readonly OxyColor GridColor = OxyColor.FromArgb(25, 255, 255, 255);
    private static readonly OxyColor LineRows = OxyColor.FromRgb(46, 232, 214);
    private static readonly OxyColor LineLinks = OxyColor.FromRgb(255, 126, 179);
    private static readonly OxyColor LineFiles = OxyColor.FromRgb(139, 108, 255);

    public static PlotModel Build(IReadOnlyList<DailyActivityPoint> points)
    {
        var model = new PlotModel
        {
            Background = BgTransparent,
            PlotAreaBackground = BgTransparent,
            PlotAreaBorderColor = OxyColor.FromArgb(40, 139, 108, 255),
            PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 1),
            TextColor = TextColor,
            TitleColor = OxyColor.FromRgb(244, 246, 255),
            SubtitleColor = TextColor,
            IsLegendVisible = true,
            Padding = new OxyThickness(0, 8, 8, 0)
        };

        model.Legends.Add(new Legend
        {
            LegendPlacement = LegendPlacement.Inside,
            LegendPosition = LegendPosition.TopRight,
            LegendBackground = OxyColor.FromArgb(40, 20, 24, 36),
            LegendBorder = OxyColor.FromArgb(60, 139, 108, 255),
            LegendBorderThickness = 1,
            LegendPadding = 8,
            TextColor = OxyColor.FromRgb(244, 246, 255)
        });

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Key = "DateAxis",
            TextColor = TextColor,
            TicklineColor = GridColor,
            AxislineColor = OxyColors.Undefined,
            MajorGridlineColor = GridColor,
            MinorGridlineColor = OxyColors.Undefined,
            Angle = -35,
            GapWidth = 0.5,
            FontSize = 10
        };

        foreach (var p in points)
            categoryAxis.Labels.Add(p.Label);

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = TextColor,
            TicklineColor = GridColor,
            AxislineColor = OxyColors.Undefined,
            MajorGridlineColor = GridColor,
            MinorGridlineColor = OxyColors.Undefined,
            MinimumPadding = 0.15,
            AbsoluteMinimum = 0,
            FontSize = 10
        };

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);

        model.Series.Add(CreateArea("Строки", LineRows, points, p => p.RowsProcessed));
        model.Series.Add(CreateArea("Ссылки", LineLinks, points, p => p.LinksGenerated));
        model.Series.Add(CreateLine("Файлы", LineFiles, points, p => (double)p.FilesProcessed));

        model.ResetAllAxes();
        model.InvalidatePlot(true);

        return model;
    }

    private static AreaSeries CreateArea(
        string title,
        OxyColor color,
        IReadOnlyList<DailyActivityPoint> points,
        Func<DailyActivityPoint, double> selector)
    {
        var series = new AreaSeries
        {
            Title = title,
            Color = OxyColor.FromAColor(180, color),
            Fill = OxyColor.FromAColor(35, color),
            StrokeThickness = 2.5,
            MarkerType = points.Count <= 1 ? MarkerType.None : MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = color,
            MarkerStroke = color
        };

        for (var i = 0; i < points.Count; i++)
            series.Points.Add(new DataPoint(i, selector(points[i])));

        return series;
    }

    private static LineSeries CreateLine(
        string title,
        OxyColor color,
        IReadOnlyList<DailyActivityPoint> points,
        Func<DailyActivityPoint, double> selector)
    {
        var series = new LineSeries
        {
            Title = title,
            Color = color,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            MarkerType = points.Count <= 1 ? MarkerType.None : MarkerType.Diamond,
            MarkerSize = 4,
            MarkerFill = color
        };

        if (points.Count >= 4)
            series.InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline;

        for (var i = 0; i < points.Count; i++)
            series.Points.Add(new DataPoint(i, selector(points[i])));

        return series;
    }
}
