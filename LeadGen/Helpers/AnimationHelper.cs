using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using LeadGen.Services;

namespace LeadGen.Helpers;

public enum SlideDirection
{
  Up,
  Down,
  Left,
  Right
}

/// <summary>
/// С какой границы окна приезжает блок.
/// </summary>
public enum EdgeSide
{
  Top,
  Bottom,
  Left,
  Right
}

/// <summary>
/// Вспомогательные анимации для плавных переходов.
/// </summary>
public static class AnimationHelper
{
  public static void SetVisibleInstant(FrameworkElement element)
  {
    element.BeginAnimation(UIElement.OpacityProperty, null);
    element.Opacity = 1;
    element.RenderTransform = Transform.Identity;
    element.Effect = null;
  }

  public static void FadeIn(FrameworkElement element, double durationMs = 350)
  {
    if (!AnimationSettings.IsEnabled)
    {
      SetVisibleInstant(element);
      return;
    }

    element.Opacity = 0;
    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    element.BeginAnimation(UIElement.OpacityProperty, anim);
  }

  public static void FadeOut(FrameworkElement element, double durationMs = AnimationTimings.ExitMs, Action? onCompleted = null)
  {
    if (!AnimationSettings.IsEnabled)
    {
      element.Opacity = 0;
      onCompleted?.Invoke();
      return;
    }

    var anim = new DoubleAnimation(element.Opacity, 0, TimeSpan.FromMilliseconds(durationMs))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
    };
    if (onCompleted is not null)
      anim.Completed += (_, _) => onCompleted();
    element.BeginAnimation(UIElement.OpacityProperty, anim);
  }

  public static void SlideIn(FrameworkElement element, double fromY = 24, double durationMs = 400) =>
      Reveal(element, SlideDirection.Up, 0, durationMs);

  /// <summary>
  /// Появление со сдвигом (инвертировано: SlideUp = с верхней границы окна).
  /// </summary>
  public static void Reveal(
      FrameworkElement element,
      SlideDirection direction,
      double delayMs = 0,
      double durationMs = AnimationTimings.EntranceMs,
      bool useScale = false)
  {
    _ = useScale;
    RevealFromEdge(element, MapSlideDirectionToEdge(direction), pageRoot: null, delayMs, durationMs);
  }

  public static void BlurReveal(FrameworkElement element, double delayMs = 0, double durationMs = AnimationTimings.EntranceMs) =>
      RevealFromEdge(element, fromEdge: null, pageRoot: null, delayMs, durationMs, useBlur: true);

  /// <summary>
  /// Появление с ближайшей границы окна приложения.
  /// </summary>
  public static void RevealFromEdge(
      FrameworkElement element,
      EdgeSide? fromEdge,
      FrameworkElement? pageRoot,
      double delayMs = 0,
      double durationMs = AnimationTimings.EntranceMs,
      bool useBlur = false)
  {
    if (!AnimationSettings.IsEnabled)
    {
      SetVisibleInstant(element);
      return;
    }

    var bounds = ResolveAppBounds(pageRoot ?? element);
    bounds.UpdateLayout();
    element.UpdateLayout();

    var position = element.TransformToAncestor(bounds).Transform(new Point(0, 0));
    var elementW = GetActualDimension(element.ActualWidth, element.RenderSize.Width);
    var elementH = GetActualDimension(element.ActualHeight, element.RenderSize.Height);
    var boundsW = GetActualDimension(bounds.ActualWidth, bounds.RenderSize.Width);
    var boundsH = GetActualDimension(bounds.ActualHeight, bounds.RenderSize.Height);

    var edge = fromEdge ?? PickNearestEdge(position, elementW, elementH, boundsW, boundsH);
    var (offsetX, offsetY) = ComputeEdgeOffset(edge, position, elementW, elementH, boundsW, boundsH);

    RevealWithOffset(element, offsetX, offsetY, delayMs, durationMs, useBlur);
  }

  private static EdgeSide MapSlideDirectionToEdge(SlideDirection direction) =>
      direction switch
      {
        SlideDirection.Up => EdgeSide.Top,
        SlideDirection.Down => EdgeSide.Bottom,
        SlideDirection.Left => EdgeSide.Left,
        SlideDirection.Right => EdgeSide.Right,
        _ => EdgeSide.Top
      };

  private static FrameworkElement ResolveAppBounds(FrameworkElement element) =>
      Window.GetWindow(element) as FrameworkElement ?? element;

  private static double GetActualDimension(double actual, double render) =>
      actual > 0 ? actual : (render > 0 ? render : 1);

  private static EdgeSide PickNearestEdge(Point position, double elementW, double elementH, double boundsW, double boundsH)
  {
    var centerX = position.X + elementW / 2;
    var centerY = position.Y + elementH / 2;
    var toLeft = centerX;
    var toRight = boundsW - centerX;
    var toTop = centerY;
    var toBottom = boundsH - centerY;
    var min = Math.Min(Math.Min(toLeft, toRight), Math.Min(toTop, toBottom));

    if (min == toTop)
      return EdgeSide.Top;
    if (min == toBottom)
      return EdgeSide.Bottom;
    return min == toLeft ? EdgeSide.Left : EdgeSide.Right;
  }

  private static (double X, double Y) ComputeEdgeOffset(
      EdgeSide edge,
      Point position,
      double elementW,
      double elementH,
      double boundsW,
      double boundsH)
  {
    const double minTravel = 120;
    const double margin = 40;

    return edge switch
    {
      EdgeSide.Top => (0, -Math.Max(position.Y + margin, minTravel)),
      EdgeSide.Bottom => (0, Math.Max(boundsH - position.Y + margin, minTravel)),
      EdgeSide.Left => (-Math.Max(position.X + margin, minTravel), 0),
      EdgeSide.Right => (Math.Max(boundsW - position.X - elementW + margin, minTravel), 0),
      _ => (0, -minTravel)
    };
  }

  private static void RevealWithOffset(
      FrameworkElement element,
      double offsetX,
      double offsetY,
      double delayMs,
      double durationMs,
      bool useBlur)
  {
    var translate = new TranslateTransform(offsetX, offsetY);
    element.RenderTransform = translate;
    element.Opacity = 0;

    if (useBlur)
      element.Effect = new BlurEffect { Radius = 10 };

    var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
    var begin = TimeSpan.FromMilliseconds(delayMs);
    var duration = TimeSpan.FromMilliseconds(durationMs);

    var fade = new DoubleAnimation(0, 1, duration)
    {
      BeginTime = begin,
      EasingFunction = easing
    };
    element.BeginAnimation(UIElement.OpacityProperty, fade);

    var slideX = new DoubleAnimation(offsetX, 0, duration)
    {
      BeginTime = begin,
      EasingFunction = easing
    };
    translate.BeginAnimation(TranslateTransform.XProperty, slideX);

    var slideY = new DoubleAnimation(offsetY, 0, duration)
    {
      BeginTime = begin,
      EasingFunction = easing
    };
    translate.BeginAnimation(TranslateTransform.YProperty, slideY);

    if (useBlur && element.Effect is BlurEffect blur)
    {
      var blurAnim = new DoubleAnimation(10, 0, duration)
      {
        BeginTime = begin,
        EasingFunction = easing
      };
      blurAnim.Completed += (_, _) => element.Effect = null;
      blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);
    }
  }

  public static void ScalePulse(FrameworkElement element, double durationMs = AnimationTimings.ScalePulseMs)
  {
    if (!AnimationSettings.IsEnabled)
      return;

    var scale = new ScaleTransform(1, 1);
    element.RenderTransformOrigin = new Point(0.5, 0.5);
    element.RenderTransform = scale;

    var duration = TimeSpan.FromMilliseconds(durationMs / 2);
    var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

    var up = new DoubleAnimation(1, 1.05, duration) { EasingFunction = easing };
    up.Completed += (_, _) =>
    {
      var down = new DoubleAnimation(1.05, 1, duration) { EasingFunction = easing };
      down.Completed += (_, _) => element.RenderTransform = Transform.Identity;
      scale.BeginAnimation(ScaleTransform.ScaleXProperty, down);
      scale.BeginAnimation(ScaleTransform.ScaleYProperty, down);
    };
    scale.BeginAnimation(ScaleTransform.ScaleXProperty, up);
    scale.BeginAnimation(ScaleTransform.ScaleYProperty, up);
  }

  public static void SlideNavIndicator(FrameworkElement indicator, double targetX, double width, Action? onCompleted = null)
  {
    if (double.IsNaN(indicator.Width) || indicator.Width <= 0)
    {
      indicator.Width = indicator.ActualWidth > 0 ? indicator.ActualWidth : width;
    }

    double currentX = 0;
    if (indicator.RenderTransform is TranslateTransform tt)
    {
      currentX = tt.X;
    }
    else
    {
      tt = new TranslateTransform();
      indicator.RenderTransform = tt;
    }

    if (!AnimationSettings.IsEnabled)
    {
      tt.X = targetX;
      indicator.Width = width;
      onCompleted?.Invoke();
      return;
    }

    double currentW = indicator.Width;
    double targetW = width;
    double distance = targetX - currentX;

    if (Math.Abs(distance) < 2)
    {
      var animX = new DoubleAnimation(currentX, targetX, TimeSpan.FromMilliseconds(AnimationTimings.NavIndicatorMs))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      };
      var animW = new DoubleAnimation(currentW, targetW, TimeSpan.FromMilliseconds(AnimationTimings.NavIndicatorMs))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      };
      if (onCompleted is not null)
        animX.Completed += (_, _) => onCompleted();

      tt.BeginAnimation(TranslateTransform.XProperty, animX);
      indicator.BeginAnimation(FrameworkElement.WidthProperty, animW);
      return;
    }

    // Рассчитываем параметры эластичного движения (Rubber-Band Effect)
    double totalDurationMs = AnimationTimings.NavIndicatorMs;
    double stretchDurationMs = totalDurationMs * 0.45;
    double settleDurationMs = totalDurationMs * 0.55;
    double extraWidth = Math.Min(Math.Abs(distance) * 0.35, 90);

    var animXFrames = new DoubleAnimationUsingKeyFrames();
    var animWFrames = new DoubleAnimationUsingKeyFrames();

    if (onCompleted is not null)
      animXFrames.Completed += (_, _) => onCompleted();

    var keyTimeStart = KeyTime.FromTimeSpan(TimeSpan.Zero);
    var keyTimeStretch = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(stretchDurationMs));
    var keyTimeEnd = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(totalDurationMs));

    var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
    var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };
    var easeInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
    var springSettle = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 };

    if (distance > 0)
    {
      // Движение вправо: правый край летит вперед, левый отстает
      double intermediateX = currentX + (distance * 0.25);
      double peakWidth = targetW + extraWidth;

      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(currentX, keyTimeStart));
      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(intermediateX, keyTimeStretch) { EasingFunction = easeIn });
      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(targetX, keyTimeEnd) { EasingFunction = easeOut });

      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(currentW, keyTimeStart));
      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(peakWidth, keyTimeStretch) { EasingFunction = easeOut });
      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(targetW, keyTimeEnd) { EasingFunction = springSettle });
    }
    else
    {
      // Движение влево: левый край улетает влево быстро, правый край отстает
      double intermediateX = currentX + (distance * 0.75);
      double peakWidth = targetW + extraWidth;

      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(currentX, keyTimeStart));
      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(intermediateX, keyTimeStretch) { EasingFunction = easeOut });
      animXFrames.KeyFrames.Add(new EasingDoubleKeyFrame(targetX, keyTimeEnd) { EasingFunction = easeInOut });

      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(currentW, keyTimeStart));
      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(peakWidth, keyTimeStretch) { EasingFunction = easeOut });
      animWFrames.KeyFrames.Add(new EasingDoubleKeyFrame(targetW, keyTimeEnd) { EasingFunction = springSettle });
    }

    tt.BeginAnimation(TranslateTransform.XProperty, animXFrames);
    indicator.BeginAnimation(FrameworkElement.WidthProperty, animWFrames);
  }

  public static void LoadingOverlayEntrance(FrameworkElement backdrop, FrameworkElement card)
  {
    if (!AnimationSettings.IsEnabled)
    {
      backdrop.Opacity = 1;
      SetVisibleInstant(card);
      return;
    }

    backdrop.Opacity = 0;
    var backdropFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    backdrop.BeginAnimation(UIElement.OpacityProperty, backdropFade);

    var scale = new ScaleTransform(0.92, 0.92);
    card.RenderTransformOrigin = new Point(0.5, 0.5);
    card.RenderTransform = scale;
    card.Opacity = 0;

    var duration = TimeSpan.FromMilliseconds(AnimationTimings.EntranceMs);
    var easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.2 };

    var cardFade = new DoubleAnimation(0, 1, duration) { EasingFunction = easing };
    card.BeginAnimation(UIElement.OpacityProperty, cardFade);

    var scaleAnim = new DoubleAnimation(0.92, 1, duration) { EasingFunction = easing };
    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
  }

  /// <summary>
  /// Анимация счётчика.
  /// </summary>
  public static void AnimateCounter(
      Action<int> setter,
      int from,
      int to,
      int durationMs = 800)
  {
    if (from == to)
    {
      setter(to);
      return;
    }

    if (!AnimationSettings.IsEnabled)
    {
      setter(to);
      return;
    }

    var dispatcher = UiDispatcher.Current;
    if (dispatcher is null)
    {
      setter(to);
      return;
    }

    var start = DateTime.Now;
    var timer = new DispatcherTimer(DispatcherPriority.Render, dispatcher)
    {
      Interval = TimeSpan.FromMilliseconds(1000.0 / 120)
    };

    timer.Tick += (_, _) =>
    {
      var elapsed = (DateTime.Now - start).TotalMilliseconds;
      var progress = Math.Min(1.0, elapsed / durationMs);
      var eased = 1 - Math.Pow(1 - progress, 3);
      setter((int)(from + (to - from) * eased));

      if (progress >= 1)
        timer.Stop();
    };
    timer.Start();
  }
}
