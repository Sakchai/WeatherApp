using Squirrel;
using System.Windows;
using System.Threading.Tasks; // เพิ่ม using สำหรับ Task

namespace WeatherApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}