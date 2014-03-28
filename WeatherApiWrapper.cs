using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Umbrella
{
    public enum RainStatus
    {
        Undefined,
        CouldNotDetermine,
        LocationDisabled,
        RainingOrExpected,
        NotRaining
    }

    public class WeatherData
    {
        public RainStatus RainStatus = RainStatus.Undefined;
        public string City = null;
    }

    public class WeatherApiWrapper
    {
        static string cityCurrentApiUriFormat = "http://api.openweathermap.org/data/2.5/weather?q={0}";
        static string gpsCurrentApiUrlFormat = "http://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}";
        static string gpsForecastApiUrlFormat = "http://api.openweathermap.org/data/2.5/forecast?lat={0}&lon={1}";

        public async static Task<WeatherData> GetWeatherForCurrentLocation()
        {
            // Get user location from device
            Geoposition geoposition = null;

            try
            {
                Geolocator geolocator = new Geolocator();
                geolocator.DesiredAccuracyInMeters = 10000; // 10 km

                // location should be maximum 30 mins old
                geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(30),
                    timeout: TimeSpan.FromSeconds(20)
                    );
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off
                    return new WeatherData { RainStatus = RainStatus.LocationDisabled };
                }
                
                // something else happened acquring the location
                return new WeatherData { RainStatus = RainStatus.CouldNotDetermine };
            }

            WeatherData retWeatherData = new WeatherData();

            // Location retrieved. Now make HTTP call to Web Service to get weather
            string fullUri = string.Format(gpsForecastApiUrlFormat, geoposition.Coordinate.Latitude, geoposition.Coordinate.Longitude);
            
            // TEST: Warsaw, Poland
            // string fullUri = string.Format(gpsForecastApiUrlFormat, 52.163824, 20.988522);

            HttpClient client = new HttpClient();
            try
            {
                string jsonWeatherText = await client.GetStringAsync(fullUri);

                //dynamic weatherInfo = Newtonsoft.Json.Linq.JObject.Parse(jsonWeatherText);
                
                dynamic weatherInfoForecast = Newtonsoft.Json.Linq.JObject.Parse(jsonWeatherText);

                // By default, assume it's not raining
                retWeatherData.RainStatus = RainStatus.NotRaining;

                // Loop through data for current and next 9 hours (at 3-hour intervals)
                for (int i = 0; i < 3; i++)
                {
                    dynamic weatherInfo = weatherInfoForecast.list[i];

                    int code = weatherInfo.weather[0].id;
                    string city = weatherInfoForecast.city.name;

                    if (city.Length > 0) { retWeatherData.City = city; }
                    if (IsRaining(code)) { retWeatherData.RainStatus = RainStatus.RainingOrExpected; }
                }
            }
            catch (Exception)
            {
                retWeatherData.RainStatus = RainStatus.CouldNotDetermine;
            }

            return retWeatherData;
        }

        private static bool IsRaining(int weatherCode)
        {
            return (weatherCode >= 200 && weatherCode < 700);
            // return true;
        }
    }

}
