using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LeadGen.Helpers;
using LeadGen.ViewModels;

namespace LeadGen.Views;

public partial class DashboardView : UserControl
{
  private DashboardViewModel? _vm;

  public DashboardView()
  {
    InitializeComponent();
    DataContextChanged += OnDataContextChanged;
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    if (DataContext is not DashboardViewModel vm)
      return;

    vm.Initialize();

    ApplyChartModel(vm);
  }

  private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (_vm is not null)
      _vm.PropertyChanged -= OnViewModelPropertyChanged;

    _vm = e.NewValue as DashboardViewModel;
    if (_vm is not null)
      _vm.PropertyChanged += OnViewModelPropertyChanged;
  }

  private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    switch (e.PropertyName)
    {
      case nameof(DashboardViewModel.FilesProcessed):
        AnimationHelper.ScalePulse(StatFilesCard);
        break;
      case nameof(DashboardViewModel.RowsProcessed):
        AnimationHelper.ScalePulse(StatRowsCard);
        break;
      case nameof(DashboardViewModel.LinksGenerated):
        AnimationHelper.ScalePulse(StatLinksCard);
        break;
      case nameof(DashboardViewModel.LinkSessions):
        AnimationHelper.ScalePulse(StatSessionsCard);
        break;
      case nameof(DashboardViewModel.ActivityChart):
        if (_vm is not null)
          ApplyChartModel(_vm);
        break;
    }
  }

  private void ApplyChartModel(DashboardViewModel vm)
  {
    if (vm.ActivityChart is null)
      return;

    ActivityPlotView.Model = vm.ActivityChart;
    ActivityPlotView.InvalidatePlot(true);
  }
}
