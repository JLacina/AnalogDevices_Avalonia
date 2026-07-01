using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Services;
using HardwareDeviceConfigManager.ViewModels;
using HardwareDeviceConfigManager.Views;

namespace HardwareDeviceConfigManager;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Simple manual DI composition root — no DI container needed for this scale.
        // In a larger app, use Microsoft.Extensions.DependencyInjection.
        IDeviceRepository repository     = new InMemoryDeviceRepository();
        IHardwareAdapter  hardwareAdapter = new SimulatedHardwareAdapter();
        var viewModel = new MainWindowViewModel(repository, hardwareAdapter);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
