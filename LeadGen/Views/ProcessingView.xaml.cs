using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LeadGen.Helpers;
using LeadGen.ViewModels;

namespace LeadGen.Views;

public partial class ProcessingView : UserControl
{
  private ProcessingViewModel? _vm;

  public ProcessingView()
  {
    InitializeComponent();
    DataContextChanged += OnDataContextChanged;
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    // entrance handled by PageTransitionHost
  }

  private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (_vm?.LoadedFiles is not null)
      _vm.LoadedFiles.CollectionChanged -= OnFilesCollectionChanged;

    _vm = e.NewValue as ProcessingViewModel;
    if (_vm?.LoadedFiles is not null)
      _vm.LoadedFiles.CollectionChanged += OnFilesCollectionChanged;
  }

  private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
  {
    if (e.Action != NotifyCollectionChangedAction.Add)
      return;

    Dispatcher.BeginInvoke(() =>
    {
      var index = _vm!.LoadedFiles.Count - 1;
      if (index < 0)
        return;

      var container = FilesList.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
      if (container is null)
      {
        FilesList.LayoutUpdated += OnFilesLayoutUpdated;
        return;
      }

      AnimateFileRow(container);
    });
  }

  private void OnFilesLayoutUpdated(object? sender, EventArgs e)
  {
    if (_vm is null)
      return;

    var index = _vm.LoadedFiles.Count - 1;
    if (index < 0)
      return;

    if (FilesList.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
      return;

    FilesList.LayoutUpdated -= OnFilesLayoutUpdated;
    AnimateFileRow(container);
  }

  private static void AnimateFileRow(FrameworkElement container)
  {
    var row = FindVisualChild<Border>(container);
    if (row is not null)
      AnimationHelper.RevealFromEdge(row, EdgeSide.Right, pageRoot: null, 0, AnimationTimings.FileRowMs);
  }

  private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
  {
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
      var child = VisualTreeHelper.GetChild(parent, i);
      if (child is T match)
        return match;
      var found = FindVisualChild<T>(child);
      if (found is not null)
        return found;
    }
    return null;
  }

  private void OnDragOver(object sender, DragEventArgs e)
  {
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
      e.Effects = DragDropEffects.Copy;
    else
      e.Effects = DragDropEffects.None;
    e.Handled = true;
  }

  private void OnDrop(object sender, DragEventArgs e)
  {
    if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
      return;

    var valid = files.Where(f =>
    {
      var ext = Path.GetExtension(f).ToLowerInvariant();
      return ext is ".json" or ".tsv" or ".csv";
    });

    if (DataContext is ProcessingViewModel vm)
      vm.AddFilePaths(valid);
  }

  private void OnDropZoneClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    if (DataContext is ProcessingViewModel vm)
      vm.AddFilesCommand.Execute(null);
  }
}
