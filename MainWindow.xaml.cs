using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaIsland;

public partial class MainWindow : Window
{
    static readonly Size CompactSize = new(104, 38);
    static readonly Size ExpandedSize = new(372, 172);
    static readonly Geometry PlayIcon = Geometry.Parse("M8,5 L19,12 L8,19 Z");
    static readonly Geometry PauseIcon = Geometry.Parse("M7,5 H10.5 V19 H7 Z M13.5,5 H17 V19 H13.5 Z");

    // Her çubuk kendi sırasıyla döner; ilk ve son kare aynı olduğundan döngü dikişsizdir.
    static readonly double[][] EqPatterns =
    [
        [5, 13, 8, 14, 6, 11],
        [9, 5, 14, 7, 12, 4],
        [12, 7, 10, 4, 14, 8],
        [6, 11, 5, 13, 9, 12],
    ];

    readonly MediaService _media = new();
    readonly DispatcherTimer _collapseTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    readonly DispatcherTimer _pulseTimer = new() { Interval = TimeSpan.FromSeconds(3.5) };
    readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly SolidColorBrush _accent;
    readonly GradientStop _glowInner, _glowOuter;
    MediaState? _state;
    string? _trackKey;
    bool _expanded, _hover;

    public MainWindow()
    {
        InitializeComponent();
        _accent = (SolidColorBrush)Resources["AccentBrush"];
        var glow = (RadialGradientBrush)Glow.Fill;
        _glowInner = glow.GradientStops[0];
        _glowOuter = glow.GradientStops[1];

        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            if (!_hover && !_pulseTimer.IsEnabled) SetExpanded(false);
        };
        _pulseTimer.Tick += (_, _) =>
        {
            _pulseTimer.Stop();
            if (!_hover) SetExpanded(false);
        };
        _tick.Tick += (_, _) =>
        {
            // Küçükken ilerleme görünmüyor; güncellemek şeffaf pencereyi boşuna yeniden çizdirir.
            if (_expanded) UpdateProgress();
            UpdateVisibility();
        };
        _media.Changed += OnMediaChanged;
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.WorkArea)) PlaceWindow();
        };
    }

    public async Task StartAsync()
    {
        _tick.Start();
        await _media.StartAsync();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeMethods.MakeToolWindow(new WindowInteropHelper(this).Handle);
    }

    void OnMediaChanged(MediaState? state)
    {
        var previous = _state;
        _state = state;

        if (state is not null)
        {
            TitleText.Text = state.Title;
            ArtistText.Text = state.Artist;
            SourceText.Text = state.Source.ToUpperInvariant();

            if (!ReferenceEquals(state.Cover, previous?.Cover))
                CoverBox.Background = state.Cover is null
                    ? (Brush)Resources["Rim"]
                    : new ImageBrush(state.Cover) { Stretch = Stretch.UniformToFill };
            if (state.Accent != previous?.Accent)
                AnimateAccent(state.Accent);
            if (state.IsPlaying != previous?.IsPlaying)
                SetPlaying(state.IsPlaying);

            PlayGlyph.Data = state.IsPlaying ? PauseIcon : PlayIcon;
            PlayButton.ToolTip = state.IsPlaying ? "Duraklat" : "Oynat";
            PlayButton.IsEnabled = state.CanTogglePlay;
            PrevButton.IsEnabled = state.CanPrevious;
            NextButton.IsEnabled = state.CanNext;
            SeekArea.Cursor = state.CanSeek ? Cursors.Hand : null;

            // Yeni parça (ya da ilk açılış): kısa süre açılıp ne çaldığını göster.
            var trackKey = $"{state.Source}|{state.Title}|{state.Artist}";
            if (trackKey != _trackKey)
            {
                _trackKey = trackKey;
                SetExpanded(true);
                _pulseTimer.Stop();
                _pulseTimer.Start();
            }
            UpdateProgress();
        }

        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        bool show = _state is not null && !NativeMethods.IsFullscreenAppActive();
        if (show && !IsVisible)
        {
            PlaceWindow();
            Show();
        }
        else if (!show && IsVisible)
        {
            Hide();
        }
    }

    void PlaceWindow()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;
    }

    void SetExpanded(bool expand)
    {
        if (_expanded == expand) return;
        _expanded = expand;

        var target = expand ? ExpandedSize : CompactSize;
        var duration = TimeSpan.FromMilliseconds(expand ? 480 : 340);
        IEasingFunction ease = expand
            ? new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
            : new CubicEase { EasingMode = EasingMode.EaseInOut };

        Animate(Island, WidthProperty, target.Width, duration, ease);
        Animate(Island, HeightProperty, target.Height, duration, ease);

        double cover = expand ? 64 : 26, inset = expand ? 15 : 5;
        Animate(CoverHost, WidthProperty, cover, duration, ease);
        Animate(CoverHost, HeightProperty, cover, duration, ease);
        CoverHost.BeginAnimation(MarginProperty,
            new ThicknessAnimation(new Thickness(inset, inset, 0, 0), duration) { EasingFunction = ease });

        UpdateGlow();
        ExpandedPanel.IsHitTestVisible = expand;
        ExpandedPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(expand ? 1 : 0, TimeSpan.FromMilliseconds(expand ? 240 : 100))
        {
            BeginTime = TimeSpan.FromMilliseconds(expand ? 160 : 0),
        });
        if (expand) UpdateProgress();
    }

    static void Animate(UIElement element, DependencyProperty property, double to, TimeSpan duration, IEasingFunction ease) =>
        element.BeginAnimation(property, new DoubleAnimation(to, duration) { EasingFunction = ease });

    void AnimateAccent(Color color)
    {
        var duration = TimeSpan.FromMilliseconds(600);
        _accent.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(color, duration));
        _glowInner.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(color, duration));
        _glowOuter.BeginAnimation(GradientStop.ColorProperty,
            new ColorAnimation(Color.FromArgb(0, color.R, color.G, color.B), duration));
    }

    /// <summary>Çalarken çubuklar oynar ve kapak rengi parlar; duraklatınca çubuklar noktaya iner.</summary>
    void SetPlaying(bool playing)
    {
        for (int i = 0; i < Equalizer.Children.Count; i++)
        {
            var bar = (Rectangle)Equalizer.Children[i];
            AnimationTimeline animation;
            if (playing)
            {
                var pattern = EqPatterns[i];
                var frames = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
                for (int k = 0; k <= pattern.Length; k++)
                    frames.KeyFrames.Add(new EasingDoubleKeyFrame(
                        pattern[k % pattern.Length],
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds((140 + i * 15) * k)),
                        new SineEase()));
                Timeline.SetDesiredFrameRate(frames, 30);
                animation = frames;
            }
            else
            {
                animation = new DoubleAnimation(3, TimeSpan.FromMilliseconds(250));
            }
            bar.BeginAnimation(HeightProperty, animation);
        }
        UpdateGlow();
    }

    /// <summary>Küçük hapta parlama yarım güçte; tam güçte hapın yarısını bulanık bir banda çeviriyor.</summary>
    void UpdateGlow()
    {
        var opacity = _state is not { IsPlaying: true } ? 0.18 : _expanded ? 0.55 : 0.25;
        Glow.BeginAnimation(OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(500)));
    }

    /// <summary>Kaynaklar konumu seyrek bildirir; aradaki süre son bildirimden itibaren tahmin edilir.</summary>
    void UpdateProgress()
    {
        if (_state is not { } s || s.Duration <= TimeSpan.Zero)
        {
            ProgressRow.Visibility = Visibility.Hidden;
            return;
        }
        ProgressRow.Visibility = Visibility.Visible;

        var position = s.Position;
        if (s.IsPlaying) position += (DateTimeOffset.Now - s.PositionUpdatedAt) * s.Rate;
        position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, s.Duration.Ticks));

        ElapsedText.Text = Format(position);
        DurationText.Text = Format(s.Duration);
        ProgressFill.Width = SeekArea.ActualWidth * (position / s.Duration);
    }

    static string Format(TimeSpan t) => t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");

    void Island_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Küçükken tam hap, büyüdükçe 30px köşeli kart; içerik de aynı yuvarlaklıkla kırpılır.
        var radius = Math.Min(e.NewSize.Height / 2, 30);
        Island.CornerRadius = new CornerRadius(radius);
        var inner = new Rect(0, 0, Math.Max(0, e.NewSize.Width - 2), Math.Max(0, e.NewSize.Height - 2));
        IslandContent.Clip = new RectangleGeometry(inner, radius - 1, radius - 1);
    }

    void CoverHost_SizeChanged(object sender, SizeChangedEventArgs e) =>
        CoverBox.CornerRadius = new CornerRadius(e.NewSize.Width * 0.22);

    void Island_MouseEnter(object sender, MouseEventArgs e)
    {
        _hover = true;
        _collapseTimer.Stop();
        SetExpanded(true);
    }

    void Island_MouseLeave(object sender, MouseEventArgs e)
    {
        _hover = false;
        _collapseTimer.Start();
    }

    async void PlayPause_Click(object sender, RoutedEventArgs e) => await _media.TogglePlayPauseAsync();

    async void Prev_Click(object sender, RoutedEventArgs e) => await _media.PreviousAsync();

    async void Next_Click(object sender, RoutedEventArgs e) => await _media.NextAsync();

    async void Seek_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_state is not { CanSeek: true } s || s.Duration <= TimeSpan.Zero) return;
        var ratio = Math.Clamp(e.GetPosition(SeekArea).X / SeekArea.ActualWidth, 0, 1);
        await _media.SeekAsync(s.Duration * ratio);
    }

    void Quit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
