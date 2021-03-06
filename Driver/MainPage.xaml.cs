﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.HumanInterfaceDevice;
using Windows.System;
using Windows.UI.Core;
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

        public ControllerDirection ControllerDirection { get; set; }

        //public static bool FoundLocalControlsWorking = false;

        private static XboxHidController controller;
        private static int lastControllerCount = 0;

        private int LEDStatus = 0;
        private const int LED_PIN = 47;
        private GpioPin pin;
        private DispatcherTimer timer;
        private SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.LawnGreen);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        private int answer1Count = 0;
        private int answer1ThisCount = 0;
        private int answer2Count = 0;
        private int answer2ThisCount = 0;

        public MainPage()
        {
            this.InitializeComponent();
            ApproachStoryboard.Completed += ApproachStoryboard_Completed;

            DriveStoryboard.Begin();

            this.XboxJoystickInit();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(5000);
            timer.Tick += Timer_Tick;
            timer.Start();

            //Unloaded += MainPage_Unloaded;

            InitGPIO();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
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

            foreach (var animation in DriveStoryboard.Children)
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

        public async void XboxJoystickInit()
        {
            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);

            if (deviceInformationCollection.Count == 0)
            {
                Debug.WriteLine("No Xbox360 controller found!");
            }
            lastControllerCount = deviceInformationCollection.Count;

            foreach (DeviceInformation d in deviceInformationCollection)
            {
                Debug.WriteLine("Device ID: " + d.Id);

                HidDevice hidDevice = await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read);

                if (hidDevice == null)
                {
                    try
                    {
                        var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;

                        if (!deviceAccessStatus.Equals(DeviceAccessStatus.Allowed))
                        {
                            Debug.WriteLine("DeviceAccess: " + deviceAccessStatus.ToString());
                            //FoundLocalControlsWorking = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Xbox init - " + e.Message);
                    }

                    Debug.WriteLine("Failed to connect to the controller!");
                }

                controller = new XboxHidController(hidDevice);
                controller.DirectionChanged += Controller_DirectionChanged;
            }
        }
        private async void Controller_DirectionChanged(ControllerVector sender)
        {
            //Debug.WriteLine("Direction: " + sender.Direction + ", Magnitude: " + sender.Magnitude);

            if (sender.Direction == this.ControllerDirection)
            {
                return;
            }
            this.ControllerDirection = sender.Direction;

            //FoundLocalControlsWorking = true;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (sender.Direction)
                {
                    case ControllerDirection.Up:
                        Speed(1);
                        break;
                    case ControllerDirection.Right:
                        ((CompositeTransform)CarImage.RenderTransform).TranslateX = 400;
                        break;
                    case ControllerDirection.Down:
                        Speed(3);
                        break;
                    case ControllerDirection.Left:
                        ((CompositeTransform)CarImage.RenderTransform).TranslateX = 0;
                        break;
                    default:
                        break;
                }
            });
        }

        private void InitGPIO()
        {
            GpioController gpio = null;

            try
            {
                gpio = GpioController.GetDefault();
            }
            catch (Exception ex) { }

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                Debug.WriteLine("There is no GPIO controller on this device.");
                return;
            }

            pin = gpio.OpenPin(LED_PIN);

            // Show an error if the pin wasn't initialized properly
            if (pin == null)
            {
                Debug.WriteLine("There were problems initializing the GPIO pin.");
                return;
            }

            pin.Write(GpioPinValue.High);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            Debug.WriteLine("GPIO pin initialized correctly.");
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            pin.Dispose();
        }

        private void FlipLED()
        {
            if (LEDStatus == 0)
            {
                LEDStatus = 1;
                if (pin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    pin.Write(GpioPinValue.Low);
                }
                LED.Fill = greenBrush;
            }
            else
            {
                LEDStatus = 0;
                if (pin != null)
                {
                    pin.Write(GpioPinValue.High);
                }
                LED.Fill = grayBrush;
            }
        }

        private void TurnOffLED()
        {
            if (LEDStatus == 1)
            {
                FlipLED();
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FlipLED();
            });

            var httpClient = new HttpClient();
            var voteServiceResponseAsString = await httpClient.GetStringAsync("http://votes.api.ninemsn.com.au/network/questions/302121B3-9A01-4F4C-B891-FC182F13A16C");
            //await Task.Delay(TimeSpan.FromSeconds(2));

            var definition = new[] { new { answers = new[] { new { id = string.Empty, count = string.Empty, percent = string.Empty, percentMax = string.Empty } } } };
            var voteServiceResponse = definition;
            voteServiceResponse = JsonConvert.DeserializeAnonymousType(voteServiceResponseAsString, definition);

            var answer1NewCount = 0;
            var answer2NewCount = 0;
            if (voteServiceResponse != null && voteServiceResponse.Length > 0)
            {
                if (voteServiceResponse[0].answers != null && voteServiceResponse[0].answers.Length > 0)
                {
                    if (voteServiceResponse[0].answers[0] != null)
                    {
                        int.TryParse(voteServiceResponse[0].answers[0].count, out answer1NewCount);
                    }
                }
                if (voteServiceResponse[0].answers != null && voteServiceResponse[0].answers.Length > 0)
                {
                    if (voteServiceResponse[0].answers[1] != null)
                    {
                        int.TryParse(voteServiceResponse[0].answers[1].count, out answer2NewCount);
                    }
                }
            }

            if (answer1Count > 0)
                answer1ThisCount = answer1NewCount - answer1Count;
            answer1Count = answer1NewCount;
            if (answer2Count > 0)
                answer2ThisCount = answer2NewCount - answer2Count;
            answer2Count = answer2NewCount;

            if(answer1ThisCount + answer2ThisCount > 0)
            {
                if (answer1ThisCount > answer2ThisCount)
                {
                    ApproachStoryboard_Beginning(RockImage);
                }
                else
                {
                    ApproachStoryboard_Beginning(TreeImage);
                }
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FlipLED();
            });
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Up:
                    Speed(1);
                    break;
                case VirtualKey.Right:
                    ((CompositeTransform)CarImage.RenderTransform).TranslateX = 400;
                    break;
                case VirtualKey.Down:
                    Speed(3);
                    break;
                case VirtualKey.Left:
                    ((CompositeTransform)CarImage.RenderTransform).TranslateX = 0;
                    break;
                default:
                    break;
            }
        }
    }
}