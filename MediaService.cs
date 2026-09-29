using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;

namespace MediaIsland;

public sealed record MediaState(
    string Title,
    string Artist,
    string Source,
    BitmapSource? Cover,
    Color Accent,
    bool IsPlaying,
    bool CanPrevious,
    bool CanNext,
    bool CanTogglePlay,
    bool CanSeek,
    TimeSpan Position,
    TimeSpan Duration,
    DateTimeOffset PositionUpdatedAt,
    double Rate);

/// <summary>
/// Windows'un ortak medya oturumlarını (GSMTC) izler. Spotify, Chrome/Edge/Firefox'taki YouTube
/// ve Windows'a medya bilgisi bildiren her uygulama buradan gelir. Değişiklikler UI thread'inde yayınlanır.
/// </summary>
public sealed class MediaService
{
    readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    readonly List<GlobalSystemMediaTransportControlsSession> _watched = [];
    GlobalSystemMediaTransportControlsSessionManager? _manager;
    GlobalSystemMediaTransportControlsSession? _session;
    string? _coverKey;
    BitmapSource? _cover;
    Color _accent = CoverArt.FallbackAccent;
    bool _refreshing, _dirty;

    /// <summary>Çalan bir şey yoksa null.</summary>
    public event Action<MediaState?>? Changed;

    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.SessionsChanged += (_, _) => _dispatcher.InvokeAsync(() =>
        {
            WatchSessions();
            RefreshLoop();
        });
        _manager.CurrentSessionChanged += (_, _) => RequestRefresh();
        WatchSessions();
        RefreshLoop();
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_session is not null) await _session.TryTogglePlayPauseAsync();
    }

    public async Task PreviousAsync()
    {
        if (_session is not null) await _session.TrySkipPreviousAsync();
    }

    public async Task NextAsync()
    {
        if (_session is not null) await _session.TrySkipNextAsync();
    }

    public async Task SeekAsync(TimeSpan position)
    {
        if (_session is not null) await _session.TryChangePlaybackPositionAsync(position.Ticks);
    }

    /// <summary>
    /// Sadece seçili oturumu değil hepsini dinler: Spotify duraklatılmışken YouTube'da video başlarsa
    /// ada ona geçebilsin.
    /// </summary>
    void WatchSessions()
    {
        foreach (var s in _watched)
        {
            try
            {
                s.MediaPropertiesChanged -= OnSessionChanged;
                s.PlaybackInfoChanged -= OnSessionChanged;
                s.TimelinePropertiesChanged -= OnSessionChanged;
            }
            catch (COMException)
            {
                // Uygulama kapandıysa oturum nesnesi artık erişilemez; bırakmak yeterli.
            }
        }
        _watched.Clear();

        foreach (var s in _manager!.GetSessions())
        {
            s.MediaPropertiesChanged += OnSessionChanged;
            s.PlaybackInfoChanged += OnSessionChanged;
            s.TimelinePropertiesChanged += OnSessionChanged;
            _watched.Add(s);
        }
    }

    void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args) => RequestRefresh();

    // WinRT olayları thread pool'dan gelir; okuma UI thread'inde sırayla yapılır.
    void RequestRefresh() => _dispatcher.InvokeAsync(RefreshLoop);

    /// <summary>Okuma sürerken gelen olaylar tek bir ek okumada birleştirilir.</summary>
    async void RefreshLoop()
    {
        if (_refreshing)
        {
            _dirty = true;
            return;
        }

        _refreshing = true;
        try
        {
            do
            {
                _dirty = false;
                try
                {
                    Changed?.Invoke(await ReadStateAsync());
                }
                catch (COMException)
                {
                    // Oturum okunurken uygulama kapandı; ardından gelen SessionsChanged yeniden okur.
                }
            } while (_dirty);
        }
        finally
        {
            _refreshing = false;
        }
    }

    async Task<MediaState?> ReadStateAsync()
    {
        var session = _session = PickSession();
        if (session is null) return null;

        var props = await session.TryGetMediaPropertiesAsync();
        if (string.IsNullOrWhiteSpace(props.Title)) return null;
        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();

        // Tarayıcılar önce başlığı, sonra küçük resmi bildirir; küçük resim gelince yeniden yükle.
        var coverKey = $"{session.SourceAppUserModelId}|{props.Title}|{props.Artist}|{props.Thumbnail is not null}";
        if (coverKey != _coverKey)
        {
            _coverKey = coverKey;
            _cover = props.Thumbnail is null ? null : await CoverArt.LoadAsync(props.Thumbnail);
            _accent = _cover is null ? CoverArt.FallbackAccent : CoverArt.Accent(_cover);
        }

        var controls = playback.Controls;
        return new MediaState(
            props.Title,
            props.Artist,
            FriendlyName(session.SourceAppUserModelId),
            _cover,
            _accent,
            playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            controls.IsPreviousEnabled,
            controls.IsNextEnabled,
            controls.IsPlayPauseToggleEnabled,
            controls.IsPlaybackPositionEnabled,
            timeline.Position,
            timeline.EndTime - timeline.StartTime,
            timeline.LastUpdatedTime,
            playback.PlaybackRate is double rate && rate > 0 ? rate : 1);
    }

    /// <summary>Önce Windows'un güncel oturumu çalıyorsa o, sonra çalan herhangi biri, sonra duraklatılmış olan.</summary>
    GlobalSystemMediaTransportControlsSession? PickSession()
    {
        static bool IsPlaying(GlobalSystemMediaTransportControlsSession s) =>
            s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        var current = _manager!.GetCurrentSession();
        if (current is not null && IsPlaying(current)) return current;
        var sessions = _manager.GetSessions();
        return sessions.FirstOrDefault(IsPlaying) ?? current ?? sessions.FirstOrDefault();
    }

    /// <summary>"Spotify.exe" → "Spotify", "MSEdge" → "Edge"; Firefox 16 haneli hash kimlik kullanır.</summary>
    static string FriendlyName(string appId)
    {
        var name = appId.Split('!')[0].Split('_')[0];
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        name = name[(name.LastIndexOf('.') + 1)..];
        if (name.Length == 16 && name.All(Uri.IsHexDigit)) return "Firefox";
        return name.ToLowerInvariant() switch
        {
            "msedge" => "Edge",
            "chrome" => "Chrome",
            _ => name,
        };
    }
}
