using System; // สำหรับ Path, File
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection; // สำหรับ AssemblyVersion
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Squirrel; // เพิ่ม using สำหรับ Squirrel.Windows

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private static string ApiKey => ConfigurationManager.AppSettings["OpenWeatherMapApiKey"];
        private static string BaseUrl => ConfigurationManager.AppSettings["OpenWeatherMapBaseUrl"];
        // Squirrel จะค้นหาไฟล์ RELEASES ที่อยู่ใน Root ของ URL นี้
        private static string UpdateReleasesUrl => ConfigurationManager.AppSettings["UpdateReleasesUrl"];

        public MainWindow()
        {
            InitializeComponent();
            DisplayCurrentVersion();
        }

        private async void btnGetWeather_Click(object sender, RoutedEventArgs e)
        {
            string city = txtCity.Text;
            if (string.IsNullOrWhiteSpace(city))
            {
                MessageBox.Show("Please enter a city.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await GetWeatherData(city);
        }

        private async Task GetWeatherData(string city)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"{BaseUrl}?q={city}&appid={ApiKey}&units=metric";

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var weatherData = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>();

                    if (weatherData != null)
                    {
                        lblLocation.Text = $"{weatherData.name}, {weatherData.sys.country}";
                        lblTemperature.Text = $"{weatherData.main.temp}°C";
                        lblCondition.Text = weatherData.weather[0].description;
                        lblHumidity.Text = $"Humidity: {weatherData.main.humidity}%";
                        lblWindSpeed.Text = $"Wind Speed: {weatherData.wind.speed} m/s";

                        if (weatherData.weather[0].icon != null)
                        {
                            string iconUrl = $"https://openweathermap.org/img/wn/{weatherData.weather[0].icon}@2x.png";
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new System.Uri(iconUrl);
                            bitmap.EndInit();
                            imgWeatherIcon.Source = bitmap;
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Error fetching weather data: {httpEx.Message}. Please check city name or API Key.", "API Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayCurrentVersion()
        {
            Version version = Assembly.GetEntryAssembly()?.GetName().Version;
            if (version != null)
            {
                lblCurrentVersion.Text = $"App Version: {version}";
            }
        }

        private async void btnCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates();
        }

        // *** ส่วนที่ปรับปรุงสำหรับ Squirrel.Windows ***
        private async Task CheckForUpdates()
        {
            try
            {
                using (var mgr = new UpdateManager(UpdateReleasesUrl))
                {
                    UpdateInfo updateInfo = await mgr.CheckForUpdate();

                    if (updateInfo != null)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"New version {updateInfo.FutureReleaseEntry.Version} available! Current version is {updateInfo.CurrentlyInstalledVersion}.\n\nDo you want to download and install the update?",
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            MessageBox.Show("Downloading update...", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                            await mgr.DownloadReleases(updateInfo.ReleasesToApply);

                            MessageBox.Show("Installing update...", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                            await mgr.ApplyReleases(updateInfo); //.ApplyUpdates

                            MessageBox.Show("Update installed. Restarting application...", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                            UpdateManager.RestartApp(); // รีสตาร์ทแอปพลิเคชันเพื่อให้การอัปเดตมีผล
                        }
                    }
                    else
                    {
                        MessageBox.Show("You are running the latest version.", "No Update", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An error occurred during update check: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class OpenWeatherMapResponse
        {
            public string name { get; set; }
            public Sys sys { get; set; }
            public Main main { get; set; }
            public Weather[] weather { get; set; }
            public Wind wind { get; set; }
        }

        public class Sys
        {
            public string country { get; set; }
        }

        public class Main
        {
            public double temp { get; set; }
            public int humidity { get; set; }
        }

        public class Weather
        {
            public string description { get; set; }
            public string icon { get; set; }
        }

        public class Wind
        {
            public double speed { get; set; }
        }
    }
}