using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LeadGen.Helpers;

public enum EntranceKind
{
  Default,
  BlurReveal,
  SlideUp,
  SlideDown,
  SlideLeft,
  SlideRight
}

/// <summary>
/// XAML-алиас: helpers:Animation.* (см. AnimationBehavior).
/// </summary>
public static class Animation
{
  public static readonly DependencyProperty StaggerIndexProperty = AnimationBehavior.StaggerIndexProperty;
  public static readonly DependencyProperty EntranceProperty = AnimationBehavior.EntranceProperty;
  public static readonly DependencyProperty StaggerChildrenProperty = AnimationBehavior.StaggerChildrenProperty;
  public static readonly DependencyProperty InteractiveGlowProperty = AnimationBehavior.InteractiveGlowProperty;
  public static readonly DependencyProperty InteractiveGlowColorProperty = AnimationBehavior.InteractiveGlowColorProperty;
  public static readonly DependencyProperty InteractiveGlowSecondaryColorProperty = AnimationBehavior.InteractiveGlowSecondaryColorProperty;

  public static int GetStaggerIndex(DependencyObject obj) => AnimationBehavior.GetStaggerIndex(obj);
  public static void SetStaggerIndex(DependencyObject obj, int value) => AnimationBehavior.SetStaggerIndex(obj, value);
  public static EntranceKind GetEntrance(DependencyObject obj) => AnimationBehavior.GetEntrance(obj);
  public static void SetEntrance(DependencyObject obj, EntranceKind value) => AnimationBehavior.SetEntrance(obj, value);
  public static bool GetStaggerChildren(DependencyObject obj) => AnimationBehavior.GetStaggerChildren(obj);
  public static void SetStaggerChildren(DependencyObject obj, bool value) => AnimationBehavior.SetStaggerChildren(obj, value);
  public static bool GetInteractiveGlow(DependencyObject obj) => AnimationBehavior.GetInteractiveGlow(obj);
  public static void SetInteractiveGlow(DependencyObject obj, bool value) => AnimationBehavior.SetInteractiveGlow(obj, value);
  public static Color GetInteractiveGlowColor(DependencyObject obj) => AnimationBehavior.GetInteractiveGlowColor(obj);
  public static void SetInteractiveGlowColor(DependencyObject obj, Color value) => AnimationBehavior.SetInteractiveGlowColor(obj, value);
  public static Color GetInteractiveGlowSecondaryColor(DependencyObject obj) => AnimationBehavior.GetInteractiveGlowSecondaryColor(obj);
  public static void SetInteractiveGlowSecondaryColor(DependencyObject obj, Color value) => AnimationBehavior.SetInteractiveGlowSecondaryColor(obj, value);
}

/// <summary>
/// Attached properties для каскадного появления блоков (Style B).
/// </summary>
public static class AnimationBehavior
{
  public static readonly DependencyProperty StaggerIndexProperty =
      DependencyProperty.RegisterAttached(
          "StaggerIndex",
          typeof(int),
          typeof(AnimationBehavior),
          new PropertyMetadata(-1));

  public static readonly DependencyProperty EntranceProperty =
      DependencyProperty.RegisterAttached(
          "Entrance",
          typeof(EntranceKind),
          typeof(AnimationBehavior),
          new PropertyMetadata(EntranceKind.Default));

  public static readonly DependencyProperty StaggerChildrenProperty =
      DependencyProperty.RegisterAttached(
          "StaggerChildren",
          typeof(bool),
          typeof(AnimationBehavior),
          new PropertyMetadata(false));

  public static int GetStaggerIndex(DependencyObject obj) => (int)obj.GetValue(StaggerIndexProperty);
  public static void SetStaggerIndex(DependencyObject obj, int value) => obj.SetValue(StaggerIndexProperty, value);

  public static EntranceKind GetEntrance(DependencyObject obj) => (EntranceKind)obj.GetValue(EntranceProperty);
  public static void SetEntrance(DependencyObject obj, EntranceKind value) => obj.SetValue(EntranceProperty, value);

