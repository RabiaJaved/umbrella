using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Umbrella.Resources;

namespace Umbrella
{
    public partial class MainPage : PhoneApplicationPage
    {
        const string LastUpdatedTimeSettingKey = "LastUpdateTime";
        const string LastRainStatusKey = "LastWeatherCode";

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Ask for user's consent to using their location
            GetUserConsentForLocation();

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(LastUpdatedTimeSettingKey))
            {
                IsolatedStorageSettings.ApplicationSettings.Add(LastUpdatedTimeSettingKey, DateTime.MinValue);
            }

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(LastRainStatusKey))
            {
                IsolatedStorageSettings.ApplicationSettings.Add(LastRainStatusKey, RainStatus.Undefined);
            }

            mainTextBlock.Text = "Checking ...";
            statusTextBlock.Text = "";
            statusTextBlock2.Text = "";

            // TODO: Enable this again after we figure out dynamic live tile content
            // EnableBackgroundWeatherCheck();

            // CheckWeatherWrapper();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            CheckWeatherWrapper();
        }

        private static void EnableBackgroundWeatherCheck()
        {
            // TODO: If not first-time launch, simply return

            ShellTileSchedule tileSchedule = new ShellTileSchedule();
            bool TileScheduleRunning = false;

            // Updates will happen on a fixed interval. 
            tileSchedule.Recurrence = UpdateRecurrence.Interval;

            // Updates will happen every hour.  Because MaxUpdateCount is not set, the schedule will run indefinitely.
            tileSchedule.Interval = UpdateInterval.EveryHour;

            // TODO: fix. we need to show image based on actual weather
            tileSchedule.RemoteImageUri = new Uri("http://www.rabiajaved.com/wp-content/mystuff/Umbrella.png");
            tileSchedule.Start();
            TileScheduleRunning = true;
        }

        private void GetUserConsentForLocation()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                // User has opted in for Location
                return;
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show("We need your location to get weather information. Is that ok?",
                    "Location", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                }
                else
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                }

                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        private async void CheckWeatherWrapper()
        {
            await CheckWeather();
        }

        private async Task CheckWeather()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            BitmapImage umbrellaImage = new BitmapImage(new Uri(@"Assets/Umbrella.png", UriKind.Relative));
            BitmapImage noUmbrellaImage = new BitmapImage(new Uri(@"Assets/NoUmbrella.png", UriKind.Relative));
            BitmapImage questionMarkImage = new BitmapImage(new Uri(@"Assets/QuestionMark.png", UriKind.Relative));

            // Early exit for location usage disable
            if ((bool)settings["LocationConsent"] == false)
            {
                image1.Source = questionMarkImage;
                mainTextBlock.Text = "Phone location disabled. Go to settings to re-enable";
                statusTextBlock.Text = string.Empty;
                statusTextBlock2.Text = string.Empty;
                return;
            }

            SystemTray.ProgressIndicator = new ProgressIndicator();
            SystemTray.ProgressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            SystemTray.ProgressIndicator.Text = "Getting weather";

            WeatherData weatherData = await WeatherApiWrapper.GetWeatherForCurrentLocation();
            if (weatherData.RainStatus == RainStatus.NotRaining || weatherData.RainStatus == RainStatus.RainingOrExpected)
            {
                if (!settings.Contains(LastUpdatedTimeSettingKey))
                {
                    settings.Add(LastUpdatedTimeSettingKey, DateTime.Now);
                    settings.Add(LastRainStatusKey, weatherData.RainStatus);
                }
                else
                {
                    settings[LastUpdatedTimeSettingKey] = DateTime.Now;
                    settings[LastRainStatusKey] = weatherData.RainStatus;
                }
            }
            else if (weatherData.RainStatus == RainStatus.CouldNotDetermine && settings.Contains(LastUpdatedTimeSettingKey) && 
                DateTime.Now.Subtract((DateTime)settings[LastUpdatedTimeSettingKey]) < TimeSpan.FromHours(2))
            {
                // If we couldn't get latest data, and stored data is less than 2 hours old, load it from stored settings
                weatherData.RainStatus = (RainStatus) settings[LastRainStatusKey];
            }

            switch (weatherData.RainStatus)
            {
                case RainStatus.RainingOrExpected:
                    image1.Source = umbrellaImage;
                    mainTextBlock.Text = "Take Your Umbrella";
                    break;
                case RainStatus.NotRaining:
                    image1.Source = noUmbrellaImage;
                    mainTextBlock.Text = "No Umbrella Needed";
                    break;
                case RainStatus.CouldNotDetermine:
                    image1.Source = questionMarkImage;
                    mainTextBlock.Text = "Sorry, we're having trouble getting the weather :(";
                    break;
            }

            SystemTray.ProgressIndicator.IsVisible = false;
            SystemTray.ProgressIndicator.IsIndeterminate = false;

            if (weatherData.City != null)
            {
                statusTextBlock.Text = weatherData.City;
            }

            statusTextBlock2.Text = "Last updated " + GetRelativeTime(DateTime.Now.Subtract((DateTime)settings[LastUpdatedTimeSettingKey]));
        }

        private string GetRelativeTime(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.FromMinutes(1))
            {
                return "a few seconds ago";
            }
            if (timeSpan < TimeSpan.FromMinutes(2))
            {
                return "a minute ago";
            }
            if (timeSpan < TimeSpan.FromMinutes(45))
            {
                return timeSpan.Minutes + " minutes ago";
            }
            if (timeSpan < TimeSpan.FromMinutes(90))
            {
                return "an hour ago";
            }
            if (timeSpan < TimeSpan.FromHours(24))
            {
                return timeSpan.Hours + " hours ago";
            }
            if (timeSpan < TimeSpan.FromHours(48))
            {
                return "yesterday";
            }
            if (timeSpan < TimeSpan.FromDays(30))
            {
                return timeSpan.Days + " days ago";
            }
            if (timeSpan < TimeSpan.FromDays(365))
            {
                int months = Convert.ToInt32(Math.Floor((double)timeSpan.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                return "never";
            }
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }
    }
}