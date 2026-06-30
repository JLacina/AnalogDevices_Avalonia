// MainWindowViewModel is the core of the MVVM pattern.
//
// MVVM EXPLAINED:
//   Model      = DeviceModel           → pure data
//   View       = MainWindow.axaml      → pure UI (no logic)
//   ViewModel  = This class            → UI state + commands + business logic
//
// WHY THE VIEWMODEL DOES NOT TALK DIRECTLY TO HARDWARE:
//   If the ViewModel called USB APIs directly, it would be impossible to unit-test
//   without physical hardware. By depending on IHardwareAdapter, we can inject a
//   SimulatedHardwareAdapter in tests and a real USB adapter in production.
//
// DEPENDENCY INJECTION:
//   Both IDeviceRepository and IHardwareAdapter are injected via the constructor.
//   This is standard DI — no service locator, no singletons, easy to test.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Models;
using HardwareDeviceConfigManager.Services;

namespace HardwareDeviceConfigManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDeviceRepository  _repository;
    private readonly IHardwareAdapter   _hardwareAdapter;

    // ── Bound collection ──────────────────────────────────────────────────────
    public ObservableCollection<DeviceModel> Devices { get; } = new();

    // ── Selected device ───────────────────────────────────────────────────────
    [ObservableProperty]
    private DeviceModel? _selectedDevice;

    partial void OnSelectedDeviceChanged(DeviceModel? value) => LoadFormFromDevice(value);

    // ── Form fields ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _formDeviceName      = string.Empty;
    [ObservableProperty] private string _formDeviceType      = string.Empty;
    [ObservableProperty] private string _formFirmwareVersion  = string.Empty;
    [ObservableProperty] private string _formNotes            = string.Empty;
    [ObservableProperty] private ConnectionType _formConnectionType = ConnectionType.USB;
    [ObservableProperty] private DeviceStatus   _formStatus         = DeviceStatus.Disconnected;

    // ── Validation / status messages ──────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool   _isBusy        = false;

    // ── Enum lists for ComboBoxes ─────────────────────────────────────────────
    public IEnumerable<ConnectionType> ConnectionTypes => Enum.GetValues<ConnectionType>();
    public IEnumerable<DeviceStatus>   StatusValues    => Enum.GetValues<DeviceStatus>();

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainWindowViewModel(IDeviceRepository repository, IHardwareAdapter hardwareAdapter)
    {
        _repository      = repository;
        _hardwareAdapter = hardwareAdapter;
        LoadDevices();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void LoadDevices()
    {
        Devices.Clear();
        foreach (var d in _repository.GetAll())
            Devices.Add(d);
    }

    private void LoadFormFromDevice(DeviceModel? device)
    {
        if (device is null) { ClearFormFields(); return; }
        FormDeviceName      = device.DeviceName;
        FormDeviceType      = device.DeviceType;
        FormFirmwareVersion = device.FirmwareVersion;
        FormNotes           = device.Notes;
        FormConnectionType  = device.ConnectionType;
        FormStatus          = device.Status;
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(FormDeviceName))
        {
            StatusMessage = "Validation error: Device Name is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(FormFirmwareVersion))
        {
            StatusMessage = "Validation error: Firmware Version is required.";
            return false;
        }
        return true;
    }

    private void ClearFormFields()
    {
        FormDeviceName      = string.Empty;
        FormDeviceType      = string.Empty;
        FormFirmwareVersion = string.Empty;
        FormNotes           = string.Empty;
        FormConnectionType  = ConnectionType.USB;
        FormStatus          = DeviceStatus.Disconnected;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddDevice()
    {
        if (!ValidateForm()) return;
        var device = new DeviceModel
        {
            DeviceName      = FormDeviceName,
            DeviceType      = FormDeviceType,
            ConnectionType  = FormConnectionType,
            FirmwareVersion = FormFirmwareVersion,
            Status          = FormStatus,
            Notes           = FormNotes
        };
        _repository.Add(device);
        LoadDevices();
        SelectedDevice = Devices.LastOrDefault();
        StatusMessage  = $"Device '{device.DeviceName}' added (Id={device.Id}).";
    }

    [RelayCommand]
    private void UpdateDevice()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to update."; return; }
        if (!ValidateForm()) return;

        SelectedDevice.DeviceName      = FormDeviceName;
        SelectedDevice.DeviceType      = FormDeviceType;
        SelectedDevice.ConnectionType  = FormConnectionType;
        SelectedDevice.FirmwareVersion = FormFirmwareVersion;
        SelectedDevice.Status          = FormStatus;
        SelectedDevice.Notes           = FormNotes;

        _repository.Update(SelectedDevice);
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);
        StatusMessage  = $"Device '{FormDeviceName}' updated.";
    }

    [RelayCommand]
    private void DeleteDevice()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to delete."; return; }
        var name = SelectedDevice.DeviceName;
        _repository.Delete(SelectedDevice.Id);
        LoadDevices();
        SelectedDevice = null;
        ClearFormFields();
        StatusMessage = $"Device '{name}' deleted.";
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedDevice = null;
        ClearFormFields();
        StatusMessage = "Form cleared.";
    }

    [RelayCommand]
    private async Task SimulateConnect()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to connect."; return; }
        IsBusy        = true;
        StatusMessage = $"Connecting to '{SelectedDevice.DeviceName}'...";

        // The ViewModel calls the HAL interface — it doesn't know if this is USB, VISA, or simulated.
        var newStatus = await _hardwareAdapter.ConnectAsync(SelectedDevice);

        SelectedDevice.Status = newStatus;
        FormStatus            = newStatus;
        _repository.Update(SelectedDevice);
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);

        IsBusy        = false;
        StatusMessage = $"'{SelectedDevice?.DeviceName}' → {newStatus}";
    }

    [RelayCommand]
    private async Task SimulateDisconnect()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to disconnect."; return; }
        IsBusy        = true;
        StatusMessage = $"Disconnecting '{SelectedDevice.DeviceName}'...";

        var newStatus = await _hardwareAdapter.DisconnectAsync(SelectedDevice);

        SelectedDevice.Status = newStatus;
        FormStatus            = newStatus;
        _repository.Update(SelectedDevice);
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);

        IsBusy        = false;
        StatusMessage = $"'{SelectedDevice?.DeviceName}' → {newStatus}";
    }
}
