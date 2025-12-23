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
            services.AddSingleton<ITimeProvider, SystemTimeProvider>();
            services.AddSingleton<ILogFileReader, LogFileReader>();
            services.AddSingleton<IFileResolver, FileResolver>();
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<ISearchWindowService, SearchWindowService>();
            services.AddSingleton<ILogSearchService, LogSearchService>();
            services.AddSingleton<ILogFormatConfigLoader, LogFormatConfigLoader>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<IClipboardService, WpfClipboardService>();
            services.AddSingleton<IConfigPathResolver, ConfigPathResolver>();
            services.AddSingleton<IFilterPresetService, FilterPresetService>();
            services.AddSingleton<IDispatcherService, WpfDispatcherService>();
            services.AddSingleton<ITaskRunner, TaskRunner>();

            // ViewModels
            services.AddTransient(provider =>
                new MainViewModel(
                    provider.GetRequiredService<ILogService>(),
                    provider.GetRequiredService<IUserDialogService>(),
                    provider.GetRequiredService<ISearchWindowService>(),
                    provider.GetRequiredService<ILogSearchService>(),
                    provider.GetRequiredService<IClipboardService>(),
                    provider.GetRequiredService<IConfigPathResolver>(),
                    provider.GetRequiredService<IFilterPresetService>(),
                    provider.GetRequiredService<IDispatcherService>(),
                    provider.GetRequiredService<ITaskRunner>()));


            // Main Window
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // .NET Core/.NET 5+ で Shift-JIS などのコードページエンコーディングをサポートするために登録
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var configPathResolver = _serviceProvider.GetRequiredService<IConfigPathResolver>();
                var configPath = configPathResolver.ResolvePath(e.Args);

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                _ = mainViewModel.Initialize(configPath); // 新しい初期化メソッドを呼び出す
                mainWindow.DataContext = mainViewModel;
                mainWindow.Show();
            }
            catch (System.Exception ex)
            {
                ShowErrorWindow(ex);
                Shutdown();
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowErrorWindow(e.Exception);
            e.Handled = true;
        }

        private void ShowErrorWindow(System.Exception ex)
        {
            var errorMessage = $"Message: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }

            var errorWindow = new ErrorWindow(errorMessage);
            errorWindow.ShowDialog();
        }
    }
}

