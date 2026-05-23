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

    if (ActivityPlotView.Model is null && vm.ActivityChart is not null)
      ActivityPlotView.Model = vm.ActivityChart;
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    // Снимаем модель при уходе с вкладки — иначе OxyPlot не даст привязать её снова.
    ActivityPlotView.Model = null;
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
    }
  }
}
