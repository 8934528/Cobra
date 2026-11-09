using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;

using MediaColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;

namespace Cobra.UI.Views
{
    public enum DialogIcon
    {
        Info,
        Success,
        Warning,
        Error,
        Question
    }

    public partial class CustomDialog
    {
        private bool _result;

        public CustomDialog()
        {
            InitializeComponent();
            Loaded += CustomDialog_Loaded;
        }

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        public static async Task<bool> ShowAsync(
            Window? owner,
            string message,
            string title = "Message",
            DialogIcon icon = DialogIcon.Info,
            string button1Text = "OK",
            string? button2Text = null)
        {
            var dialog = new CustomDialog
            {
                Owner = owner,
                Title = title,
                TitleText = { Text = title },
                MessageText = { Text = message },
                Button1 = { Content = button1Text }
            };

            // set icon based on type using packiconmaterialkind
            if (dialog.TitleIcon != null)
            {
                switch (icon)
                {
                    case DialogIcon.Info:
                        dialog.TitleIcon.Kind = PackIconMaterialKind.Information;
                        dialog.TitleIcon.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 140, 0));
                        break;
                    case DialogIcon.Success:
                        dialog.TitleIcon.Kind = PackIconMaterialKind.CheckCircle;
                        dialog.TitleIcon.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 140, 0));
                        break;
                    case DialogIcon.Warning:
                        dialog.TitleIcon.Kind = PackIconMaterialKind.Alert;
                        dialog.TitleIcon.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 140, 0));
                        break;
                    case DialogIcon.Error:
                        dialog.TitleIcon.Kind = PackIconMaterialKind.CloseCircle;
                        dialog.TitleIcon.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 140, 0));
                        break;
                    case DialogIcon.Question:
                        dialog.TitleIcon.Kind = PackIconMaterialKind.HelpCircle;
                        dialog.TitleIcon.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 140, 0));
                        break;
                }
            }

            if (!string.IsNullOrEmpty(button2Text))
            {
                dialog.Button2.Content = button2Text;
                dialog.Button2.Visibility = Visibility.Visible;
            }

            dialog.ShowDialog();

            // wait for result
            await Task.Delay(10);
            return dialog._result;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            _result = button == Button1;
            CloseWithAnimation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _result = false;
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
