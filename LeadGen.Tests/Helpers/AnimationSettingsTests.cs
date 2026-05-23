using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LeadGen.Helpers;
using LeadGen.Models;

namespace LeadGen.Tests.Helpers;

public class AnimationSettingsTests
{
  [Fact]
  public void Config_RoundTrips_AnimationsEnabled()
  {
    var config = new AppConfig
    {
      Ui = new UiSettings { AnimationsEnabled = false }
    };

    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
      PropertyNameCaseInsensitive = true
    });

    var loaded = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
      PropertyNameCaseInsensitive = true
    });

    Assert.NotNull(loaded);
    Assert.False(loaded!.Ui.AnimationsEnabled);
  }

  [Fact]
  public void Reveal_WhenDisabled_SetsVisibleInstantly()
  {
    double opacity = 0;
    Exception? error = null;
    var thread = new Thread(() =>
    {
      try
      {
        AnimationSettings.Initialize(false);
        var element = new Border { Width = 100, Height = 50, Opacity = 0 };
        AnimationHelper.Reveal(element, SlideDirection.Up);
        opacity = element.Opacity;
      }
      catch (Exception ex)
      {
        error = ex;
      }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (error is not null)
      throw error;
    Assert.Equal(1.0, opacity);
  }

  [Fact]
  public void CollectEntranceTargets_SortsByStaggerIndex()
  {
    Exception? error = null;
    var thread = new Thread(() =>
    {
      try
      {
        AnimationSettings.Initialize(true);

        var root = new StackPanel();
        var second = new Border();
        var first = new TextBlock();

        AnimationBehavior.SetStaggerIndex(second, 2);
        AnimationBehavior.SetStaggerIndex(first, 0);

        root.Children.Add(second);
        root.Children.Add(first);

        var targets = AnimationBehavior.CollectEntranceTargets(root, includeStaggerChildren: false);

        Assert.Equal(2, targets.Count);
        Assert.Equal(0, targets[0].Index);
        Assert.Equal(2, targets[1].Index);
      }
      catch (Exception ex)
      {
        error = ex;
      }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (error is not null)
      throw error;
  }
}
