using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MediaIsland;

public partial class App : Application
{
    Mutex? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Şeffaf (layered) küçük pencerede GPU her karede geri okunuyor; yazılım render ölçümde
        // yarı CPU (%5,5'e karşı %11 tek çekirdek, çalarken) ve üçte bir bellek (75'e karşı 210 MB) kullandı.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        // İkinci kopya açılırsa iki ada üst üste binmesin.
        _singleInstance = new Mutex(true, "MediaIsland.SingleInstance", out var created);
        if (!created)
        {
            Shutdown();
            return;
        }

        // Pencere medya bulunana kadar gizli kalır; kendini MainWindow gösterir.
        await new MainWindow().StartAsync();
    }
}
