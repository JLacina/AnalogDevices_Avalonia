# HardwareDeviceConfigManager

An Avalonia .NET 8 desktop application for managing hardware device configurations — built as interview prep for a **Senior .NET Desktop Developer** role (C#, MVVM, Avalonia, hardware configuration, USB/API/HAL concepts, testing).

---

## How to Run

Requires [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

```bash
# From the repository root:
dotnet run
```

To build without running:

```bash
dotnet build
```

---

## Project Structure

```
HardwareDeviceConfigManager.csproj   ← Project file (Avalonia + CommunityToolkit.Mvvm)
Program.cs                           ← Entry point, AppBuilder configuration
App.axaml / App.axaml.cs            ← Application root, DI composition root
Models/
  DeviceModel.cs                     ← POCO data model (Id, Name, Type, Status, etc.)
ViewModels/
  MainWindowViewModel.cs             ← All UI state, commands, and business logic (MVVM)
Views/
  MainWindow.axaml                   ← Two-panel UI: device list + edit form
  MainWindow.axaml.cs                ← Code-behind (nearly empty — MVVM keeps logic in ViewModel)
Services/
  IDeviceRepository.cs               ← Repository interface (Get/Add/Update/Delete)
  InMemoryDeviceRepository.cs        ← In-memory implementation seeded with sample data
Hardware/
  IHardwareAdapter.cs                ← Hardware Abstraction Layer (HAL) interface
  SimulatedHardwareAdapter.cs        ← Simulated adapter (mimics USB/API latency with Task.Delay)
```

---

## MVVM Architecture

```
┌─────────────────────────────────────────────────────────┐
│                        View                             │
│   MainWindow.axaml — pure XAML, no logic                │
│   Binds to ViewModel properties and commands            │
└──────────────────────┬──────────────────────────────────┘
                       │  DataContext (TwoWay Bindings)
┌──────────────────────▼──────────────────────────────────┐
│                    ViewModel                            │
│   MainWindowViewModel.cs                                │
│   • ObservableCollection<DeviceModel> Devices           │
│   • Form field properties (FormDeviceName, etc.)        │
│   • Commands: AddDevice, UpdateDevice, DeleteDevice,    │
│               ClearForm, SimulateConnect, SimulateDisconnect │
│   • Depends on IDeviceRepository + IHardwareAdapter     │
└───────────┬──────────────────────────┬──────────────────┘
            │                          │
┌───────────▼────────┐    ┌────────────▼────────────────┐
│      Service       │    │        Hardware (HAL)        │
│  IDeviceRepository │    │     IHardwareAdapter        │
│  (data storage)    │    │   (connect/disconnect)      │
└────────────────────┘    └─────────────────────────────┘
            │                          │
┌───────────▼────────┐    ┌────────────▼────────────────┐
│InMemoryDeviceRepo  │    │  SimulatedHardwareAdapter   │
│(seeded sample data)│    │  (Task.Delay simulation)    │
└────────────────────┘    └─────────────────────────────┘
```

**Why MVVM?**
- The View is declarative XAML — no code in code-behind.
- The ViewModel holds all state and commands — fully unit-testable without a UI.
- The Model is a plain C# class — no framework dependencies.

---

## Hardware Abstraction Layer (HAL)

`IHardwareAdapter` abstracts all hardware communication behind two methods:

```csharp
Task<DeviceStatus> ConnectAsync(DeviceModel device);
Task<DeviceStatus> DisconnectAsync(DeviceModel device);
```

The ViewModel calls these methods without knowing the underlying transport. To swap in real hardware:

| Adapter Class              | Transport                              |
|---------------------------|----------------------------------------|
| `SimulatedHardwareAdapter` | `Task.Delay` — no hardware needed     |
| `UsbHardwareAdapter`       | LibUsbDotNet / HidSharp               |
| `VisaHardwareAdapter`      | NI-VISA / Keysight VISA (GPIB, LAN)   |
| `RestApiHardwareAdapter`   | `HttpClient` → device REST API        |

To use a real USB adapter, implement `IHardwareAdapter` and change one line in `App.axaml.cs`:

```csharp
// Before:
IHardwareAdapter hardwareAdapter = new SimulatedHardwareAdapter();

// After:
IHardwareAdapter hardwareAdapter = new UsbHardwareAdapter();
```

---

## Testing Strategy

The ViewModel is fully testable without a UI or physical hardware:

```csharp
// Arrange
var mockRepo    = new Mock<IDeviceRepository>();
var mockAdapter = new Mock<IHardwareAdapter>();
mockRepo.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
mockAdapter.Setup(a => a.ConnectAsync(It.IsAny<DeviceModel>()))
           .ReturnsAsync(DeviceStatus.Connected);

var vm = new MainWindowViewModel(mockRepo.Object, mockAdapter.Object);

// Act
vm.FormDeviceName      = "Test Device";
vm.FormFirmwareVersion = "1.0.0";
vm.AddDeviceCommand.Execute(null);

// Assert
mockRepo.Verify(r => r.Add(It.Is<DeviceModel>(d => d.DeviceName == "Test Device")), Times.Once);
```

Key test scenarios:
- `AddDeviceCommand` — validation, calls `IDeviceRepository.Add`
- `UpdateDeviceCommand` — requires selected device, calls `IDeviceRepository.Update`
- `DeleteDeviceCommand` — removes device, clears form
- `SimulateConnectCommand` — sets `IsBusy`, calls `IHardwareAdapter.ConnectAsync`, updates status
- Validation — empty DeviceName or FirmwareVersion sets `StatusMessage`

---

## Dependency Injection

The composition root is in `App.axaml.cs`. All dependencies are wired manually — no DI container needed at this scale:

```csharp
IDeviceRepository repository     = new InMemoryDeviceRepository();
IHardwareAdapter  hardwareAdapter = new SimulatedHardwareAdapter();
var viewModel = new MainWindowViewModel(repository, hardwareAdapter);
desktop.MainWindow = new MainWindow { DataContext = viewModel };
```

For a larger app, replace with `Microsoft.Extensions.DependencyInjection`:

```csharp
var services = new ServiceCollection();
services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
services.AddSingleton<IHardwareAdapter, SimulatedHardwareAdapter>();
services.AddTransient<MainWindowViewModel>();
var provider = services.BuildServiceProvider();
```

---

## Future Improvements

| Area | Improvement |
|------|-------------|
| **Data storage** | Replace `InMemoryDeviceRepository` with SQLite via EF Core or Dapper |
| **Real hardware** | Implement `UsbHardwareAdapter` using LibUsbDotNet or HidSharp |
| **VISA instruments** | Implement `VisaHardwareAdapter` for GPIB/LAN test equipment |
| **Unit tests** | Add xUnit project with Moq for ViewModel and repository tests |
| **CI/CD** | GitHub Actions workflow: `dotnet build` + `dotnet test` on push |
| **Logging** | Add Microsoft.Extensions.Logging for structured hardware event logs |
| **Config persistence** | Save/load device list to JSON or SQLite on startup/shutdown |
| **Status polling** | Background `IHostedService` to periodically poll device connection status |
