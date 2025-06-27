using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;

namespace SmartPins
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private DispatcherTimer statusTimer = new DispatcherTimer();
        private Hotkey? pinHotkey;
        private WindowPinManager pinManager;
        private MouseHook? mouseHook;
        private string settingsFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartPins", "settings.json");
        private string currentHotkey = "Ctrl+Alt+P";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Инициализация менеджера закрепления окон
            pinManager = new WindowPinManager();
            pinManager.WindowPinned += OnWindowPinned;
            pinManager.WindowUnpinned += OnWindowUnpinned;

            // Инициализация горячих клавиш
            InitializeHotkeys();

            // Инициализация хука мыши
            mouseHook = new MouseHook(pinManager);
            mouseHook.MouseClick += OnMouseClick;

            // Инициализация таймера статуса
            InitializeStatusTimer();

            // Загрузка настроек
            LoadSettings();
            
            // Обновление статистики
            UpdateStatistics();
            
            // Добавляем обработчик для перетаскивания окна
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private void InitializeHotkeys()
        {
            try
            {
                pinHotkey = new Hotkey(ModifierKeys.Control | ModifierKeys.Alt, Key.P);
                pinHotkey.Pressed += (s, e) => ToggleActiveWindow();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка регистрации горячих клавиш: {ex.Message}");
            }
        }

        private void InitializeStatusTimer()
        {
            statusTimer.Interval = TimeSpan.FromSeconds(2);
            statusTimer.Tick += (s, e) =>
            {
                // Статусная строка убрана, ничего не делаем
            };
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    var json = File.ReadAllText(settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    
                    if (settings != null && !string.IsNullOrEmpty(settings.Hotkey))
                    {
                        var (modifiers, key) = ParseHotkeyString(settings.Hotkey);
                        pinHotkey = new Hotkey(modifiers, key);
                        pinHotkey.Pressed += (s, args) => ToggleActiveWindow();
                        currentHotkey = settings.Hotkey;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
            // Обновляем отображение
            UpdateHotkeyText();
        }

        private void SaveSettings(string hotkey)
        {
            try
            {
                var settings = new AppSettings { Hotkey = hotkey };
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                
                var directory = System.IO.Path.GetDirectoryName(settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }
                
                File.WriteAllText(settingsFilePath, json);
                currentHotkey = hotkey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
            // Обновляем отображение
            UpdateHotkeyText();
        }

        private void UpdateHotkeyText()
        {
            if (CurrentHotkeyText != null)
                CurrentHotkeyText.Text = currentHotkey;
        }

        private void ShowStatus(string message)
        {
            // Статусная строка убрана, ничего не делаем
        }

        private void EnablePinMode()
        {
            pinManager.IsPinMode = true;
            ShowStatus("Режим закрепления активирован. Кликните на заголовок окна для закрепления.");
        }

        public void ToggleActiveWindow()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero || foregroundWindow == new System.Windows.Interop.WindowInteropHelper(this).Handle)
                    return;

                var title = GetWindowTitle(foregroundWindow);
                var processName = GetProcessName(foregroundWindow);

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(processName))
                    return;

                // Проверяем, закреплено ли уже окно
                if (pinManager.IsWindowPinned(foregroundWindow))
                {
                    // Открепляем окно
                    pinManager.UnpinWindow(foregroundWindow);
                }
                else
                {
                    // Закрепляем окно
                    pinManager.PinWindow(foregroundWindow);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка переключения закрепления окна: {ex.Message}");
            }
        }

        private void PinActiveWindow()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero || foregroundWindow == new System.Windows.Interop.WindowInteropHelper(this).Handle)
                    return;

                var title = GetWindowTitle(foregroundWindow);
                var processName = GetProcessName(foregroundWindow);

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(processName))
                    return;

                // Используем менеджер закрепления
                pinManager.PinWindow(foregroundWindow);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка закрепления окна: {ex.Message}");
            }
        }

        private void OnWindowPinned(object? sender, WindowPinEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatistics();
            });
        }

        private void OnWindowUnpinned(object? sender, WindowPinEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatistics();
            });
        }

        private void OnMouseClick(object? sender, MouseHookEventArgs e)
        {
            // Обработка клика мыши для режима закрепления
            if (pinManager.IsPinMode)
            {
                pinManager.HandleMouseClick(e.WindowHandle);
            }
        }

        private string GetWindowTitle(IntPtr handle)
        {
            var title = new StringBuilder(256);
            GetWindowText(handle, title, title.Capacity);
            return title.ToString();
        }

        private string GetProcessName(IntPtr handle)
        {
            GetWindowThreadProcessId(handle, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return "Неизвестный процесс";
            }
        }

        private void PinActiveWindow_Click(object sender, RoutedEventArgs e)
        {
            ToggleActiveWindow();
        }

        private void PinWithCursor_Click(object sender, RoutedEventArgs e)
        {
            EnablePinMode();
        }

        private void UnpinAll_Click(object sender, RoutedEventArgs e)
        {
            pinManager.UnpinAllWindows();
            UpdateStatistics();
        }

        private void EnablePinMode_Click(object sender, RoutedEventArgs e)
        {
            EnablePinMode();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            pinHotkey?.Dispose();
            mouseHook?.Dispose();
            pinManager?.Dispose();
            base.OnClosing(e);
        }

        private void UpdateStatistics()
        {
            var pinnedCount = pinManager.GetPinnedWindows().Count();
            var totalCount = GetTotalWindowsCount();
            
            PinnedWindowsCount.Text = pinnedCount.ToString();
            TotalWindowsCount.Text = totalCount.ToString();
        }

        private int GetTotalWindowsCount()
        {
            return Process.GetProcesses().Length;
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            var hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.Owner = this;
            
            if (hotkeyWindow.ShowDialog() == true)
            {
                // Сохраняем новую горячую клавишу
                var newHotkey = hotkeyWindow.SelectedHotkey;
                if (!string.IsNullOrEmpty(newHotkey))
                {
                    try
                    {
                        // Освобождаем старую горячую клавишу
                        pinHotkey?.Dispose();
                        
                        // Парсим новую комбинацию
                        var (modifiers, key) = ParseHotkeyString(newHotkey);
                        
                        // Создаем новую горячую клавишу
                        pinHotkey = new Hotkey(modifiers, key);
                        pinHotkey.Pressed += (s, args) => ToggleActiveWindow();
                        
                        // Сохраняем в настройки
                        SaveSettings(newHotkey);
                        
                        ShowStatus($"Горячая клавиша изменена на: {newHotkey}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при установке горячей клавиши: {ex.Message}", 
                                       "Ошибка", 
                                       MessageBoxButton.OK, 
                                       MessageBoxImage.Error);
                    }
                }
            }
            // Обновляем отображение
            UpdateHotkeyText();
        }

        private (ModifierKeys modifiers, Key key) ParseHotkeyString(string hotkeyString)
        {
            var parts = hotkeyString.Split('+');
            var modifiers = ModifierKeys.None;
            var key = Key.None;
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                
                switch (trimmedPart.ToUpper())
                {
                    case "CTRL":
                        modifiers |= ModifierKeys.Control;
                        break;
                    case "ALT":
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case "SHIFT":
                        modifiers |= ModifierKeys.Shift;
                        break;
                    case "WIN":
                        modifiers |= ModifierKeys.Windows;
                        break;
                    default:
                        // Пытаемся найти клавишу
                        if (Enum.TryParse<Key>(trimmedPart, true, out var parsedKey))
                        {
                            key = parsedKey;
                        }
                        break;
                }
            }
            
            return (modifiers, key);
        }
    }

    public class AppSettings
    {
        public string Hotkey { get; set; } = "Ctrl+Alt+P";
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}