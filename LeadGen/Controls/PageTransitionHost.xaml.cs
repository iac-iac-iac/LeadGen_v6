using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LeadGen.Helpers;

namespace LeadGen.Controls;

public partial class PageTransitionHost : UserControl
{
  public static readonly DependencyProperty PageContentProperty =
      DependencyProperty.Register(
          nameof(PageContent),
          typeof(object),
          typeof(PageTransitionHost),
          new PropertyMetadata(null, OnPageContentChanged));

  private object? _pendingContent;
  private bool _isTransitioning;
  private int _layoutWaitAttempts;
  private readonly Dictionary<object, FrameworkElement> _viewCache = new();

  public PageTransitionHost()
  {
    InitializeComponent();
  }

  public object? PageContent
  {
    get => GetValue(PageContentProperty);
    set => SetValue(PageContentProperty, value);
  }

  /// <summary>
  /// Вызывается после смены контента и запуска entrance-анимации.
  /// </summary>
  public event EventHandler<FrameworkElement>? EntranceReady;

  private static void OnPageContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is PageTransitionHost host)
      host.HandleContentChange(e.OldValue, e.NewValue);
  }

  private void HandleContentChange(object? oldValue, object? newValue)
  {
    if (newValue is null)
    {
      ContentHost.Content = null;
      return;
    }

    if (ReferenceEquals(oldValue, newValue) && GetVisualRoot() is not null)
      return;

    _pendingContent = newValue;

    if (_isTransitioning)
      return;

    var outgoing = GetVisualRoot();

    if (outgoing is null || !AnimationSettings.IsEnabled)
    {
      ApplyContentAndAnimateEntrance();
      return;
    }

    _isTransitioning = true;
    AnimationHelper.FadeOut(outgoing, AnimationTimings.ExitMs, () =>
    {
      Dispatcher.BeginInvoke(() =>
      {
        // Сбрасываем opacity корня — view уходит в кэш и иначе остаётся невидимым.
        AnimationHelper.SetVisibleInstant(outgoing);
        _isTransitioning = false;
        ApplyContentAndAnimateEntrance();
      }, DispatcherPriority.Normal);
    });
  }

  private void ApplyContentAndAnimateEntrance()
  {
    _layoutWaitAttempts = 0;

    var view = GetOrCreateView(_pendingContent);
    if (view is not null)
    {
      ResetPageShell(view);
      ContentHost.Content = view;
    }
    else
      ContentHost.Content = _pendingContent;

    ScheduleEntranceAnimation();
  }

  /// <summary>
  /// Один экран на ViewModel — иначе OxyPlot падает (PlotModel уже привязан к другому PlotView).
  /// </summary>
  private FrameworkElement? GetOrCreateView(object? content)
  {
    if (content is FrameworkElement element)
      return element;

    if (content is null)
      return null;

    if (_viewCache.TryGetValue(content, out var cached))
    {
      ResetPageShell(cached);
      return cached;
    }

    var template = ResolveViewTemplate(content.GetType());
    if (template?.LoadContent() is not FrameworkElement root)
      return null;

    root.DataContext = content;
    _viewCache[content] = root;

    if (AnimationSettings.IsEnabled)
      PrepareEntranceTargets(root);

    return root;
  }

  private static DataTemplate? ResolveViewTemplate(Type viewModelType)
  {
    var app = Application.Current;
    if (app is null)
      return null;

    foreach (var dictionary in EnumerateDictionaries(app.Resources))
    {
      foreach (var key in dictionary.Keys)
      {
        if (key is DataTemplateKey templateKey &&
            templateKey.DataType is Type dataType &&
            dataType.IsAssignableFrom(viewModelType) &&
            dictionary[key] is DataTemplate template)
          return template;

        if (key is Type typeKey &&
            typeKey.IsAssignableFrom(viewModelType) &&
            dictionary[key] is DataTemplate typeTemplate)
          return typeTemplate;
      }
    }

    return null;
  }

  private static IEnumerable<ResourceDictionary> EnumerateDictionaries(ResourceDictionary resources)
  {
    yield return resources;
    foreach (var merged in resources.MergedDictionaries)
    {
      foreach (var nested in EnumerateDictionaries(merged))
        yield return nested;
    }
  }

  private void ScheduleEntranceAnimation()
  {
    if (!AnimationSettings.IsEnabled)
    {
      if (GetVisualRoot() is FrameworkElement instant)
        SetEntranceVisibleInstant(instant);
      return;
    }

    Dispatcher.BeginInvoke(RunEntranceAnimation, DispatcherPriority.Loaded);
  }

  private void RunEntranceAnimation()
  {
    var root = GetVisualRoot();
    if (root is null)
    {
      if (_layoutWaitAttempts++ < 8)
      {
        Dispatcher.BeginInvoke(RunEntranceAnimation, DispatcherPriority.Loaded);
        return;
      }

      return;
    }

    ResetPageShell(root);
    PrepareEntranceTargets(root);
    AnimationBehavior.PlayEntrance(root, includeStaggerChildren: false);
    EntranceReady?.Invoke(this, root);
  }

  /// <summary>
  /// Корень страницы не участвует в stagger — после FadeOut иначе остаётся с Opacity=0.
  /// </summary>
  private static void ResetPageShell(FrameworkElement root)
  {
    root.BeginAnimation(UIElement.OpacityProperty, null);
    root.Opacity = 1;
    root.Visibility = Visibility.Visible;
  }

  private static void PrepareEntranceTargets(FrameworkElement root)
  {
    foreach (var (element, _, _) in AnimationBehavior.CollectEntranceTargets(root, includeStaggerChildren: false))
    {
      element.BeginAnimation(UIElement.OpacityProperty, null);
      element.RenderTransform = Transform.Identity;
      element.Effect = null;
      element.Opacity = 0;
    }
  }

  private static void SetEntranceVisibleInstant(FrameworkElement root)
  {
    AnimationHelper.SetVisibleInstant(root);
    foreach (var (element, _, _) in AnimationBehavior.CollectEntranceTargets(root, includeStaggerChildren: false))
      AnimationHelper.SetVisibleInstant(element);
  }

  private FrameworkElement? GetVisualRoot()
  {
    if (ContentHost.Content is FrameworkElement direct)
      return direct;

    ContentHost.UpdateLayout();
    if (VisualTreeHelper.GetChildrenCount(ContentHost) > 0)
      return VisualTreeHelper.GetChild(ContentHost, 0) as FrameworkElement;

    return null;
  }
}
