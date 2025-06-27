using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace SmartPins
{
    public class WindowPinManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOPMOST = 0x00008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;
        private const uint LWA_ALPHA = 0x00000002;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private readonly Dictionary<IntPtr, bool> pinnedWindows = new();
        private readonly Dictionary<IntPtr, IntPtr> borderWindows = new(); // Окна-индикаторы
        private readonly Dictionary<IntPtr, WindowBorderOverlay> borderOverlays = new();
        private bool isPinMode = false;
        private Cursor? originalCursor;

        public event EventHandler<WindowPinEventArgs>? WindowPinned;
        public event EventHandler<WindowPinEventArgs>? WindowUnpinned;

        public bool IsPinMode
        {
            get => isPinMode;
            set
            {
                isPinMode = value;
                if (isPinMode)
                {
                    EnablePinMode();
                }
                else
                {
                    DisablePinMode();
                }
            }
        }

        public void PinWindow(IntPtr windowHandle)
        {
            if (pinnedWindows.ContainsKey(windowHandle))
                return;

            try
            {
                // Закрепляем окно
                SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
                // Добавляем визуальную индикацию
                AddPinIndicator(windowHandle);
                
                pinnedWindows[windowHandle] = true;
                WindowPinned?.Invoke(this, new WindowPinEventArgs(windowHandle));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка закрепления окна: {ex.Message}");
            }
        }

        public void UnpinWindow(IntPtr windowHandle)
        {
            if (!pinnedWindows.ContainsKey(windowHandle))
                return;

            try
            {
                // Открепляем окно
                SetWindowPos(windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
                // Убираем визуальную индикацию
                RemovePinIndicator(windowHandle);
                
                pinnedWindows.Remove(windowHandle);
                WindowUnpinned?.Invoke(this, new WindowPinEventArgs(windowHandle));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка открепления окна: {ex.Message}");
            }
        }

        public bool IsWindowPinned(IntPtr windowHandle)
        {
            return pinnedWindows.ContainsKey(windowHandle);
        }

        public IEnumerable<IntPtr> GetPinnedWindows()
        {
            return pinnedWindows.Keys;
        }

        private void EnablePinMode()
        {
            try
            {
                // Сохраняем текущий курсор
                originalCursor = Cursor.Current;
                
                // Создаем курсор-иконку закрепления
                var pinCursor = CreatePinCursor();
                Cursor.Current = pinCursor;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка активации режима закрепления: {ex.Message}", "Ошибка", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void DisablePinMode()
        {
            // Восстанавливаем оригинальный курсор
            if (originalCursor != null)
            {
                Cursor.Current = originalCursor;
                originalCursor = null;
            }
        }

        private Cursor CreatePinCursor()
        {
            // Создаем простую иконку закрепления
            var bitmap = new Bitmap(32, 32);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                
                // Рисуем иконку закрепления
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue))
                {
                    // Рисуем булавку
                    graphics.FillEllipse(brush, 12, 8, 8, 8);
                    graphics.DrawLine(pen, 16, 16, 16, 24);
                    graphics.DrawLine(pen, 12, 20, 20, 20);
                }
            }

            return new Cursor(bitmap.GetHicon());
        }

        private void AddPinIndicator(IntPtr windowHandle)
        {
            try
            {
                if (!borderOverlays.ContainsKey(windowHandle))
                {
                    var overlay = new WindowBorderOverlay(windowHandle);
                    borderOverlays[windowHandle] = overlay;
                    overlay.Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления рамки: {ex.Message}");
            }
        }

        private void RemovePinIndicator(IntPtr windowHandle)
        {
            try
            {
                if (borderOverlays.TryGetValue(windowHandle, out var overlay))
                {
                    overlay.Hide();
                    overlay.Dispose();
                    borderOverlays.Remove(windowHandle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления рамки: {ex.Message}");
            }
        }

        public void HandleMouseClick(IntPtr windowHandle)
        {
            if (IsPinMode)
            {
                try
                {
                    // Проверяем, что окно валидное
                    if (windowHandle == IntPtr.Zero)
                        return;

                    // Проверяем, что это не наше окно
                    var mainWindowHandle = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow!).Handle;
                    if (windowHandle == mainWindowHandle)
                        return;

                    // Получаем заголовок окна для проверки
                    var title = GetWindowTitle(windowHandle);
                    if (string.IsNullOrEmpty(title) || title == "Program Manager")
                        return;

                    // Переключаем состояние закрепления
                    if (IsWindowPinned(windowHandle))
                    {
                        UnpinWindow(windowHandle);
                    }
                    else
                    {
                        PinWindow(windowHandle);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка обработки клика: {ex.Message}", "Ошибка", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                finally
                {
                    // Отключаем режим закрепления после клика
                    IsPinMode = false;
                }
            }
        }

        private string GetWindowTitle(IntPtr handle)
        {
            var buffer = new System.Text.StringBuilder(256);
            _ = NativeMethods.GetWindowText(handle, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        public void UnpinAllWindows()
        {
            // Копируем ключи, чтобы избежать модификации коллекции во время итерации
            foreach (var handle in GetPinnedWindows().ToList())
            {
                UnpinWindow(handle);
            }
        }

        public void Dispose()
        {
            // Уничтожаем все рамки при закрытии
            foreach (var overlay in borderOverlays.Values)
            {
                try { overlay.Dispose(); } catch { }
            }
            borderOverlays.Clear();
        }
    }

    public class WindowPinEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; }
        public string WindowTitle { get; }
        public string ProcessName { get; }

        public WindowPinEventArgs(IntPtr windowHandle)
        {
            WindowHandle = windowHandle;
            WindowTitle = GetWindowTitle(windowHandle);
            ProcessName = GetProcessName(windowHandle);
        }

        private static string GetWindowTitle(IntPtr handle)
        {
            var buffer = new System.Text.StringBuilder(256);
            _ = NativeMethods.GetWindowText(handle, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        private static string GetProcessName(IntPtr handle)
        {
            _ = NativeMethods.GetWindowThreadProcessId(handle, out uint processId);
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById((int)processId);
                return proc.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }

    // Добавить WinAPI константы и функции
    internal static class WinApiDefs
    {
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int SW_SHOW = 5;
        public const int SW_HIDE = 0;
        public const int LWA_ALPHA = 0x00000002;

        [DllImport("user32.dll")]
        public static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string? lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }

    // Новый класс для рисования белой анимированной рамки
    public class WindowBorderOverlay
    {
        private readonly IntPtr _targetWindow;
        private IntPtr _overlayHwnd = IntPtr.Zero;
        private byte _currentAlpha = 0;
        private byte _targetAlpha = 255;
        private System.Threading.Timer? _animationTimer;
        private bool _isVisible = false;

        public WindowBorderOverlay(IntPtr targetWindow)
        {
            _targetWindow = targetWindow;
        }

        public void Show()
        {
            if (_overlayHwnd == IntPtr.Zero)
                CreateOverlay();
            _targetAlpha = 255;
            StartAnimation();
        }

        public void Hide()
        {
            _targetAlpha = 0;
            StartAnimation();
        }

        private void CreateOverlay()
        {
            WinApiDefs.GetWindowRect(_targetWindow, out WinApiDefs.RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            var hInstance = WinApiDefs.GetModuleHandle(null);
            _overlayHwnd = WinApiDefs.CreateWindowEx(
                WinApiDefs.WS_EX_LAYERED | WinApiDefs.WS_EX_TRANSPARENT | WinApiDefs.WS_EX_TOOLWINDOW | WinApiDefs.WS_EX_TOPMOST,
                "STATIC", null,
                WinApiDefs.WS_POPUP,
                rect.Left, rect.Top, width, height,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            WinApiDefs.SetLayeredWindowAttributes(_overlayHwnd, 0, _currentAlpha, WinApiDefs.LWA_ALPHA);
            WinApiDefs.ShowWindow(_overlayHwnd, WinApiDefs.SW_SHOW);
            Redraw();
        }

        private void Redraw()
        {
            if (_overlayHwnd == IntPtr.Zero) return;
            WinApiDefs.GetWindowRect(_targetWindow, out WinApiDefs.RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            WinApiDefs.MoveWindow(_overlayHwnd, rect.Left, rect.Top, width, height, true);

            using (var g = System.Drawing.Graphics.FromHwnd(_overlayHwnd))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 1))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
            }
        }

        private void StartAnimation()
        {
            _animationTimer?.Dispose();
            _animationTimer = new System.Threading.Timer(_ => Animate(), null, 0, 15);
        }

        private void Animate()
        {
            if (_currentAlpha == _targetAlpha)
            {
                _animationTimer?.Dispose();
                if (_currentAlpha == 0 && _overlayHwnd != IntPtr.Zero)
                {
                    WinApiDefs.ShowWindow(_overlayHwnd, WinApiDefs.SW_HIDE);
                }
                return;
            }
            if (_currentAlpha < _targetAlpha)
                _currentAlpha += 15;
            else if (_currentAlpha > _targetAlpha)
                _currentAlpha -= 15;
            if (_currentAlpha > 255) _currentAlpha = 255;
            if (_currentAlpha < 0) _currentAlpha = 0;
            WinApiDefs.SetLayeredWindowAttributes(_overlayHwnd, 0, _currentAlpha, WinApiDefs.LWA_ALPHA);
            if (_currentAlpha > 0)
                WinApiDefs.ShowWindow(_overlayHwnd, WinApiDefs.SW_SHOW);
            Redraw();
        }

        public void Dispose()
        {
            _animationTimer?.Dispose();
            if (_overlayHwnd != IntPtr.Zero)
            {
                WinApiDefs.DestroyWindow(_overlayHwnd);
                _overlayHwnd = IntPtr.Zero;
            }
        }
    }
} 