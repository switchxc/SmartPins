using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace SmartPins
{
    [SupportedOSPlatform("windows")]
    public class MouseHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly WindowPinManager _pinManager;

        public event EventHandler<MouseClickEventArgs>? MouseClick;

        public MouseHook(WindowPinManager pinManager)
        {
            _pinManager = pinManager;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT))!;
                var windowHandle = WindowFromPoint(hookStruct.pt);

                if (windowHandle != IntPtr.Zero && IsWindow(windowHandle))
                {
                    // Проверяем, что это не наше окно и не системные окна
                    if (windowHandle != new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow!).Handle)
                    {
                        var title = GetWindowTitle(windowHandle);
                        if (!string.IsNullOrEmpty(title) && title != "Program Manager")
                        {
                            MouseClick?.Invoke(this, new MouseClickEventArgs(windowHandle, hookStruct.pt));
                            
                            // Если включен режим закрепления, обрабатываем клик
                            if (_pinManager.IsPinMode)
                            {
                                _pinManager.HandleMouseClick(windowHandle);
                                return IntPtr.Zero; // Предотвращаем дальнейшую обработку клика
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private string GetWindowTitle(IntPtr handle)
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(handle, title, title.Capacity);
            return title.ToString();
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }

    public class MouseClickEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; }
        public MouseHook.POINT Point { get; }

        public MouseClickEventArgs(IntPtr windowHandle, MouseHook.POINT point)
        {
            WindowHandle = windowHandle;
            Point = point;
        }
    }
} 