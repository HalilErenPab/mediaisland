using System.Runtime.InteropServices;

namespace MediaIsland;

static class NativeMethods
{
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_NOACTIVATE = 0x08000000;

    // SHQueryUserNotificationState dönüş değerleri
    const int QUNS_BUSY = 2;
    const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
    const int QUNS_PRESENTATION_MODE = 4;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("shell32.dll")]
    static extern int SHQueryUserNotificationState(out int state);

    /// <summary>Alt+Tab'da görünmesin ve tıklanınca odağı çalışılan uygulamadan çalmasın.</summary>
    public static void MakeToolWindow(IntPtr hwnd) =>
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

    /// <summary>Tam ekran video/oyun/sunum açıkken true.</summary>
    public static bool IsFullscreenAppActive() =>
        SHQueryUserNotificationState(out var state) == 0
        && state is QUNS_BUSY or QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE;
}
