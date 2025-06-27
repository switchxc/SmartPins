using System;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Drawing;
using Hardcodet.Wpf.TaskbarNotification;

namespace SmartPins
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // Применяем тёмную тему по умолчанию
                var paletteHelper = new PaletteHelper();
                ITheme theme = paletteHelper.GetTheme();
                theme.SetBaseTheme(Theme.Dark);
                paletteHelper.SetTheme(theme);

                // Создаём иконку в трее
                CreateTrayIcon();
                
                // Показываем главное окно при запуске
                if (MainWindow != null)
                {
                    MainWindow.Show();
                    MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateTrayIcon()
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath)!, "pin.ico");
                if (!System.IO.File.Exists(iconPath))
                {
                    taskbarIcon = new TaskbarIcon
                    {
                        Icon = System.Drawing.SystemIcons.Application,
                        ToolTipText = "SmartPins - Умное закрепление окон",
                        Visibility = Visibility.Visible
                    };
                }
                else
                {
                    taskbarIcon = new TaskbarIcon
                    {
                        Icon = new Icon(iconPath),
                        ToolTipText = "SmartPins - Умное закрепление окон",
                        Visibility = Visibility.Visible
                    };
                }

                var contextMenu = new System.Windows.Controls.ContextMenu();
                var showItem = new System.Windows.Controls.MenuItem { Header = "Показать" };
                showItem.Click += (s, e) =>
                {
                    if (MainWindow != null)
                    {
                        MainWindow.Show();
                        MainWindow.WindowState = WindowState.Normal;
                        MainWindow.Activate();
                    }
                };
                var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
                exitItem.Click += (s, e) => Shutdown();
                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(exitItem);
                taskbarIcon.ContextMenu = contextMenu;
                taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ToggleActiveWindow();
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                taskbarIcon = new TaskbarIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    ToolTipText = "SmartPins - Умное закрепление окон",
                    Visibility = Visibility.Visible
                };
                System.Diagnostics.Debug.WriteLine($"Ошибка создания иконки в трее: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            taskbarIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
