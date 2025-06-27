using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;

namespace SmartPins
{
    public partial class PinIndicatorWindow : Window
    {
        private readonly IntPtr _targetWindow;
        private readonly DispatcherTimer _syncTimer;
        private int _lastLeft, _lastTop, _lastWidth, _lastHeight;

        public PinIndicatorWindow(IntPtr targetWindow)
        {
            InitializeComponent();
            _targetWindow = targetWindow;
            Loaded += PinIndicatorWindow_Loaded;
            Closed += PinIndicatorWindow_Closed;
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
            _syncTimer.Tick += SyncWithTargetWindow;
        }

        private void PinIndicatorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            SyncWithTargetWindow(null, null);
            _syncTimer.Start();
        }

        private void PinIndicatorWindow_Closed(object? sender, EventArgs e)
        {
            _syncTimer.Stop();
        }

        private void SyncWithTargetWindow(object? sender, EventArgs? e)
        {
            if (_targetWindow == IntPtr.Zero)
                return;
            if (!GetWindowRect(_targetWindow, out RECT rect))
                return;

            // Автоопределение толщины рамки через DWM
            int border = GetSystemBorderThickness(_targetWindow);
            int radius = GetSystemCornerRadius(_targetWindow);

            int left = rect.Left + border;
            int top = rect.Top + border - 7;
            int width = (rect.Right - rect.Left) - border * 2;
            int height = (rect.Bottom - rect.Top) - border * 2 + 8;
    
            if (left != _lastLeft || top != _lastTop || width != _lastWidth || height != _lastHeight)
            {
                this.Left = left;
                this.Top = top;
                this.Width = width;
                this.Height = height;
                _lastLeft = left;
                _lastTop = top;
                _lastWidth = width;
                _lastHeight = height;
            }

            // Устанавливаем скругление бордера
            if (this.Content is Border borderElem)
                borderElem.CornerRadius = new CornerRadius(radius);

            // Устанавливаем индикатор за закреплённым окном
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, _targetWindow, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private int GetSystemBorderThickness(IntPtr hwnd)
        {
            // DwmGetWindowAttribute с DWMWA_EXTENDED_FRAME_BOUNDS
            RECT frameRect;
            if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out frameRect, Marshal.SizeOf(typeof(RECT))) == 0)
            {
                if (GetWindowRect(hwnd, out RECT winRect))
                {
                    int border = Math.Max(Math.Abs(winRect.Left - frameRect.Left), Math.Abs(winRect.Top - frameRect.Top));
                    return border;
                }
            }
            // Fallback
            return 8; // Обычно 8px на 100% DPI
        }

        private int GetSystemCornerRadius(IntPtr hwnd)
        {
            // В Windows 11 можно попробовать DWMWA_WINDOW_CORNER_PREFERENCE, но проще подобрать вручную
            // Обычно 8px, иногда 6px
            return 8;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
} 