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
            services.AddSingleton<IFileResolver, FileResolver>();
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<ILogFormatConfigLoader, LogFormatConfigLoader>();
            services.AddSingleton<IConfigPathResolver, ConfigPathResolver>();

            // ViewModels
            services.AddTransient(provider =>
                new MainViewModel(
                    provider.GetRequiredService<ILogFileReader>(),
                    provider.GetRequiredService<IUserDialogService>(),
                    provider.GetRequiredService<ILogFormatConfigLoader>(),
                    provider.GetRequiredService<IFileResolver>(),
                    provider.GetRequiredService<IConfigPathResolver>()));


            // Main Window
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // .NET Core/.NET 5+ で Shift-JIS などのコードページエンコーディングをサポートするために登録
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var configPathResolver = _serviceProvider.GetRequiredService<IConfigPathResolver>();
            var configPath = configPathResolver.ResolvePath(e.Args);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            mainViewModel.Initialize(configPath); // 新しい初期化メソッドを呼び出す
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();
        }
    }
}

