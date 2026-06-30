using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Models;
using HardwareDeviceConfigManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HardwareDeviceConfigManager.ViewModels;

// MVVM keeps responsibilities separated:
// - View (XAML) handles rendering and user interaction wiring.
// - ViewModel holds UI state/commands and application logic.
// - Model represents hardware device data.
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHardwareAdapter _hardwareAdapter;

    public ObservableCollection<DeviceModel> Devices { get; } = [];
    public IReadOnlyList<ConnectionType> ConnectionTypes { get; } = Enum.GetValues<ConnectionType>();
    public IReadOnlyList<DeviceStatus> Statuses { get; } = Enum.GetValues<DeviceStatus>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(SimulateConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SimulateDisconnectCommand))]
    private DeviceModel? selectedDevice;

    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private string deviceType = string.Empty;

    [ObservableProperty]
    private ConnectionType selectedConnectionType = ConnectionType.USB;

    [ObservableProperty]
    private string firmwareVersion = string.Empty;

    [ObservableProperty]
    private DeviceStatus selectedStatus = DeviceStatus.Disconnected;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private string selectedDeviceStatus = "No device selected";

    // Parameterless constructor is only for XAML previewer support.
    public MainWindowViewModel() : this(new InMemoryDeviceRepository(), new SimulatedHardwareAdapter())
    {
    }

    // The ViewModel depends on abstractions, not concrete hardware APIs.
    // This lets us test UI logic easily and swap in a real adapter later.
    public MainWindowViewModel(IDeviceRepository deviceRepository, IHardwareAdapter hardwareAdapter)
    {
        _deviceRepository = deviceRepository;
        _hardwareAdapter = hardwareAdapter;

        LoadDevices();
    }

    partial void OnSelectedDeviceChanged(DeviceModel? value)
    {
        if (value is null)
        {
            SelectedDeviceStatus = "No device selected";
            return;
        }

        Id = value.Id;
        DeviceName = value.DeviceName;
        DeviceType = value.DeviceType;
        SelectedConnectionType = value.ConnectionType;
        FirmwareVersion = value.FirmwareVersion;
        SelectedStatus = value.Status;
        Notes = value.Notes;
        SelectedDeviceStatus = value.Status.ToString();
        ValidationMessage = string.Empty;
    }

    [RelayCommand]
    private void AddDevice()
    {
        if (!ValidateForm())
        {
            return;
        }

        if (Id <= 0)
        {
            Id = _deviceRepository.GetNextId();
        }

        var device = BuildFromForm();
        var added = _deviceRepository.Add(device);
        Devices.Add(added);
        SelectedDevice = added;
        ValidationMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnDevice))]
    private void UpdateDevice()
    {
        if (!ValidateForm())
        {
            return;
        }

        if (Id <= 0)
        {
            ValidationMessage = "Select a device to update.";
            return;
        }

        var updated = BuildFromForm();
        if (!_deviceRepository.Update(updated))
        {
            ValidationMessage = "Unable to update device.";
            return;
        }

        ReplaceInCollection(updated);
        SelectedDevice = updated;
        ValidationMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnDevice))]
    private void DeleteDevice()
    {
        if (Id <= 0)
        {
            ValidationMessage = "Select a device to delete.";
            return;
        }

        if (!_deviceRepository.Delete(Id))
        {
            ValidationMessage = "Unable to delete device.";
            return;
        }

        var existing = Devices.FirstOrDefault(d => d.Id == Id);
        if (existing is not null)
        {
            Devices.Remove(existing);
        }

        ClearForm();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedDevice = null;
        Id = 0;
        DeviceName = string.Empty;
        DeviceType = string.Empty;
        SelectedConnectionType = ConnectionType.USB;
        FirmwareVersion = string.Empty;
        SelectedStatus = DeviceStatus.Disconnected;
        Notes = string.Empty;
        ValidationMessage = string.Empty;
        SelectedDeviceStatus = "No device selected";
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnDevice))]
    private void SimulateConnect()
    {
        ApplyAdapterStatus(isConnect: true);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnDevice))]
    private void SimulateDisconnect()
    {
        ApplyAdapterStatus(isConnect: false);
    }

    private bool CanOperateOnDevice() => SelectedDevice is not null;

    private void ApplyAdapterStatus(bool isConnect)
    {
        if (SelectedDevice is null)
        {
            ValidationMessage = "Select a device first.";
            return;
        }

        // The ViewModel does not talk to USB/API implementations directly.
        // Instead it calls the HAL interface, making hardware behavior pluggable.
        var newStatus = isConnect
            ? _hardwareAdapter.Connect(SelectedDevice)
            : _hardwareAdapter.Disconnect(SelectedDevice);

        SelectedStatus = newStatus;
        var updated = BuildFromForm();

        if (_deviceRepository.Update(updated))
        {
            ReplaceInCollection(updated);
            SelectedDevice = updated;
            SelectedDeviceStatus = updated.Status.ToString();
            ValidationMessage = string.Empty;
        }
        else
        {
            ValidationMessage = "Unable to apply simulated hardware status.";
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(DeviceName))
        {
            ValidationMessage = "DeviceName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FirmwareVersion))
        {
            ValidationMessage = "FirmwareVersion is required.";
            return false;
        }

        return true;
    }

    private DeviceModel BuildFromForm()
    {
        return new DeviceModel
        {
            Id = Id,
            DeviceName = DeviceName.Trim(),
            DeviceType = DeviceType.Trim(),
            ConnectionType = SelectedConnectionType,
            FirmwareVersion = FirmwareVersion.Trim(),
            Status = SelectedStatus,
            Notes = Notes.Trim()
        };
    }

    private void LoadDevices()
    {
        Devices.Clear();
        foreach (var device in _deviceRepository.GetAll())
        {
            Devices.Add(device);
        }
    }

    private void ReplaceInCollection(DeviceModel updated)
    {
        var index = Devices
            .Select((device, idx) => new { device, idx })
            .FirstOrDefault(x => x.device.Id == updated.Id)
            ?.idx;

        if (index is not null)
        {
            Devices[index.Value] = updated;
        }
    }
}
