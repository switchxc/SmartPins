﻿using System;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using System.Drawing;
using System.Runtime.Versioning;

namespace SmartPins
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon;
        private WindowPinManager? pinManager;
        private MouseHook? mouseHook;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // Настройка темы Material Design
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetBaseTheme(Theme.Dark);
                paletteHelper.SetTheme(theme);

                // Создаём иконку в трее
                CreateTrayIcon();
                
                // Инициализация менеджера закрепления окон
                pinManager = new WindowPinManager();
                pinManager.WindowPinned += OnWindowPinned;
                pinManager.WindowUnpinned += OnWindowUnpinned;

                // Инициализация хука мыши
                mouseHook = new MouseHook(pinManager);
                mouseHook.MouseClick += OnMouseClick;
                
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
                // Применяем кастомный стиль
                contextMenu.Style = (Style)System.Windows.Application.Current.Resources["Win11TrayMenuStyle"];
                contextMenu.MinWidth = 180; // Сделать меню шире
                contextMenu.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left; // Выровнять текст по левому краю

                // Пункт "Показать"
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
                // Добавляем иконку к пункту
                showItem.Icon = new System.Windows.Controls.Image
                {
                    Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        new Icon(iconPath).Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(16, 16)),
                    Width = 16,
                    Height = 16
                };

                // Пункт "Настройки"
                var settingsItem = new System.Windows.Controls.MenuItem { Header = "Настройки" };
                settingsItem.Click += (s, e) => ShowSettings();
                settingsItem.Icon = new System.Windows.Controls.Image
                {
                    Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        new Icon(iconPath).Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(16, 16)),
                    Width = 16,
                    Height = 16
                };

                // Пункт "О программе"
                var aboutItem = new System.Windows.Controls.MenuItem { Header = "О программе" };
                aboutItem.Click += (s, e) =>
                {
                    var aboutWindow = new AboutWindow();
                    aboutWindow.Owner = MainWindow;
                    aboutWindow.ShowDialog();
                };
                aboutItem.Icon = new System.Windows.Controls.Image
                {
                    Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        new Icon(iconPath).Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(16, 16)),
                    Width = 16,
                    Height = 16
                };

                // Пункт "Выход"
                var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
                exitItem.Click += (s, e) => Shutdown();
                exitItem.Icon = new System.Windows.Controls.Image
                {
                    Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        new Icon(iconPath).Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(16, 16)),
                    Width = 16,
                    Height = 16
                };

                // Формируем меню с разделителями
                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(settingsItem);
                contextMenu.Items.Add(aboutItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(exitItem);
                taskbarIcon.ContextMenu = contextMenu;
                taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    // Включаем режим выбора приложения для закрепления
                    if (pinManager != null)
                        pinManager.IsPinMode = true;
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

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        private void TogglePinMode()
        {
            if (pinManager != null)
            {
                pinManager.IsPinMode = !pinManager.IsPinMode;
                var menuItem = taskbarIcon?.ContextMenu?.Items[0] as System.Windows.Controls.MenuItem;
                if (menuItem != null)
                {
                    menuItem.Header = pinManager.IsPinMode ? "Отключить режим закрепления" : "Режим закрепления";
                }
            }
        }

        private void ShowSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void OnMouseClick(object? sender, MouseClickEventArgs e)
        {
            if (pinManager != null)
            {
                pinManager.HandleMouseClick(e.WindowHandle);
            }
        }

        private void OnWindowPinned(object? sender, WindowPinEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // BalloonTip вместо ShowNotification
                taskbarIcon?.ShowBalloonTip("Окно закреплено", $"Окно '{e.WindowTitle}' закреплено поверх других окон", BalloonIcon.Info);
            });
        }

        private void OnWindowUnpinned(object? sender, WindowPinEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // BalloonTip вместо ShowNotification
                taskbarIcon?.ShowBalloonTip("Окно откреплено", $"Окно '{e.WindowTitle}' откреплено", BalloonIcon.Info);
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            taskbarIcon?.Dispose();
            mouseHook?.Dispose();
            pinManager?.Dispose();
            base.OnExit(e);
        }
    }
}
