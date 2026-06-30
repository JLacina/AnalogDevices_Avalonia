# HardwareDeviceConfigManager

A small Avalonia desktop CRUD application for hardware configuration interview practice.

## How to run

1. Install the .NET SDK (uses the installed SDK on your machine).
2. From the repository root:
   ```bash
   dotnet restore
   dotnet run
   ```

## Project structure

- `/Models/DeviceModel.cs` - Device data model and enums (`ConnectionType`, `DeviceStatus`)
- `/ViewModels/MainWindowViewModel.cs` - UI state, validation, and commands
- `/Views/MainWindow.axaml` - Two-pane UI (device list + edit form)
- `/Services/IDeviceRepository.cs` - Repository abstraction for CRUD data access
- `/Services/InMemoryDeviceRepository.cs` - In-memory repository with sample device data
- `/Hardware/IHardwareAdapter.cs` - Hardware Abstraction Layer (HAL) contract
- `/Hardware/SimulatedHardwareAdapter.cs` - Simulated HAL implementation
- `/App.axaml.cs` - Dependency injection composition root

## MVVM architecture

This app follows MVVM:

- **View** (`MainWindow.axaml`) contains Avalonia controls and bindings.
- **ViewModel** (`MainWindowViewModel`) owns UI state, validation, and commands.
- **Model** (`DeviceModel`) contains the data representation.

The View binds to command properties:

- `AddDeviceCommand`
- `UpdateDeviceCommand`
- `DeleteDeviceCommand`
- `ClearFormCommand`
- `SimulateConnectCommand`
- `SimulateDisconnectCommand`

## HAL abstraction

`MainWindowViewModel` does not call hardware APIs directly. It depends on `IHardwareAdapter`, which is a simple HAL contract. The current implementation (`SimulatedHardwareAdapter`) simulates connect/disconnect behavior.

This can be extended by adding new adapters (for USB SDK, REST API device gateways, etc.) without rewriting UI logic.

## Validation

Basic form validation is implemented in the ViewModel:

- `DeviceName` is required
- `FirmwareVersion` is required

## Testing approach

This repository currently has no test project scaffold. The app logic is structured so tests can be added easily by mocking:

- `IDeviceRepository`
- `IHardwareAdapter`

Then asserting command behavior in `MainWindowViewModel`.

## Future improvements

- SQLite persistence for device configs
- Real USB/API hardware adapter implementations
- Structured logging and diagnostics
- GitHub Actions build/test workflow
- Dedicated unit tests for ViewModel and services
