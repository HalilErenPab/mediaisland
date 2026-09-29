# MediaIsland

A Dynamic Island–style "now playing" widget for the Windows desktop. It sits as a small black
pill in the bottom-right corner, just above the taskbar, and expands when you hover over it.

- **Works with:** Spotify, YouTube in Chrome/Edge/Firefox, and any app that reports media to
  Windows (through the system media API, GSMTC). No per-app integration.
- **Compact:** cover art + an equalizer tinted with the cover's color (flattens to dots when paused).
- **Expanded:** large cover, source / title / artist, progress bar (click to seek, when the source
  supports it), previous / play-pause / next.
- Pops open for 3.5 s when a new track starts.
- The accent color is extracted from each track's cover and glows out from behind it.
- Hides when nothing is playing and during fullscreen video, games or presentations.
- Stays out of Alt+Tab and never steals focus from the app you're working in.
- To quit: right-click the island → **MediaIsland'ı kapat** ("Close MediaIsland").

## Requirements

Windows 10 2004+ or Windows 11, .NET 10 SDK.

## Install

```bash
powershell -ExecutionPolicy Bypass -File kurulum.ps1
```

Publishes a single-file `MediaIsland.exe` to `%LOCALAPPDATA%\Programs\MediaIsland`, adds a Start
menu shortcut and launches it. No admin rights needed. Re-run the same command after changing the
code; it closes the running instance and updates it. The exe requires the .NET 10 Desktop Runtime.

To start it again after closing, search for **MediaIsland** in the Start menu.

## Development

```bash
dotnet run
```

## Project structure

| File | Purpose |
|---|---|
| `MediaService.cs` | Watches GSMTC sessions, picks which one to show, forwards commands |
| `CoverArt.cs` | Loads the cover, trims YouTube's letterbox bars, extracts the accent color |
| `MainWindow.xaml(.cs)` | Island UI, expand animation, progress, visibility |
| `NativeMethods.cs` | Tool-window style, fullscreen detection |
| `kurulum.ps1` | Publish + install to the user folder + Start menu shortcut |
| `app.ico` | App icon (black island + equalizer) |

## Known limitations

- Shows the cover art / thumbnail only, not the video itself.
- On YouTube, "previous" is usually only available inside a playlist (that's what the browser reports).
- For browser playback the source shows as "CHROME" / "EDGE"; the API doesn't expose which site is playing.
- The equalizer is an animation, not a real audio level meter.
- UI labels (tooltips, menu) are currently in Turkish.
