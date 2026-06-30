// MainWindowViewModel — updated to use EditingDeviceViewModel (Option B pattern).
//
// MVVM EXPLAINED:
//   Model           = DeviceModel              → pure POCO data, no UI dependency
//   EditingWrapper  = EditingDeviceViewModel   → observable form state for one device
//   ViewModel       = This class               → list state, commands, orchestration
//   View            = MainWindow.axaml         → pure XAML, zero code-behind logic
//
// KEY CHANGES vs the original flat Form* approach:
//   - All Form* properties replaced by a single EditingDevice wrapper
//   - Right-panel form binds to EditingDevice.DeviceName etc.
//   - UpdateDevice calls EditingDevice.CommitTo() instead of mutating fields directly
//   - SimulateConnect/Disconnect keeps EditingDevice.Status in sync with HAL result
//   - AddDevice uses CommitTo() on a fresh DeviceModel
//
// WHY THE VIEWMODEL DOES NOT TALK DIRECTLY TO HARDWARE:
//   IHardwareAdapter is injected — the ViewModel never knows whether
//   the adapter is USB, VISA, REST API, or a test double.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Models;
using HardwareDeviceConfigManager.Services;

namespace HardwareDeviceConfigManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDeviceRepository _repository;
    private readonly IHardwareAdapter  _hardwareAdapter;

    // ── Device list (DataGrid source) ────────────────────────────────────────
    public ObservableCollection<DeviceModel> Devices { get; } = new();

    // ── Selected row in the DataGrid ─────────────────────────────────────────
    [ObservableProperty]
    private DeviceModel? _selectedDevice;

    partial void OnSelectedDeviceChanged(DeviceModel? value)
    {
        // When the user clicks a different row, create a fresh editing wrapper.
        // The old wrapper is simply discarded — its changes are lost unless
        // CommitTo() was called. This is the intentional "cancel on select" behaviour.
        EditingDevice = value is null ? null : new EditingDeviceViewModel(value);
    }

    // ── The observable editing wrapper — right-panel form binds to this ──────
    // Null when no device is selected (form is blank / disabled via IsEnabled).
    [ObservableProperty]
    private EditingDeviceViewModel? _editingDevice;

    // ── Status / busy state ───────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool   _isBusy        = false;

    // ── ComboBox sources (static enum lists) ─────────────────────────────────
    public static IEnumerable<ConnectionType> ConnectionTypes { get; } = Enum.GetValues<ConnectionType>();
    public static IEnumerable<DeviceStatus>   StatusValues    { get; } = Enum.GetValues<DeviceStatus>();

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

    private bool ValidateEditing(EditingDeviceViewModel editing)
    {
        if (string.IsNullOrWhiteSpace(editing.DeviceName))
        {
            StatusMessage = "Validation error: Device Name is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(editing.FirmwareVersion))
        {
            StatusMessage = "Validation error: Firmware Version is required.";
            return false;
        }
        return true;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddDevice()
    {
        // For Add, create a blank wrapper if nothing is selected
        var editing = EditingDevice ?? new EditingDeviceViewModel(new DeviceModel());
        if (!ValidateEditing(editing)) return;

        var device = new DeviceModel();
        editing.CommitTo(device);

        _repository.Add(device);
        LoadDevices();
        SelectedDevice = Devices.LastOrDefault(d => d.Id == device.Id);
        StatusMessage  = $"Device '{device.DeviceName}' added (Id={device.Id}).";
    }

    [RelayCommand]
    private void UpdateDevice()
    {
        if (SelectedDevice is null || EditingDevice is null)
        {
            StatusMessage = "Select a device to update.";
            return;
        }
        if (!ValidateEditing(EditingDevice)) return;

        // Commit the observable editing state back to the domain model
        EditingDevice.CommitTo(SelectedDevice);
        _repository.Update(SelectedDevice);

        // LoadDevices() is still needed here because DeviceModel is a POCO
        // (no INotifyPropertyChanged). Combine with the observable DeviceModel
        // variant to eliminate this and get true live DataGrid updates.
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);
        StatusMessage  = $"Device '{EditingDevice.DeviceName}' updated.";
    }

    [RelayCommand]
    private void DeleteDevice()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to delete."; return; }
        var name = SelectedDevice.DeviceName;
        _repository.Delete(SelectedDevice.Id);
        LoadDevices();
        SelectedDevice = null;
        StatusMessage  = $"Device '{name}' deleted.";
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedDevice = null;
        // EditingDevice is nulled automatically via OnSelectedDeviceChanged
        StatusMessage = "Form cleared.";
    }

    [RelayCommand]
    private async Task SimulateConnect()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to connect."; return; }
        IsBusy        = true;
        StatusMessage = $"Connecting to '{SelectedDevice.DeviceName}'...";

        // The ViewModel calls the HAL interface — it doesn't know if this is
        // USB, VISA, REST API, or a test double. That is the HAL contract.
        var newStatus = await _hardwareAdapter.ConnectAsync(SelectedDevice);

        SelectedDevice.Status = newStatus;

        // Keep the editing wrapper in sync so the status badge updates live
        if (EditingDevice is not null)
            EditingDevice.Status = newStatus;

        _repository.Update(SelectedDevice);
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);

        IsBusy        = false;
        StatusMessage = $"'{SelectedDevice?.DeviceName}' \u2192 {newStatus}";
    }

    [RelayCommand]
    private async Task SimulateDisconnect()
    {
        if (SelectedDevice is null) { StatusMessage = "Select a device to disconnect."; return; }
        IsBusy        = true;
        StatusMessage = $"Disconnecting '{SelectedDevice.DeviceName}'...";

        var newStatus = await _hardwareAdapter.DisconnectAsync(SelectedDevice);

        SelectedDevice.Status = newStatus;

        if (EditingDevice is not null)
            EditingDevice.Status = newStatus;

        _repository.Update(SelectedDevice);
        var id = SelectedDevice.Id;
        LoadDevices();
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == id);

        IsBusy        = false;
        StatusMessage = $"'{SelectedDevice?.DeviceName}' \u2192 {newStatus}";
    }
}
