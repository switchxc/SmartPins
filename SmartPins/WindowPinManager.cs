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
using System.Runtime.Versioning;
using System.Windows.Threading;

namespace SmartPins
{
    [SupportedOSPlatform("windows")]
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
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;
        private const uint LWA_ALPHA = 0x00000002;
        private const uint ULW_ALPHA = 0x00000002;
        private const int TRANSPARENT = 1;
        private const int PS_SOLID = 0;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private readonly Dictionary<IntPtr, bool> pinnedWindows = new();
        private readonly Dictionary<IntPtr, PinIndicatorWindow> pinIndicators = new();
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
                SetWindowPos(windowHandle, new IntPtr(-1), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
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
                SetWindowPos(windowHandle, new IntPtr(-2), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
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

        public void UnpinAllWindows()
        {
            var windowsToUnpin = new List<IntPtr>(pinnedWindows.Keys);
            foreach (var windowHandle in windowsToUnpin)
            {
                UnpinWindow(windowHandle);
            }
        }

        private void EnablePinMode()
        {
            if (originalCursor == null)
            {
                originalCursor = Cursor.Current;
            }
            Cursor.Current = CreatePinCursor();
        }

        private void DisablePinMode()
        {
            if (originalCursor != null)
            {
                Cursor.Current = originalCursor;
                originalCursor = null;
            }
        }

        private Cursor CreatePinCursor()
        {
            try
            {
                var bitmap = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                    using (var brush = new SolidBrush(System.Drawing.Color.White))
                    {
                        // Рисуем рамку
                        g.DrawRectangle(pen, 2, 2, 28, 28);
                        // Рисуем булавку
                        g.FillEllipse(brush, 12, 8, 8, 8);
                        g.DrawLine(pen, 16, 16, 16, 24);
                    }
                }
                return new Cursor(bitmap.GetHicon());
            }
            catch
            {
                return Cursors.Cross;
            }
        }

        private void AddPinIndicator(IntPtr windowHandle)
        {
            try
            {
                Console.WriteLine($"AddPinIndicator: Добавляем рамку для окна {windowHandle}");
                if (!pinIndicators.ContainsKey(windowHandle))
                {
                    var indicator = new PinIndicatorWindow(windowHandle);
                    pinIndicators[windowHandle] = indicator;
                    indicator.Show();
                    Console.WriteLine($"AddPinIndicator: Рамка создана и показана");
                }
                else
                {
                    Console.WriteLine($"AddPinIndicator: Рамка уже существует для окна {windowHandle}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления рамки: {ex.Message}");
            }
        }

        private void RemovePinIndicator(IntPtr windowHandle)
        {
            try
            {
                Console.WriteLine($"RemovePinIndicator: Удаляем рамку для окна {windowHandle}");
                if (pinIndicators.TryGetValue(windowHandle, out var indicator))
                {
                    indicator.Close();
                    pinIndicators.Remove(windowHandle);
                    Console.WriteLine($"RemovePinIndicator: Рамка удалена");
                }
                else
                {
                    Console.WriteLine($"RemovePinIndicator: Рамка не найдена для окна {windowHandle}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления рамки: {ex.Message}");
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

                    // Проверяем, что окно существует
                    if (!IsWindow(windowHandle))
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

                    // Отключаем режим закрепления после клика
                    IsPinMode = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обработки клика: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            // Уничтожаем все рамки при закрытии
            foreach (var indicator in pinIndicators.Values)
            {
                try { indicator.Close(); } catch { }
            }
            pinIndicators.Clear();
        }
    }

    // Крутое окно-индикатор закрепления с анимированной рамкой
    [SupportedOSPlatform("windows")]
    public class PinIndicatorWindow : Window
    {
        private readonly IntPtr _targetWindow;
        private readonly DispatcherTimer _syncTimer;
        private readonly DispatcherTimer _animationTimer;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public PinIndicatorWindow(IntPtr targetWindow)
        {
            _targetWindow = targetWindow;
            
            // Настройка окна
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Manual;
            
            // Создаём контент с анимированной рамкой
            CreateContent();
            
            // Таймеры для синхронизации и анимации
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _syncTimer.Tick += SyncPosition;
            
            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
            _animationTimer.Tick += Animate;
            
            // Начинаем анимацию появления
            _animationTimer.Start();
            
            // Синхронизируем позицию
            SyncPosition();
            _syncTimer.Start();
        }

        private void CreateContent()
        {
            var grid = new System.Windows.Controls.Grid();
            
            // Создаём анимированную рамку
            var border = new System.Windows.Controls.Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };

            grid.Children.Add(border);
            Content = grid;
        }

        private void SyncPosition(object? sender, EventArgs e)
        {
            if (!IsWindow(_targetWindow))
            {
                Close();
                return;
            }

            GetWindowRect(_targetWindow, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // Позиционируем рамку вокруг окна
            Left = rect.Left + 2;
            Top = rect.Top - 2;
            Width = width - 4;
            Height = height + 2;

            // Перемещаем окно-индикатор сразу под закреплённым окном
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, _targetWindow, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void Animate(object? sender, EventArgs e)
        {
            // Отключаем анимацию прозрачности, рамка всегда видима
            Opacity = 1.0;
        }

        public new void Hide()
        {
            _animationTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _syncTimer?.Stop();
            _animationTimer?.Stop();
            base.OnClosed(e);
        }

        private void SyncPosition()
        {
            SyncPosition(null, EventArgs.Empty);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            // Делаем окно невзаимодействующим
            var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);
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
} 