using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using RunAllHTTPFileRequests.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RunAllHTTPFileRequests
{
    /// <summary>
    /// Adornment class that draws a square box in the top right-hand corner of the viewport
    /// only for .http files.
    /// </summary>
    internal sealed class RunAllAdornment
    {
        private const double ButtonWidth = 150;
        private const double ButtonHeight = 30;
        private const double TopMargin = 30;
        private const double RightMargin = 30;

        private readonly IWpfTextView view;
        private readonly Button button;
        private readonly IAdornmentLayer adornmentLayer;

        private readonly HttpRequestService httpRequestService;

        public RunAllAdornment(IWpfTextView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.adornmentLayer = view.GetAdornmentLayer("RunAllAdornment");

            this.httpRequestService = new HttpRequestService();  // Initializes the HTTP request service


            this.button = CreateButton();
            SetInitialButtonVisibility();

            // Subscribing to layout changes to reposition the button and to text changes to recheck visibility
            view.LayoutChanged += OnSizeChanged;
            view.Closed += OnViewClosed; // Ensure to unsubscribe events on view close to prevent memory leaks
        }

        private Button CreateButton()
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Images/play-button.png")),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var textBlock = new TextBlock
            {
                Text = "Run All Requests",
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { image, textBlock }
            };

            var button = new Button
            {
                Content = stackPanel,
                Width = ButtonWidth,
                Height = ButtonHeight,
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38)),
                Foreground = new SolidColorBrush(Colors.White),
                Cursor = Cursors.Hand,
                Style = (Style)Application.Current.FindResource(ToolBar.ButtonStyleKey)
            };

            button.MouseEnter += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(255, 62, 62, 64));
            button.MouseLeave += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38));
            button.Click += Button_Click;

            return button;
        }

        private void SetInitialButtonVisibility()
        {
            UpdateButtonVisibility(GetFilePath());
        }

        private void UpdateButtonVisibility(string filePath)
        {
            button.Visibility = filePath != null && filePath.EndsWith(".http", StringComparison.OrdinalIgnoreCase)
                                ? Visibility.Visible : Visibility.Collapsed;

            if (button.Visibility == Visibility.Visible)
            {
                adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, button, null);
            }
            else
            {
                adornmentLayer.RemoveAllAdornments();
            }
        }

        private string GetFilePath()
        {
            return view.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) ? document.FilePath : null;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string filePath = GetFilePath();
            if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".http", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This is not an HTTP file.");
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(filePath);
                var results = await httpRequestService.ExecuteHttpRequests(fileContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute HTTP requests: {ex.Message}");
            }
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            if (button.Visibility == Visibility.Visible)
            {
                Canvas.SetLeft(button, view.ViewportRight - RightMargin - ButtonWidth);
                Canvas.SetTop(button, view.ViewportTop + TopMargin);
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            view.LayoutChanged -= OnSizeChanged;
            view.Closed -= OnViewClosed;
        }
    }
}
