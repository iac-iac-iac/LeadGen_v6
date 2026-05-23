using System.Windows;
using System.Windows.Controls;
using LeadGen.Helpers;

namespace LeadGen.Views;

public partial class LinkGeneratorView : UserControl
{
  private static bool _citiesEntrancePlayed;

  public LinkGeneratorView()
  {
    InitializeComponent();
  }

  private void OnLoaded(object sender, RoutedEventArgs e) =>
      ScheduleCitiesStagger();

  internal void PlayCitiesStaggerIfNeeded() =>
      ScheduleCitiesStagger();

  private void ScheduleCitiesStagger()
  {
    if (_citiesEntrancePlayed)
      return;

    Dispatcher.BeginInvoke(TryPlayCitiesStagger);
  }

  private void TryPlayCitiesStagger()
  {
    if (_citiesEntrancePlayed)
      return;

    if (PlayCitiesStagger())
      _citiesEntrancePlayed = true;
  }

  private bool PlayCitiesStagger()
  {
    CitiesList.UpdateLayout();
    var animated = 0;

    for (var i = 0; i < CitiesList.Items.Count; i++)
    {
      if (CitiesList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
        continue;

      animated++;
      var delay = 3 * AnimationTimings.StaggerStepMs + i * AnimationTimings.StaggerStepMs;
      AnimationHelper.RevealFromEdge(container, EdgeSide.Left, this, delay, AnimationTimings.EntranceMs);
    }

    if (animated > 0)
      return true;

    if (CitiesList.Items.Count > 0)
    {
      CitiesList.LayoutUpdated -= OnCitiesLayoutUpdated;
      CitiesList.LayoutUpdated += OnCitiesLayoutUpdated;
      return false;
    }

    return true;
  }

  private void OnCitiesLayoutUpdated(object? sender, EventArgs e)
  {
    if (CitiesList.Items.Count == 0)
      return;

    if (CitiesList.ItemContainerGenerator.ContainerFromIndex(0) is null)
      return;

    CitiesList.LayoutUpdated -= OnCitiesLayoutUpdated;
    if (PlayCitiesStagger())
      _citiesEntrancePlayed = true;
  }
}
