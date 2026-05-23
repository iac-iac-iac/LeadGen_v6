using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LeadGen.Models;
using LeadGen.ViewModels;

namespace LeadGen;

public partial class MainWindow : Window
{
  private readonly MainViewModel _viewModel;
  private bool _loadingEntrancePlayed;

  public MainWindow()
  {
    InitializeComponent();
    _viewModel = new MainViewModel();
    DataContext = _viewModel;
    _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    StateChanged += OnWindowStateChanged;
    Loaded += OnWindowLoaded;
    PageHost.EntranceReady += OnPageEntranceReady;
    IntroOverlay.Completed += OnIntroOverlayCompleted;
    UpdateMaximizeIcon();
  }

  private void OnIntroOverlayCompleted(object? sender, EventArgs e)
  {
    PlayStaggeredEntrance();
  }

  private void PlayStaggeredEntrance()
  {
    if (!Helpers.AnimationSettings.IsEnabled)
    {
      MainContentGrid.Opacity = 1.0;
      HeaderBorder.Opacity = 1.0;
      PageHost.Opacity = 1.0;
      return;
    }

    // Делаем основную сетку видимой
    MainContentGrid.Opacity = 1.0;

    // Скрываем элементы перед каскадной анимацией
    HeaderBorder.Opacity = 0;
    PageHost.Opacity = 0;

    var headerTranslate = new System.Windows.Media.TranslateTransform(0, -25);
    HeaderBorder.RenderTransform = headerTranslate;

    var hostTranslate = new System.Windows.Media.TranslateTransform(0, 30);
    PageHost.RenderTransform = hostTranslate;

    var easeOut = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

    // Анимация Header (сдвиг вниз)
    var headerFade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(550)) { EasingFunction = easeOut };
    var headerMove = new System.Windows.Media.Animation.DoubleAnimation(-25, 0, TimeSpan.FromMilliseconds(550)) { EasingFunction = easeOut };

    HeaderBorder.BeginAnimation(OpacityProperty, headerFade);
    headerTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, headerMove);

    // Анимация PageHost (сдвиг вверх) с задержкой 150мс
    var hostFade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(650))
    {
      BeginTime = TimeSpan.FromMilliseconds(150),
      EasingFunction = easeOut
    };
    var hostMove = new System.Windows.Media.Animation.DoubleAnimation(30, 0, TimeSpan.FromMilliseconds(650))
    {
      BeginTime = TimeSpan.FromMilliseconds(150),
      EasingFunction = easeOut
    };

    PageHost.BeginAnimation(OpacityProperty, hostFade);
    hostTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, hostMove);
  }

  private void OnPageEntranceReady(object? sender, FrameworkElement root)
  {
    if (root is Views.LinkGeneratorView linksView)
      linksView.PlayCitiesStaggerIfNeeded();
  }

  private void OnWindowLoaded(object sender, RoutedEventArgs e)
  {
    UpdateNavIndicator(animate: false);
  }

  private void OnMinimizeClick(object sender, RoutedEventArgs e) =>
      WindowState = WindowState.Minimized;

  private void OnMaximizeClick(object sender, RoutedEventArgs e) =>
      WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

  private void OnCloseClick(object sender, RoutedEventArgs e) =>
      Close();

  private void OnWindowStateChanged(object? sender, EventArgs e) =>
      UpdateMaximizeIcon();

  private void UpdateMaximizeIcon()
  {
    if (MaximizeButton is null)
      return;

    MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Восстановить" : "Развернуть";
  }

  private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(MainViewModel.CurrentPage))
      UpdateNavIndicator(animate: true);

    if (e.PropertyName == nameof(MainViewModel.IsLoading))
    {
      if (_viewModel.IsLoading)
      {
        LoadingOverlay.SetMessage(_viewModel.LoadingMessage);
        if (!_loadingEntrancePlayed)
        {
          _loadingEntrancePlayed = true;
          LoadingOverlay.PlayEntrance();

          if (Helpers.AnimationSettings.IsEnabled)
          {
            var blur = new System.Windows.Media.Effects.BlurEffect { Radius = 0 };
            MainContentGrid.Effect = blur;
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 15, TimeSpan.FromMilliseconds(400))
            {
              EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            blur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, anim);
          }
        }
      }
      else
      {
        _loadingEntrancePlayed = false;
        if (Helpers.AnimationSettings.IsEnabled && MainContentGrid.Effect is System.Windows.Media.Effects.BlurEffect blur)
        {
          var anim = new System.Windows.Media.Animation.DoubleAnimation(blur.Radius, 0, TimeSpan.FromMilliseconds(300))
          {
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
          };
          anim.Completed += (s, _) => MainContentGrid.Effect = null;
          blur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, anim);
        }
        else
        {
          MainContentGrid.Effect = null;
        }
      }
    }
    else if (e.PropertyName == nameof(MainViewModel.LoadingMessage) && _viewModel.IsLoading)
    {
      LoadingOverlay.SetMessage(_viewModel.LoadingMessage);
    }
  }

  private void UpdateNavIndicator(bool animate)
  {
  if (NavIndicator is null || NavContainer is null)
      return;

    var active = _viewModel.CurrentPage switch
    {
      AppPage.Processing => NavProcessing,
      AppPage.LinkGenerator => NavLinks,
      _ => NavDashboard
    };

    if (active is null)
      return;

    NavContainer.UpdateLayout();
    active.UpdateLayout();

    var offset = active.TranslatePoint(new Point(0, 0), NavContainer).X;
    var width = active.ActualWidth;

    if (width <= 0)
      return;

    if (animate)
      Helpers.AnimationHelper.SlideNavIndicator(NavIndicator, offset, width);
    else
    {
      NavIndicator.Width = width;
      if (NavIndicator.RenderTransform is System.Windows.Media.TranslateTransform tt)
        tt.X = offset;
      else
        NavIndicator.RenderTransform = new System.Windows.Media.TranslateTransform(offset, 0);
    }
  }
}
