using Microsoft.Extensions.DependencyInjection;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System.Windows;

namespace MultiLogViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<ILogFileReader, LogFileReader>();
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<ILogFormatConfigLoader, LogFormatConfigLoader>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Main Window
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetService<MainViewModel>();
            mainWindow.Show();
        }
    }
}

