
using System;
using System.Threading.Tasks;
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
        public bool Approaching { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            ApproachStoryboard.Completed += ApproachStoryboard_Completed;

            DriveStoryboard.Begin();
        }

        private void TreeButton_Click(object sender, RoutedEventArgs e)
        {
            ApproachStoryboard_Beginning(TreeImage);
        }

        private void RockButton_Click(object sender, RoutedEventArgs e)
        {
            ApproachStoryboard_Beginning(RockImage);
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            ((CompositeTransform)CarImage.RenderTransform).TranslateX = 0;
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            ((CompositeTransform)CarImage.RenderTransform).TranslateX = 225;
        }

        private void SpeedDownButton_Click(object sender, RoutedEventArgs e)
        {
            Speed(2);
        }

        private void SpeedUpButton_Click(object sender, RoutedEventArgs e)
        {
            Speed(0.5);
        }

        async private void ApproachStoryboard_Completed(object sender, object e)
        {
            var carPosition = ((CompositeTransform)CarImage.RenderTransform).TranslateX;

            if (
                (TreeImage.Visibility == Visibility.Visible && carPosition == 0) ||
                (RockImage.Visibility == Visibility.Visible && carPosition > 0)
            )
            {
                Collision(carPosition);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            TreeImage.Visibility = Visibility.Collapsed;
            RockImage.Visibility = Visibility.Collapsed;
            CrashImage.Visibility = Visibility.Collapsed;
            this.Approaching = false;
        }

        private void ApproachStoryboard_Beginning(Image image)
        {
            if (!this.Approaching)
            {
                image.Visibility = Visibility.Visible;
                ApproachStoryboard.Begin();
                this.Approaching = true;
            }
        }

        private void Speed(double speed)
        {
            foreach (var animation in ApproachStoryboard.Children)
            {
                animation.Duration = new Duration(TimeSpan.FromSeconds(speed));
            }
        }

        private void Collision(double carPosition)
        {
            var transform = (CompositeTransform)CrashImage.RenderTransform;
            transform.TranslateX = carPosition;
            CrashImage.Visibility = Visibility.Visible;
        }
    }
}
