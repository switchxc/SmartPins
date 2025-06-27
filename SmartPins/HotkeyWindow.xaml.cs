using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace SmartPins
{
    public partial class HotkeyWindow : Window
    {
        private readonly HashSet<Key> pressedKeys = new();
        private readonly HashSet<ModifierKeys> pressedModifiers = new();
        private bool isListening = false;
        
        public string SelectedHotkey { get; private set; } = "";

        public HotkeyWindow()
        {
            InitializeComponent();
            Focusable = true;
            KeyDown += HotkeyWindow_KeyDown;
            KeyUp += HotkeyWindow_KeyUp;
            Loaded += (s, e) => StartListening();
            
            // Устанавливаем обработчики для кнопок управления окном
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private void StartListening()
        {
            isListening = true;
            Focus();
            UpdateHotkeyDisplay();
        }

        private void StopListening()
        {
            isListening = false;
        }

        private void HotkeyWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isListening) return;
            
            e.Handled = true;

            // Добавляем модификаторы
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                pressedModifiers.Add(ModifierKeys.Control);
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                pressedModifiers.Add(ModifierKeys.Alt);
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                pressedModifiers.Add(ModifierKeys.Shift);
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                pressedModifiers.Add(ModifierKeys.Windows);

            // Добавляем основную клавишу (если это не модификатор)
            if (!IsModifierKey(e.Key))
            {
                pressedKeys.Clear(); // Очищаем предыдущие клавиши
                pressedKeys.Add(e.Key);
            }

            UpdateHotkeyDisplay();
        }

        private void HotkeyWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (!isListening) return;
            
            e.Handled = true;

            // Удаляем модификаторы
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                pressedModifiers.Remove(ModifierKeys.Control);
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
                pressedModifiers.Remove(ModifierKeys.Alt);
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                pressedModifiers.Remove(ModifierKeys.Shift);
            if (e.Key == Key.LWin || e.Key == Key.RWin)
                pressedModifiers.Remove(ModifierKeys.Windows);

            // Удаляем основную клавишу
            pressedKeys.Remove(e.Key);

            UpdateHotkeyDisplay();
        }

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin;
        }

        private void UpdateHotkeyDisplay()
        {
            var modifiers = new List<string>();
            
            if (pressedModifiers.Contains(ModifierKeys.Control))
                modifiers.Add("Ctrl");
            if (pressedModifiers.Contains(ModifierKeys.Alt))
                modifiers.Add("Alt");
            if (pressedModifiers.Contains(ModifierKeys.Shift))
                modifiers.Add("Shift");
            if (pressedModifiers.Contains(ModifierKeys.Windows))
                modifiers.Add("Win");

            var mainKey = pressedKeys.FirstOrDefault();
            var mainKeyText = mainKey != Key.None ? GetKeyDisplayName(mainKey) : "";

            if (modifiers.Count > 0 && !string.IsNullOrEmpty(mainKeyText))
            {
                var hotkeyText = string.Join("+", modifiers) + "+" + mainKeyText;
                HotkeyDisplay.Text = hotkeyText;
                HotkeyDisplay.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Win11TextPrimary
                SelectedHotkey = hotkeyText;
            }
            else if (modifiers.Count > 0)
            {
                HotkeyDisplay.Text = string.Join("+", modifiers) + "+...";
                HotkeyDisplay.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)); // Win11TextSecondary
            }
            else
            {
                HotkeyDisplay.Text = "Нажмите клавиши...";
                HotkeyDisplay.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)); // Win11TextSecondary
            }
        }

        private string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                Key.A => "A", Key.B => "B", Key.C => "C", Key.D => "D", Key.E => "E",
                Key.F => "F", Key.G => "G", Key.H => "H", Key.I => "I", Key.J => "J",
                Key.K => "K", Key.L => "L", Key.M => "M", Key.N => "N", Key.O => "O",
                Key.P => "P", Key.Q => "Q", Key.R => "R", Key.S => "S", Key.T => "T",
                Key.U => "U", Key.V => "V", Key.W => "W", Key.X => "X", Key.Y => "Y",
                Key.Z => "Z",
                Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
                Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
                Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
                Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
                Key.F9 => "F9", Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",
                Key.Space => "Space", Key.Enter => "Enter", Key.Escape => "Escape",
                Key.Tab => "Tab", Key.Back => "Backspace", Key.Delete => "Delete",
                Key.Insert => "Insert", Key.Home => "Home", Key.End => "End",
                Key.PageUp => "PageUp", Key.PageDown => "PageDown",
                Key.Up => "↑", Key.Down => "↓", Key.Left => "←", Key.Right => "→",
                Key.NumPad0 => "Num0", Key.NumPad1 => "Num1", Key.NumPad2 => "Num2",
                Key.NumPad3 => "Num3", Key.NumPad4 => "Num4", Key.NumPad5 => "Num5",
                Key.NumPad6 => "Num6", Key.NumPad7 => "Num7", Key.NumPad8 => "Num8",
                Key.NumPad9 => "Num9",
                Key.Add => "+", Key.Subtract => "-", Key.Multiply => "*", Key.Divide => "/",
                Key.OemPlus => "+", Key.OemMinus => "-", Key.OemComma => ",", Key.OemPeriod => ".",
                Key.OemQuestion => "?", Key.OemTilde => "~", Key.OemOpenBrackets => "[", Key.OemCloseBrackets => "]",
                Key.OemPipe => "|", Key.OemQuotes => "'", Key.OemSemicolon => ";", Key.OemBackslash => "\\",
                _ => key.ToString()
            };
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            pressedKeys.Clear();
            pressedModifiers.Clear();
            SelectedHotkey = "";
            UpdateHotkeyDisplay();
            StartListening();
        }

        private void PresetHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string hotkey)
            {
                // Парсим предустановленную комбинацию
                ParseHotkeyString(hotkey);
                UpdateHotkeyDisplay();
            }
        }

        private void ParseHotkeyString(string hotkeyString)
        {
            pressedKeys.Clear();
            pressedModifiers.Clear();

            var parts = hotkeyString.Split('+');
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                
                switch (trimmedPart.ToUpper())
                {
                    case "CTRL":
                        pressedModifiers.Add(ModifierKeys.Control);
                        break;
                    case "ALT":
                        pressedModifiers.Add(ModifierKeys.Alt);
                        break;
                    case "SHIFT":
                        pressedModifiers.Add(ModifierKeys.Shift);
                        break;
                    case "WIN":
                        pressedModifiers.Add(ModifierKeys.Windows);
                        break;
                    default:
                        // Пытаемся найти клавишу
                        if (Enum.TryParse<Key>(trimmedPart, true, out var key))
                        {
                            pressedKeys.Add(key);
                        }
                        break;
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SelectedHotkey))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите комбинацию клавиш или нажмите 'Отмена'.", 
                               "Не выбрана комбинация", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            // Останавливаем прослушивание при потере фокуса
            StopListening();
            base.OnDeactivated(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            // Возобновляем прослушивание при получении фокуса
            StartListening();
            base.OnActivated(e);
        }
    }
} 