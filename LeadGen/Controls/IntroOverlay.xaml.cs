using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LeadGen.Controls;

public partial class IntroOverlay : UserControl
{
    private Storyboard? _mainStoryboard;
    private bool _isSkipped;
    private readonly List<AnimationClock> _activeClocks = [];

    // Событие завершения интро
    public event EventHandler? Completed;

    public IntroOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!LeadGen.Helpers.AnimationSettings.IsEnabled)
        {
            CompleteIntro();
            return;
        }

        // Даем фокус, чтобы ловить нажатия клавиш
        Focus();
        Keyboard.Focus(this);

        // Инициализируем и запускаем анимации
        BuildAndStartIntro();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // Пропускаем по ESC, Space или Enter
        if (e.Key == Key.Escape || e.Key == Key.Space || e.Key == Key.Enter)
        {
            SkipIntro();
            e.Handled = true;
        }
    }

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        SkipIntro();
    }

    private void SkipIntro()
    {
        if (_isSkipped) return;
        _isSkipped = true;

        // Останавливаем все запущенные часы анимаций
        foreach (var clock in _activeClocks)
        {
            clock.Controller?.Stop();
        }
        _activeClocks.Clear();

        if (_mainStoryboard != null)
        {
            _mainStoryboard.Stop();
            _mainStoryboard = null;
        }

        // Запускаем мгновенное красивое растворение за 300мс
        PlayQuickOutro();
    }

    private void BuildAndStartIntro()
    {
        _mainStoryboard = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(3800);

        // --- ФАЗА 1: Стягивание сети точек к центру (0.0с - 1.2с) ---
        var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };
        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easeInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var elasticOut = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 };
        var backOut = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 };

        // Анимация точек (Dot1 - Dot8) с использованием TranslateTransform
        // Мы будем анимировать отступы (Margin) от их исходного положения к центру (200, 200)
        // Координаты центра: Width=400, Height=400, центр (200,200)
        // Точки сходятся в (196, 196) с учетом размера точки (8х8)
        var dotTargets = new[]
        {
            (Dot1, new Thickness(76, 96, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot2, new Thickness(326, 116, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot3, new Thickness(296, 316, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot4, new Thickness(86, 306, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot5, new Thickness(56, 196, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot6, new Thickness(246, 66, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot7, new Thickness(316, 216, 0, 0), new Thickness(196, 196, 0, 0)),
            (Dot8, new Thickness(156, 336, 0, 0), new Thickness(196, 196, 0, 0))
        };

        foreach (var (dot, from, to) in dotTargets)
        {
            var anim = new ThicknessAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(1200)))
            {
                EasingFunction = easeIn
            };
            Storyboard.SetTarget(anim, dot);
            Storyboard.SetTargetProperty(anim, new PropertyPath(MarginProperty));
            _mainStoryboard.Children.Add(anim);

            // Плавное исчезновение точек в момент слияния (1.0с - 1.25с)
            var fade = new DoubleAnimationUsingKeyFrames();
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000))));
            fade.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1250)), easeOut));
            Storyboard.SetTarget(fade, dot);
            Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
            _mainStoryboard.Children.Add(fade);
        }

        // Анимация линий (Line1 - Line8), их сжатие к центру и исчезновение
        // X1/Y1 сходятся к X2/Y2 (200, 200)
        var lineTargets = new[]
        {
            (Line1, 80.0, 100.0), (Line2, 330.0, 120.0), (Line3, 300.0, 320.0), (Line4, 90.0, 310.0),
            (Line5, 60.0, 200.0), (Line6, 250.0, 70.0), (Line7, 320.0, 220.0), (Line8, 160.0, 340.0)
        };

        foreach (var (line, x1, y1) in lineTargets)
        {
            var animX = new DoubleAnimation(x1, 200, TimeSpan.FromMilliseconds(1200)) { EasingFunction = easeIn };
            Storyboard.SetTarget(animX, line);
            Storyboard.SetTargetProperty(animX, new PropertyPath(Line.X1Property));
            _mainStoryboard.Children.Add(animX);

            var animY = new DoubleAnimation(y1, 200, TimeSpan.FromMilliseconds(1200)) { EasingFunction = easeIn };
            Storyboard.SetTarget(animY, line);
            Storyboard.SetTargetProperty(animY, new PropertyPath(Line.Y1Property));
            _mainStoryboard.Children.Add(animY);

            // Движение штрихов по линии (имитация тока)
            var dashAnim = new DoubleAnimation(0, -60, TimeSpan.FromMilliseconds(1200));
            Storyboard.SetTarget(dashAnim, line);
            Storyboard.SetTargetProperty(dashAnim, new PropertyPath(Line.StrokeDashOffsetProperty));
            _mainStoryboard.Children.Add(dashAnim);

            // Исчезновение линий
            var fadeLine = new DoubleAnimationUsingKeyFrames();
            fadeLine.KeyFrames.Add(new LinearDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900))));
            fadeLine.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1200)), easeOut));
            Storyboard.SetTarget(fadeLine, line);
            Storyboard.SetTargetProperty(fadeLine, new PropertyPath(OpacityProperty));
            _mainStoryboard.Children.Add(fadeLine);
        }


        // --- ФАЗА 2: Вспышка и проявление логотипа LG (1.1с - 2.2с) ---
        // Появление границы логотипа с упругим отскоком
        var logoBorderFade = new DoubleAnimationUsingKeyFrames();
        logoBorderFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1100))));
        logoBorderFade.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1600)), easeOut));
        Storyboard.SetTarget(logoBorderFade, IntroLogoHost);
        Storyboard.SetTargetProperty(logoBorderFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(logoBorderFade);

        var logoScaleX = new DoubleAnimationUsingKeyFrames();
        logoScaleX.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1100))));
        logoScaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1800)), backOut));
        Storyboard.SetTarget(logoScaleX, LogoScale);
        Storyboard.SetTargetProperty(logoScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
        _mainStoryboard.Children.Add(logoScaleX);

        var logoScaleY = logoScaleX.Clone();
        Storyboard.SetTarget(logoScaleY, LogoScale);
        Storyboard.SetTargetProperty(logoScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
        _mainStoryboard.Children.Add(logoScaleY);

        // Свечение логотипа (Вспышка и затухание в пульс)
        var glowFade = new DoubleAnimationUsingKeyFrames();
        glowFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1150))));
        glowFade.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1300)), easeOut));
        glowFade.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2200)), easeInOut));
        Storyboard.SetTarget(glowFade, LogoGlow);
        Storyboard.SetTargetProperty(glowFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(glowFade);


        // --- ФАЗА 3: Кинематографичное раздвижение букв и появление надписи (1.5с - 3.2с) ---
        var letters = new[]
        {
            (LetterL, TransL, -45.0, -75.0),
            (LetterE, TransE, -30.0, -50.0),
            (LetterA, TransA, -15.0, -25.0),
            (LetterD, TransD, 0.0, 0.0),
            (LetterG, TransG, 15.0, 25.0),
            (LetterE2, TransE2, 30.0, 50.0),
            (LetterN, TransN, 45.0, 75.0)
        };

        foreach (var (block, trans, startX, endX) in letters)
        {
            // Плавное проявление буквы с задержкой (1.5с - 2.2с)
            var lFade = new DoubleAnimationUsingKeyFrames();
            lFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1500))));
            lFade.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2100)), easeOut));
            Storyboard.SetTarget(lFade, block);
            Storyboard.SetTargetProperty(lFade, new PropertyPath(OpacityProperty));
            _mainStoryboard.Children.Add(lFade);

            // Раздвижение (трекинг) по оси X (1.5с - 3.2с)
            var lMove = new DoubleAnimationUsingKeyFrames();
            lMove.KeyFrames.Add(new DiscreteDoubleKeyFrame(startX, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1500))));
            lMove.KeyFrames.Add(new EasingDoubleKeyFrame(endX, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3200)), easeOut));
            Storyboard.SetTarget(lMove, trans);
            Storyboard.SetTargetProperty(lMove, new PropertyPath(TranslateTransform.XProperty));
            _mainStoryboard.Children.Add(lMove);
        }


        // --- ФАЗА 3.5: Консоль инициализации и прогресс (1.4с - 3.2с) ---
        // Плавное проявление прогресс-бара
        var barBorderFade = new DoubleAnimationUsingKeyFrames();
        barBorderFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400))));
        barBorderFade.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1700)), easeOut));
        Storyboard.SetTarget(barBorderFade, IntroProgressBorder);
        Storyboard.SetTargetProperty(barBorderFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(barBorderFade);

        // Нарастание прогресс-бара до 180px
        var barProgress = new DoubleAnimationUsingKeyFrames();
        barProgress.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400))));
        barProgress.KeyFrames.Add(new EasingDoubleKeyFrame(50, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1900)), easeOut));
        barProgress.KeyFrames.Add(new EasingDoubleKeyFrame(120, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2500)), easeInOut));
        barProgress.KeyFrames.Add(new EasingDoubleKeyFrame(180, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3100)), easeOut));
        Storyboard.SetTarget(barProgress, IntroProgressBar);
        Storyboard.SetTargetProperty(barProgress, new PropertyPath(WidthProperty));
        _mainStoryboard.Children.Add(barProgress);

        // Анимация консольного лога (текст меняется через С# таймеры, а проявление/сдвиг - через Storyboard)
        var consoleFade = new DoubleAnimationUsingKeyFrames();
        consoleFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1300))));
        consoleFade.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1600)), easeOut));
        Storyboard.SetTarget(consoleFade, ConsoleContainer);
        Storyboard.SetTargetProperty(consoleFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(consoleFade);

        // Появление кнопки пропуска через 0.6с
        var skipFade = new DoubleAnimationUsingKeyFrames();
        skipFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600))));
        skipFade.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1200)), easeOut));
        Storyboard.SetTarget(skipFade, SkipBtn);
        Storyboard.SetTargetProperty(skipFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(skipFade);


        // --- ФАЗА 4: Величественное растворение (Outro) (3.2с - 3.8с) ---
        // Плавное увеличение всего контента
        var rootScaleX = new DoubleAnimationUsingKeyFrames();
        rootScaleX.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3200))));
        rootScaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3800)), easeInOut));
        Storyboard.SetTarget(rootScaleX, RootScale);
        Storyboard.SetTargetProperty(rootScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
        _mainStoryboard.Children.Add(rootScaleX);

        var rootScaleY = rootScaleX.Clone();
        Storyboard.SetTarget(rootScaleY, RootScale);
        Storyboard.SetTargetProperty(rootScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
        _mainStoryboard.Children.Add(rootScaleY);

        // Плавное размытие всего контента перед исчезновением
        var blur = new BlurEffect { Radius = 0 };
        RootGrid.Effect = blur;
        var blurAnim = new DoubleAnimationUsingKeyFrames();
        blurAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3200))));
        blurAnim.KeyFrames.Add(new EasingDoubleKeyFrame(16, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3800)), easeIn));
        Storyboard.SetTarget(blurAnim, RootGrid);
        Storyboard.SetTargetProperty(blurAnim, new PropertyPath("Effect.Radius"));
        _mainStoryboard.Children.Add(blurAnim);

        // Полное исчезновение оверлея
        var rootFade = new DoubleAnimationUsingKeyFrames();
        rootFade.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3200))));
        rootFade.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3800)), easeIn));
        Storyboard.SetTarget(rootFade, this);
        Storyboard.SetTargetProperty(rootFade, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(rootFade);


        // Инициализируем C# таймеры для обновления консольных надписей в такт прогресс-бару
        SetupConsoleStages();

        // Запуск главного Storyboard
        _mainStoryboard.Completed += (s, _) => CompleteIntro();
        _mainStoryboard.Begin(this, isControllable: true);
    }

    private void SetupConsoleStages()
    {
        var dispatcher = Dispatcher;
        
        void AddConsoleTimer(int delayMs, string text)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
            timer.Tick += (s, _) =>
            {
                timer.Stop();
                if (!_isSkipped)
                {
                    AnimateConsoleMessage(text);
                }
            };
            timer.Start();
        }

        AddConsoleTimer(100, "[SYS] Connecting deep intelligence...");
        AddConsoleTimer(1300, "[PARSE] Re-mapping Webbee data schemas...");
        AddConsoleTimer(1950, "[DB] Syncing leads local index...");
        AddConsoleTimer(2600, "[SYS] Systems verified. Launching main UI...");
    }

    private void AnimateConsoleMessage(string text)
    {
        // Элегантная анимация смены текста: сдвиг вниз с исчезновением, смена текста, сдвиг вверх с проявлением
        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fadeOut = new DoubleAnimation(ConsoleLine.Opacity, 0, TimeSpan.FromMilliseconds(150)) { EasingFunction = easeIn };
        var moveDown = new DoubleAnimation(ConsoleTrans.Y, 10, TimeSpan.FromMilliseconds(150)) { EasingFunction = easeIn };

        fadeOut.Completed += (s, e) =>
        {
            ConsoleLine.Text = text;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = easeOut };
            var moveUp = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = easeOut };
            ConsoleLine.BeginAnimation(OpacityProperty, fadeIn);
            ConsoleTrans.BeginAnimation(TranslateTransform.YProperty, moveUp);
        };

        ConsoleLine.BeginAnimation(OpacityProperty, fadeOut);
        ConsoleTrans.BeginAnimation(TranslateTransform.YProperty, moveDown);
    }

    private void PlayQuickOutro()
    {
        // Создаем изящное быстрое размытие и исчезновение за 350мс
        var easeInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var duration = TimeSpan.FromMilliseconds(350);

        // Масштаб
        var rootScaleX = new DoubleAnimation(RootScale.ScaleX, 1.05, duration) { EasingFunction = easeInOut };
        var rootScaleY = new DoubleAnimation(RootScale.ScaleY, 1.05, duration) { EasingFunction = easeInOut };
        RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, rootScaleX);
        RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, rootScaleY);

        // Размытие
        var blur = new BlurEffect { Radius = 0 };
        RootGrid.Effect = blur;
        var blurAnim = new DoubleAnimation(0, 12, duration) { EasingFunction = easeInOut };
        blurAnim.Completed += (s, e) => CompleteIntro();
        blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);

        // Непрозрачность самого UserControl
        var fade = new DoubleAnimation(Opacity, 0, duration) { EasingFunction = easeInOut };
        BeginAnimation(OpacityProperty, fade);
    }

    private void CompleteIntro()
    {
        if (Visibility == Visibility.Collapsed) return;
        Visibility = Visibility.Collapsed;
        Completed?.Invoke(this, EventArgs.Empty);
    }
}
