using System.Windows;

namespace SmartPins
{
    public partial class SettingsWindow : Window
    {
        public bool HighlightOnlyPinned { get; private set; }
        private string? SelectedHotkey;

        public string? SelectedHotkeyValue => SelectedHotkey;

        public SettingsWindow(bool highlightOnlyPinned = false)
        {
            InitializeComponent();
            HighlightOnlyPinnedToggle.IsChecked = highlightOnlyPinned;
            HighlightOnlyPinned = highlightOnlyPinned;
            HighlightOnlyPinnedToggle.Checked += HighlightOnlyPinnedToggle_Changed;
            HighlightOnlyPinnedToggle.Unchecked += HighlightOnlyPinnedToggle_Changed;
        }

        private void HighlightOnlyPinnedToggle_Changed(object sender, RoutedEventArgs e)
        {
            HighlightOnlyPinned = HighlightOnlyPinnedToggle.IsChecked == true;
            // Мгновенно применяем настройку в главном окне
            if (Owner is MainWindow mainWindow)
            {
                mainWindow.SetHighlightOnlyPinned(HighlightOnlyPinned);
            }
        }

        private void DarkThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Включить тёмную тему
        }

        private void DarkThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Включить светлую тему
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            var hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.Owner = this;
            if (hotkeyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(hotkeyWindow.SelectedHotkey))
                {
                    SelectedHotkey = hotkeyWindow.SelectedHotkey;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}