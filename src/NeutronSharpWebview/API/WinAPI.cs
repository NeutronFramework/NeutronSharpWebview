using System.Runtime.InteropServices;

namespace NeutronSharpWebview.API;

public static partial class WinAPI
{
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOZORDER = 0x0004;
    public const int SW_MAXIMIZE = 3;
    public const int SM_CXSCREEN = 0x0000;
    public const int SM_CYSCREEN = 0x0001;
    public const int SW_MINIMIZE = 6;
    public const int SWP_NOMOVE = 0x0002;
    public const int GWL_WNDPROC = -4;
    public const int GWL_STYLE = -16;
    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_MAXIMIZEBOX = 0x00010000;
    public const int SW_RESTORE = 9;
    public const int SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_NOOWNERZORDER = 0x0200;
    public const uint SPI_GETWORKAREA = 0x003;
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_MINIMIZEBOX = 0x00020000;
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000; // Add WS_OVERLAPPEDWINDOW

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [LibraryImport("user32.dll")]
    public static partial byte SystemParametersInfoA(uint uiAction, uint uiParam, out Rect rect, uint fWinIni);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint Msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint hWnd, out Rect lpRect);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetWindowLongPtr(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
}
