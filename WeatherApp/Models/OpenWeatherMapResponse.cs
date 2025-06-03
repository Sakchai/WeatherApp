namespace WeatherApp
{
    public partial class MainWindow
    {
        public class OpenWeatherMapResponse
        {
            public string name { get; set; }
            public Sys sys { get; set; }
            public Main main { get; set; }
            public Weather[] weather { get; set; }
            public Wind wind { get; set; }
        }
    }
}