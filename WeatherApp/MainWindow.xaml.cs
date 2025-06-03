using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums; // สำหรับ IUIFactory
using NetSparkleUpdater.SignatureVerifiers; // สำหรับ Digital Signature

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private static string _apiKey => ConfigurationManager.AppSettings["OpenWeatherMapApiKey"];
        private static string _baseUrl => ConfigurationManager.AppSettings["OpenWeatherMapBaseUrl"];

        // *** เปลี่ยนตรงนี้สำหรับ NetSparkleUpdater และ GitHub ***
        // URL ของ appcast.xml ที่โฮสต์บน GitHub (เช่น Raw Content หรือ GitHub Pages)
        private static string _appcastUrl => ConfigurationManager.AppSettings["UpdateAppcastUrl"];
        private static string _publicDSAKeyPath = ConfigurationManager.AppSettings["PublicDSAKeyPath"];
        

        private SparkleUpdater _sparkleUpdater;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize NetSparkleUpdater
            InitializeSparkleUpdater();
        }

        private void InitializeSparkleUpdater()
        {
            try
            {
                Version currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);

                string publicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _publicDSAKeyPath);
                if (!File.Exists(publicKeyPath))
                {
                    MessageBox.Show($"Public DSA key file not found at: {publicKeyPath}. Updates cannot be securely verified.", "Security Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                string publicKey = File.ReadAllText(publicKeyPath);
               
                _sparkleUpdater = new SparkleUpdater(
                            _appcastUrl, // link to your app cast file - change extension to .json if using json
                            new Ed25519Checker(SecurityMode.Strict, // security mode -- use .Unsafe to ignore all signature checking (NOT recommended!!)
                                               publicKey) // your base 64 public key
                )
                {
                    UIFactory = new NetSparkleUpdater.UI.WPF.UIFactory(), // or null, or choose some other UI factory, or build your own IUIFactory implementation!
                    RelaunchAfterUpdate = false, // set to true if needed
                };
                _sparkleUpdater.StartLoop(true); // will auto-check for updates
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing update system: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    string url = $"{_baseUrl}?q={city}&appid={_apiKey}&units=metric";

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

        private async void btnCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (_sparkleUpdater == null)
            {
                MessageBox.Show("Update system not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _sparkleUpdater.CheckForUpdatesQuietly();
        }

    }
}