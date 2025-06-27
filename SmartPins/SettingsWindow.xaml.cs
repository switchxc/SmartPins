using System.Windows;

namespace SmartPins
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // Здесь можно добавить инициализацию состояния тумблера и других новых элементов
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
            // Открыть окно для выбора горячей клавиши
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохранить настройки (тема, горячая клавиша)
            this.Close();
        }
    }
}