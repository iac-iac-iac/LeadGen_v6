using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LeadGen.Models;
using LeadGen.Services;
using LeadGen.ViewModels;
using LeadGen.Helpers;

namespace LeadGen.Views;

public partial class CityManagerWindow : Window
{
    private const double StackThreshold = 720;
    private readonly CityManagerViewModel _viewModel;

    public CityManagerWindow(AppConfig config, ConfigService configService)
    {
        InitializeComponent();

        _viewModel = new CityManagerViewModel(config, configService);
        _viewModel.CloseRequested += success =>
        {
            DialogResult = success;
            Close();
        };
        DataContext = _viewModel;

        Loaded += (_, _) =>
        {
            ApplyLayout(ActualWidth);
            AnimateMainWindowBlur(true);
            PlayEntranceAnimation();
        };

        Closing += (_, _) =>
        {
            AnimateMainWindowBlur(false);
        };
    }

    private void PlayEntranceAnimation()
    {
        if (!AnimationSettings.IsEnabled)
        {
            RootGrid.Opacity = 1;
            RootScale.ScaleX = 1;
            RootScale.ScaleY = 1;
            return;
        }

        var duration = TimeSpan.FromMilliseconds(450);
        var easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 };

        var animOpacity = new DoubleAnimation(0, 1, duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var animScaleX = new DoubleAnimation(0.9, 1, duration) { EasingFunction = easing };
        var animScaleY = new DoubleAnimation(0.9, 1, duration) { EasingFunction = easing };

        RootGrid.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
        RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
    }

    private void AnimateMainWindowBlur(bool enable)
    {
        if (!AnimationSettings.IsEnabled)
            return;

        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow is null)
            return;

        var mainGrid = mainWindow.FindName("MainContentGrid") as Grid;
        if (mainGrid is null)
            return;

        if (enable)
        {
            var blur = new System.Windows.Media.Effects.BlurEffect { Radius = 0 };
            mainGrid.Effect = blur;
            var anim = new DoubleAnimation(0, 12, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            blur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, anim);
        }
        else
        {
            if (mainGrid.Effect is System.Windows.Media.Effects.BlurEffect blur)
            {
                var anim = new DoubleAnimation(blur.Radius, 0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                anim.Completed += (s, _) => mainGrid.Effect = null;
                blur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, anim);
            }
            else
            {
                mainGrid.Effect = null;
            }
        }
    }

    private void OnDeleteDistrictsClick(object sender, RoutedEventArgs e)
    {
        var selected = DistrictsList.SelectedItems;
        if (_viewModel.DeleteSelectedDistrictsCommand.CanExecute(selected))
            _viewModel.DeleteSelectedDistrictsCommand.Execute(selected);
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyLayout(e.NewSize.Width);

    private void ApplyLayout(double width)
    {
        var stacked = width < StackThreshold;

        if (stacked)
        {
            PanelsGrid.ColumnDefinitions[1].Width = new GridLength(0);
            PanelsGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            PanelsGrid.RowDefinitions[1].Height = new GridLength(12);
            PanelsGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(CitiesPanel, 0);
            Grid.SetRow(CitiesPanel, 0);
            Grid.SetColumn(DistrictsPanel, 0);
            Grid.SetRow(DistrictsPanel, 2);

            CitiesList.MinHeight = 80;
            DistrictsList.MinHeight = 80;
            CitiesList.ClearValue(MaxHeightProperty);
            DistrictsList.ClearValue(MaxHeightProperty);
        }
        else
        {
            PanelsGrid.ColumnDefinitions[1].Width = new GridLength(16);
            PanelsGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            PanelsGrid.RowDefinitions[1].Height = new GridLength(0);
            PanelsGrid.RowDefinitions[2].Height = new GridLength(0);

            Grid.SetColumn(CitiesPanel, 0);
            Grid.SetRow(CitiesPanel, 0);
            Grid.SetColumn(DistrictsPanel, 2);
            Grid.SetRow(DistrictsPanel, 0);

            ClearListHeight(CitiesList);
            ClearListHeight(DistrictsList);
        }
    }

    private static void ClearListHeight(ListBox list)
    {
        list.MinHeight = 100;
        list.ClearValue(MaxHeightProperty);
        list.ClearValue(HeightProperty);
    }
}
