using System;
using System.Windows;
using MediaColor = System.Windows.Media.Color;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Cobra.UI.Views
{
    public partial class GlobalFooterControl
    {
        public GlobalFooterControl()
        {
            InitializeComponent();
        }

        public void SetPythonStatus(string text, bool isOk = true)
        {
            if (FooterPythonText != null)
            {
                FooterPythonText.Text = text.StartsWith("Python:", StringComparison.OrdinalIgnoreCase) ? text : $"Python: {text}";
                FooterPythonText.Foreground = isOk
                    ? new MediaSolidColorBrush(MediaColor.FromRgb(161, 161, 170))
                    : new MediaSolidColorBrush(MediaColor.FromRgb(248, 113, 113));
            }
        }

        public void SetTerminalStatus(string text, bool isAvailable = true)
        {
            if (FooterTerminalText != null)
            {
                FooterTerminalText.Text = text.StartsWith("Terminal:", StringComparison.OrdinalIgnoreCase) ? text : $"Terminal: {text}";
                FooterTerminalText.Foreground = isAvailable
                    ? new MediaSolidColorBrush(MediaColor.FromRgb(161, 161, 170))
                    : new MediaSolidColorBrush(MediaColor.FromRgb(248, 113, 113));
            }
        }

        public void SetFramework(string frameworkName)
        {
            if (FooterFrameworkText != null && FooterFrameworkSeparator != null)
            {
                if (string.IsNullOrWhiteSpace(frameworkName))
                {
                    FooterFrameworkText.Visibility = Visibility.Collapsed;
                    FooterFrameworkSeparator.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FooterFrameworkText.Text = frameworkName.StartsWith("Framework:", StringComparison.OrdinalIgnoreCase) ? frameworkName : $"Framework: {frameworkName}";
                    FooterFrameworkText.Visibility = Visibility.Visible;
                    FooterFrameworkSeparator.Visibility = Visibility.Visible;
                }
            }
        }

        public void SetGitBranch(string branchName)
        {
            if (FooterGitText != null && FooterGitSeparator != null)
            {
                if (string.IsNullOrWhiteSpace(branchName))
                {
                    FooterGitText.Visibility = Visibility.Collapsed;
                    FooterGitSeparator.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FooterGitText.Text = branchName.StartsWith("Git:", StringComparison.OrdinalIgnoreCase) ? branchName : $"Git: {branchName}";
                    FooterGitText.Visibility = Visibility.Visible;
                    FooterGitSeparator.Visibility = Visibility.Visible;
                }
            }
        }

        public void SetStatus(string text)
        {
            if (FooterStatusText != null)
                FooterStatusText.Text = text;
        }
    }
}