  public static bool GetStaggerChildren(DependencyObject obj) => (bool)obj.GetValue(StaggerChildrenProperty);
  public static void SetStaggerChildren(DependencyObject obj, bool value) => obj.SetValue(StaggerChildrenProperty, value);

  public static readonly DependencyProperty InteractiveGlowProperty =
      DependencyProperty.RegisterAttached(
          "InteractiveGlow",
          typeof(bool),
          typeof(AnimationBehavior),
          new PropertyMetadata(false, OnInteractiveGlowChanged));

  public static bool GetInteractiveGlow(DependencyObject obj) => (bool)obj.GetValue(InteractiveGlowProperty);
  public static void SetInteractiveGlow(DependencyObject obj, bool value) => obj.SetValue(InteractiveGlowProperty, value);

  public static readonly DependencyProperty InteractiveGlowColorProperty =
      DependencyProperty.RegisterAttached(
          "InteractiveGlowColor",
          typeof(Color),
          typeof(AnimationBehavior),
          new PropertyMetadata(Color.FromRgb(0x8B, 0x6C, 0xFF), OnGlowColorChanged));

  public static readonly DependencyProperty InteractiveGlowSecondaryColorProperty =
      DependencyProperty.RegisterAttached(
          "InteractiveGlowSecondaryColor",
          typeof(Color),
          typeof(AnimationBehavior),
          new PropertyMetadata(Color.FromRgb(0x2E, 0xE8, 0xD6), OnGlowColorChanged));

  public static Color GetInteractiveGlowColor(DependencyObject obj) => (Color)obj.GetValue(InteractiveGlowColorProperty);
  public static void SetInteractiveGlowColor(DependencyObject obj, Color value) => obj.SetValue(InteractiveGlowColorProperty, value);

  public static Color GetInteractiveGlowSecondaryColor(DependencyObject obj) => (Color)obj.GetValue(InteractiveGlowSecondaryColorProperty);
  public static void SetInteractiveGlowSecondaryColor(DependencyObject obj, Color value) => obj.SetValue(InteractiveGlowSecondaryColorProperty, value);

