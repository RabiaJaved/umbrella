using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace Umbrella
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This app uses your location to anonymously retrieve weather information from online services. You can choose to turn off location tracking; however, the application will cease to function correctly");
        }

        private void locationToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
        }

        private void locationToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            locationToggleSwitch.IsChecked = (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"];
        }
    }
}