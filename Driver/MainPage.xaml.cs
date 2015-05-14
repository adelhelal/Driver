
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Driver
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            DriveStoryboard.Begin();
            ApproachStoryboard.Completed += ApproachStoryboard_Completed;
        }

        private void TreeButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            TreeImage.Visibility = Visibility.Visible;
            ApproachStoryboard.Begin();
        }

        private void RockButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RockImage.Visibility = Visibility.Visible;
            ApproachStoryboard.Begin();
        }

        private void ApproachStoryboard_Completed(object sender, object e)
        {
            TreeImage.Visibility = Visibility.Collapsed;
            RockImage.Visibility = Visibility.Collapsed;
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            var transform = (CompositeTransform)CarImage.RenderTransform;
            transform.TranslateX = 0;
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            var transform = (CompositeTransform)CarImage.RenderTransform;
            transform.TranslateX = 225;
        }
    }
}
