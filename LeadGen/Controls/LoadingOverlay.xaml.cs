using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LeadGen.Helpers;
using LeadGen.ViewModels;

namespace LeadGen.Controls;

public partial class LoadingOverlay : UserControl
{
    private MainViewModel? _viewModel;

    public LoadingOverlay()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            _viewModel = newVm;
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
        else
        {
            _viewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel == null) return;

        if (e.PropertyName == nameof(MainViewModel.LoadingProgress))
        {
            // Анимируем ширину PremiumProgressBar до нового значения (максимум 260)
            double targetWidth = (_viewModel.LoadingProgress / 100.0) * 260.0;
            if (AnimationSettings.IsEnabled)
            {
                var anim = new System.Windows.Media.Animation.DoubleAnimation(PremiumProgressBar.Width, targetWidth, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                PremiumProgressBar.BeginAnimation(WidthProperty, anim);
            }
            else
            {
                PremiumProgressBar.Width = targetWidth;
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.LoadingStageText))
        {
            // Элегантная микро-анимация смены этапа
            if (AnimationSettings.IsEnabled)
            {
                var easeIn = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn };
                var easeOut = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(PremiumStageText.Opacity, 0, TimeSpan.FromMilliseconds(150)) { EasingFunction = easeIn };
                var moveDown = new System.Windows.Media.Animation.DoubleAnimation(StageTextTrans.Y, 10, TimeSpan.FromMilliseconds(150)) { EasingFunction = easeIn };

                fadeOut.Completed += (s, ev) =>
                {
                    PremiumStageText.Text = _viewModel.LoadingStageText;

                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = easeOut };
                    var moveUp = new System.Windows.Media.Animation.DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = easeOut };

                    PremiumStageText.BeginAnimation(OpacityProperty, fadeIn);
                    StageTextTrans.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, moveUp);
                };

                PremiumStageText.BeginAnimation(OpacityProperty, fadeOut);
                StageTextTrans.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, moveDown);
            }
            else
            {
                PremiumStageText.Text = _viewModel.LoadingStageText;
            }
        }
    }

    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    public void PlayEntrance()
    {
        PremiumProgressBar.Width = 0;
        AnimationHelper.LoadingOverlayEntrance(Backdrop, OverlayCard);
    }
}
