using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Cobra.UI.Views
{
    public partial class AnimatedModal
    {
        private object? _result;
        private bool _isConfirmed;

        public AnimatedModal()
        {
            InitializeComponent();
            Loaded += AnimatedModal_Loaded;
        }

        private void AnimatedModal_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        public static async Task<(bool confirmed, object? result)> ShowCustomModalAsync(
            Window? owner,
            string title,
            UIElement content,
            string confirmText = "Create",
            string cancelText = "Cancel")
        {
            var modal = new AnimatedModal
            {
                Owner = owner,
                Title = title,
                TitleText = { Text = title }
            };

            modal.ContentPanel.Children.Clear();
            modal.ContentPanel.Children.Add(content);

            // add buttons
            var confirmButton = new System.Windows.Controls.Button
            {
                Content = confirmText,
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                Foreground = Brushes.White,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            confirmButton.Click += (_, _) => modal.Confirm();

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = cancelText,
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 48)),
                Foreground = Brushes.White,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            cancelButton.Click += (_, _) => modal.Cancel();

            modal.ButtonsPanel.Children.Add(cancelButton);
            modal.ButtonsPanel.Children.Add(confirmButton);

            modal.ShowDialog();
            await Task.Delay(10);
            return (modal._isConfirmed, modal._result);
        }

        public static async Task<string?> ShowInputModalAsync(
            Window? owner,
            string title,
            string message,
            string defaultValue = "")
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock 
            { 
                Text = message, 
                Foreground = Brushes.White, 
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14
            });
            
            var textBox = new TextBox 
            { 
                Text = defaultValue,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Padding = new Thickness(5),
                FontSize = 14,
                CaretBrush = Brushes.White
            };
            stack.Children.Add(textBox);

            // focus the textbox when loaded
            textBox.Loaded += (_, _) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            var (confirmed, _) = await ShowCustomModalAsync(owner, title, stack, "OK");
            return confirmed ? textBox.Text : null;
        }

        private void Confirm()
        {
            _isConfirmed = true;
            _result = TitleText.Text;
            CloseWithAnimation();
        }

        private void Cancel()
        {
            _isConfirmed = false;
            CloseWithAnimation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmed = false;
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