  private static void OnGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is Border border && GetInteractiveGlow(border) && border.IsLoaded)
      InitializeGlowBrush(border);
  }

  private static void OnInteractiveGlowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not Border border)
      return;

    if ((bool)e.NewValue)
    {
      border.MouseMove += Border_MouseMove;
      border.MouseLeave += Border_MouseLeave;
      border.Loaded += Border_Loaded;

      if (border.IsLoaded)
        InitializeGlowBrush(border);
    }
    else
    {
      border.MouseMove -= Border_MouseMove;
      border.MouseLeave -= Border_MouseLeave;
      border.Loaded -= Border_Loaded;
    }
  }

  private static void Border_Loaded(object sender, RoutedEventArgs e)
  {
    if (sender is Border border)
      InitializeGlowBrush(border);
  }

  private static void InitializeGlowBrush(Border border)
  {
    if (!AnimationSettings.IsEnabled)
      return;

    var primary = GetInteractiveGlowColor(border);
    var secondary = GetInteractiveGlowSecondaryColor(border);

    var glowBrush = new RadialGradientBrush
    {
      Center = new Point(0.5, 0.5),
      GradientOrigin = new Point(0.5, 0.5),
      RadiusX = 1.3,
      RadiusY = 1.3,
      MappingMode = BrushMappingMode.RelativeToBoundingBox
    };

    glowBrush.GradientStops.Add(new GradientStop(BlendAlpha(primary, 0xEE), 0.0));
    glowBrush.GradientStops.Add(new GradientStop(BlendAlpha(secondary, 0x66), 0.35));
    glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x28, 0x38, 0x2A, 0x30), 0.75));
    glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x18, 0x25, 0x2B, 0x3D), 1.0));

    border.BorderBrush = glowBrush;
  }

  private static Color BlendAlpha(Color color, byte alpha) =>
      Color.FromArgb(alpha, color.R, color.G, color.B);

  private static void Border_MouseMove(object sender, MouseEventArgs e)
  {
    if (sender is not Border border)
      return;

    if (!AnimationSettings.IsEnabled)
      return;

    if (border.BorderBrush is not RadialGradientBrush brush)
    {
      InitializeGlowBrush(border);
      brush = (RadialGradientBrush)border.BorderBrush;
    }

    if (brush is null)
      return;

    var pos = e.GetPosition(border);
    double w = border.ActualWidth;
    double h = border.ActualHeight;

    if (w <= 0 || h <= 0)
      return;

    var relativePos = new Point(pos.X / w, pos.Y / h);

    var animCenter = new PointAnimation(brush.Center, relativePos, TimeSpan.FromMilliseconds(150))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    var animOrigin = new PointAnimation(brush.GradientOrigin, relativePos, TimeSpan.FromMilliseconds(150))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };

    brush.BeginAnimation(RadialGradientBrush.CenterProperty, animCenter);
    brush.BeginAnimation(RadialGradientBrush.GradientOriginProperty, animOrigin);

    // 3D-наклон (Skew) для упругого параллакса
    if (border.RenderTransform is not TransformGroup group)
    {
      group = new TransformGroup();
      var scale = new ScaleTransform(1, 1);
      var skew = new SkewTransform(0, 0);
      group.Children.Add(scale);
      group.Children.Add(skew);
      border.RenderTransformOrigin = new Point(0.5, 0.5);
      border.RenderTransform = group;
    }

    if (group.Children.Count >= 2 &&
        group.Children[0] is ScaleTransform scaleTrans &&
        group.Children[1] is SkewTransform skewTrans)
    {
      double offsetX = relativePos.X - 0.5;
      double offsetY = relativePos.Y - 0.5;

      double targetSkewX = offsetY * 1.5;
      double targetSkewY = -offsetX * 1.5;

      var animScaleX = new DoubleAnimation(scaleTrans.ScaleX, 1.015, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animScaleY = new DoubleAnimation(scaleTrans.ScaleY, 1.015, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animSkewX = new DoubleAnimation(skewTrans.AngleX, targetSkewX, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animSkewY = new DoubleAnimation(skewTrans.AngleY, targetSkewY, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

      scaleTrans.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
      scaleTrans.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
      skewTrans.BeginAnimation(SkewTransform.AngleXProperty, animSkewX);
      skewTrans.BeginAnimation(SkewTransform.AngleYProperty, animSkewY);
    }
  }

  private static void Border_MouseLeave(object sender, MouseEventArgs e)
  {
    if (sender is not Border border)
      return;

    if (!AnimationSettings.IsEnabled)
      return;

    if (border.BorderBrush is RadialGradientBrush brush)
    {
      var defaultPos = new Point(0.5, 0.5);
      var animCenter = new PointAnimation(brush.Center, defaultPos, TimeSpan.FromMilliseconds(450))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      };
      var animOrigin = new PointAnimation(brush.GradientOrigin, defaultPos, TimeSpan.FromMilliseconds(450))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      };

      brush.BeginAnimation(RadialGradientBrush.CenterProperty, animCenter);
      brush.BeginAnimation(RadialGradientBrush.GradientOriginProperty, animOrigin);
    }

    if (border.RenderTransform is TransformGroup group &&
        group.Children.Count >= 2 &&
        group.Children[0] is ScaleTransform scaleTrans &&
        group.Children[1] is SkewTransform skewTrans)
    {
      var animScaleX = new DoubleAnimation(scaleTrans.ScaleX, 1.0, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animScaleY = new DoubleAnimation(scaleTrans.ScaleY, 1.0, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animSkewX = new DoubleAnimation(skewTrans.AngleX, 0, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
      var animSkewY = new DoubleAnimation(skewTrans.AngleY, 0, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

      scaleTrans.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
      scaleTrans.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
      skewTrans.BeginAnimation(SkewTransform.AngleXProperty, animSkewX);
      skewTrans.BeginAnimation(SkewTransform.AngleYProperty, animSkewY);
    }
  }

  /// <summary>
  /// Проигрывает entrance-анимации для всех элементов с StaggerIndex >= 0.
  /// </summary>
  public static void PlayEntrance(FrameworkElement? root, bool includeStaggerChildren = true)
  {
    if (root is null)
      return;

    var entries = CollectEntranceTargets(root, includeStaggerChildren);
    foreach (var (element, index, kind) in entries)
    {
      var delay = index * AnimationTimings.StaggerStepMs;
      PlaySingleEntrance(element, kind, delay, root);
    }
  }

  /// <summary>
  /// Stagger по прямым дочерним элементам панели (для списка городов и т.п.).
  /// </summary>
  public static void StaggerChildrenReveal(
      Panel panel,
      EntranceKind kind = EntranceKind.SlideUp,
      int startIndex = 0,
      FrameworkElement? pageRoot = null)
  {
    pageRoot ??= FindPageRoot(panel);

    if (!AnimationSettings.IsEnabled)
    {
      foreach (UIElement child in panel.Children)
      {
        if (child is FrameworkElement fe)
          AnimationHelper.SetVisibleInstant(fe);
      }
      return;
    }

    var i = 0;
    foreach (UIElement child in panel.Children)
    {
      if (child is not FrameworkElement fe)
        continue;

      var delay = (startIndex + i) * AnimationTimings.StaggerStepMs;
      PlaySingleEntrance(fe, kind, delay, pageRoot);
      i++;
    }
  }

  public static void PlaySingleEntrance(
      FrameworkElement element,
      EntranceKind kind,
      double delayMs,
      FrameworkElement? pageRoot = null)
  {
    if (!AnimationSettings.IsEnabled)
    {
      AnimationHelper.SetVisibleInstant(element);
      return;
    }

    var edge = MapEntranceKindToEdge(kind);
    var useBlur = kind == EntranceKind.BlurReveal;
    AnimationHelper.RevealFromEdge(element, edge, pageRoot, delayMs, useBlur: useBlur);
  }

  /// <summary>
  /// Инвертированная семантика: SlideUp = с верхней границы, SlideLeft = с левой и т.д.
  /// </summary>
  private static EdgeSide? MapEntranceKindToEdge(EntranceKind kind) =>
      kind switch
      {
        EntranceKind.SlideUp => EdgeSide.Top,
        EntranceKind.SlideDown => EdgeSide.Bottom,
        EntranceKind.SlideLeft => EdgeSide.Left,
        EntranceKind.SlideRight => EdgeSide.Right,
        EntranceKind.BlurReveal => null,
        _ => null
      };

  private static FrameworkElement FindPageRoot(DependencyObject start)
  {
    var current = start;
    while (current is not null)
    {
      if (current is UserControl pageRoot)
        return pageRoot;
      current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
    }

    return start as FrameworkElement
        ?? throw new InvalidOperationException("Cannot resolve page root for entrance animation.");
  }

  internal static List<(FrameworkElement Element, int Index, EntranceKind Kind)> CollectEntranceTargets(
      DependencyObject root,
      bool includeStaggerChildren)
  {
    var list = new List<(FrameworkElement, int, EntranceKind)>();
    Walk(root, list, includeStaggerChildren);
    return list.OrderBy(t => t.Item2).ToList();
  }

  private static void Walk(DependencyObject node, List<(FrameworkElement, int, EntranceKind)> list, bool includeStaggerChildren)
  {
    var count = VisualTreeHelper.GetChildrenCount(node);
    for (var i = 0; i < count; i++)
    {
      var child = VisualTreeHelper.GetChild(node, i);

      if (child is FrameworkElement fe)
      {
        var index = GetStaggerIndex(fe);
        if (index >= 0)
        {
          list.Add((fe, index, GetEntrance(fe)));
        }
        else if (includeStaggerChildren && GetStaggerChildren(fe) && fe is Panel panel)
        {
          var childIndex = 0;
          foreach (UIElement panelChild in panel.Children)
          {
            if (panelChild is FrameworkElement pfe)
              list.Add((pfe, 1000 + childIndex++, EntranceKind.SlideUp));
          }
          continue;
        }
      }

      Walk(child, list, includeStaggerChildren);
    }
  }
}

public static class AnimationTimings
{
  public const double EntranceMs = 650;
  public const double ExitMs = 280;
  public const double StaggerStepMs = 80;
  public const double NavIndicatorMs = 350;
  public const double ScalePulseMs = 300;
  public const double FileRowMs = 350;
}
