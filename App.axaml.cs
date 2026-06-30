using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Services;
using HardwareDeviceConfigManager.ViewModels;
using HardwareDeviceConfigManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareDeviceConfigManager;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider = ConfigureServices();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };

            desktop.Exit += (_, _) => _serviceProvider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
        services.AddSingleton<IHardwareAdapter, SimulatedHardwareAdapter>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
