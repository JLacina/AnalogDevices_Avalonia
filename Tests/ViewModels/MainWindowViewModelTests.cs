// =============================================================================
// MainWindowViewModelTests.cs
// =============================================================================
// Full unit-test coverage for MainWindowViewModel using xUnit + Moq.
//
// WHAT WE ARE TESTING:
//   • All 6 RelayCommands (AddDevice, UpdateDevice, DeleteDevice,
//     ClearForm, SimulateConnect, SimulateDisconnect)
//   • Validation rules (DeviceName required, FirmwareVersion required)
//   • IsBusy lifecycle during async hardware operations
//   • Form population when SelectedDevice changes
//   • Status message updates for every operation
//   • Edge cases: commands called with no selection, duplicate validation
//
// WHY MOCKS WORK HERE:
//   IDeviceRepository and IHardwareAdapter are interfaces injected via the
//   constructor — Moq replaces them with controllable fakes so tests run
//   instantly without a database, UI thread, or real hardware.
// =============================================================================

using HardwareDeviceConfigManager.Hardware;
using HardwareDeviceConfigManager.Models;
using HardwareDeviceConfigManager.Services;
using HardwareDeviceConfigManager.ViewModels;
using Moq;

namespace HardwareDeviceConfigManager.Tests.ViewModels;

// ---------------------------------------------------------------------------
// Shared test infrastructure
// ---------------------------------------------------------------------------

/// <summary>
/// Base class that wires up fresh mocks and a ViewModel for every test.
/// Keeping this in a base class prevents copy-paste between test classes.
/// </summary>
public abstract class MainWindowViewModelTestBase
{
    // Mocks are recreated per-test because xUnit constructs a new instance
    // of each test class for every [Fact] / [Theory].
    protected readonly Mock<IDeviceRepository> RepoMock  = new(MockBehavior.Strict);
    protected readonly Mock<IHardwareAdapter>  HalMock   = new(MockBehavior.Strict);

    /// <summary>Seed data returned by RepoMock.GetAll() by default.</summary>
    protected List<DeviceModel> SeedDevices { get; } = new()
    {
        new DeviceModel { Id = 1, DeviceName = "Device A", DeviceType = "DAQ",
                         ConnectionType = ConnectionType.USB,
                         FirmwareVersion = "1.0", Status = DeviceStatus.Disconnected },
        new DeviceModel { Id = 2, DeviceName = "Device B", DeviceType = "Sensor",
                         ConnectionType = ConnectionType.API,
                         FirmwareVersion = "2.0", Status = DeviceStatus.Connected },
    };

    /// <summary>Configures GetAll() to return the seed list, then creates the VM.</summary>
    protected MainWindowViewModel BuildVm()
    {
        RepoMock.Setup(r => r.GetAll()).Returns(() => SeedDevices.ToList());
        return new MainWindowViewModel(RepoMock.Object, HalMock.Object);
    }
}

// ---------------------------------------------------------------------------
// 1. Constructor / initial state
// ---------------------------------------------------------------------------

public class Constructor_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void Constructor_LoadsDevicesFromRepository()
    {
        var vm = BuildVm();

        // The Devices collection should mirror the seed data
        Assert.Equal(2, vm.Devices.Count);
        Assert.Equal("Device A", vm.Devices[0].DeviceName);
        Assert.Equal("Device B", vm.Devices[1].DeviceName);
    }

    [Fact]
    public void Constructor_SetsInitialStatusMessage_ToReady()
    {
        var vm = BuildVm();
        Assert.Equal("Ready", vm.StatusMessage);
    }

    [Fact]
    public void Constructor_IsBusy_IsFalse()
    {
        var vm = BuildVm();
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void Constructor_SelectedDevice_IsNull()
    {
        var vm = BuildVm();
        Assert.Null(vm.SelectedDevice);
    }
}

// ---------------------------------------------------------------------------
// 2. Form population — SelectedDevice → form fields
// ---------------------------------------------------------------------------

public class FormLoading_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void SettingSelectedDevice_PopulatesAllFormFields()
    {
        var vm     = BuildVm();
        var device = SeedDevices[0];

        vm.SelectedDevice = device;

        Assert.Equal(device.DeviceName,      vm.FormDeviceName);
        Assert.Equal(device.DeviceType,      vm.FormDeviceType);
        Assert.Equal(device.FirmwareVersion, vm.FormFirmwareVersion);
        Assert.Equal(device.Notes,           vm.FormNotes);
        Assert.Equal(device.ConnectionType,  vm.FormConnectionType);
        Assert.Equal(device.Status,          vm.FormStatus);
    }

    [Fact]
    public void SettingSelectedDevice_ToNull_ClearsFormFields()
    {
        var vm = BuildVm();

        // First select something so fields are populated
        vm.SelectedDevice = SeedDevices[0];

        // Then clear
        vm.SelectedDevice = null;

        Assert.Equal(string.Empty,               vm.FormDeviceName);
        Assert.Equal(string.Empty,               vm.FormDeviceType);
        Assert.Equal(string.Empty,               vm.FormFirmwareVersion);
        Assert.Equal(string.Empty,               vm.FormNotes);
        Assert.Equal(ConnectionType.USB,         vm.FormConnectionType);
        Assert.Equal(DeviceStatus.Disconnected,  vm.FormStatus);
    }
}

// ---------------------------------------------------------------------------
// 3. Validation
// ---------------------------------------------------------------------------

public class Validation_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void AddDevice_WithEmptyDeviceName_SetsValidationErrorMessage()
    {
        // Arrange
        RepoMock.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);

        vm.FormDeviceName      = "";
        vm.FormFirmwareVersion = "1.0.0";

        // Act
        vm.AddDeviceCommand.Execute(null);

        // Assert — repository.Add must NOT have been called
        RepoMock.Verify(r => r.Add(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Device Name is required", vm.StatusMessage);
    }

    [Fact]
    public void AddDevice_WithWhitespaceDeviceName_SetsValidationErrorMessage()
    {
        RepoMock.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);

        vm.FormDeviceName      = "   ";
        vm.FormFirmwareVersion = "1.0.0";

        vm.AddDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Add(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Device Name is required", vm.StatusMessage);
    }

    [Fact]
    public void AddDevice_WithEmptyFirmwareVersion_SetsValidationErrorMessage()
    {
        RepoMock.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);

        vm.FormDeviceName      = "My Device";
        vm.FormFirmwareVersion = "";

        vm.AddDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Add(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Firmware Version is required", vm.StatusMessage);
    }

    [Fact]
    public void UpdateDevice_WithEmptyDeviceName_SetsValidationError_AndDoesNotCallUpdate()
    {
        var vm = BuildVm();
        vm.SelectedDevice      = SeedDevices[0];
        vm.FormDeviceName      = "";
        vm.FormFirmwareVersion = "1.0.0";

        vm.UpdateDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Update(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Device Name is required", vm.StatusMessage);
    }
}

// ---------------------------------------------------------------------------
// 4. AddDeviceCommand
// ---------------------------------------------------------------------------

public class AddDevice_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void AddDevice_ValidForm_CallsRepositoryAdd()
    {
        // Arrange
        RepoMock.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
        RepoMock.Setup(r => r.Add(It.IsAny<DeviceModel>())).Verifiable();
        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);

        vm.FormDeviceName      = "New Sensor";
        vm.FormDeviceType      = "Sensor";
        vm.FormFirmwareVersion = "3.0.1";
        vm.FormConnectionType  = ConnectionType.USB;
        vm.FormStatus          = DeviceStatus.Disconnected;
        vm.FormNotes           = "Test note";

        // Act
        vm.AddDeviceCommand.Execute(null);

        // Assert — Add called exactly once with matching data
        RepoMock.Verify(r => r.Add(It.Is<DeviceModel>(d =>
            d.DeviceName      == "New Sensor"   &&
            d.DeviceType      == "Sensor"        &&
            d.FirmwareVersion == "3.0.1"         &&
            d.ConnectionType  == ConnectionType.USB &&
            d.Status          == DeviceStatus.Disconnected
        )), Times.Once);
    }

    [Fact]
    public void AddDevice_ValidForm_RefreshesDeviceList()
    {
        // First GetAll returns empty; after Add it returns one device
        var added = new DeviceModel { Id = 1, DeviceName = "Scope", FirmwareVersion = "1.0" };
        var callCount = 0;
        RepoMock.Setup(r => r.GetAll()).Returns(() =>
            callCount++ == 0 ? new List<DeviceModel>() : new List<DeviceModel> { added });
        RepoMock.Setup(r => r.Add(It.IsAny<DeviceModel>())).Callback<DeviceModel>(_ => { });

        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);
        vm.FormDeviceName      = "Scope";
        vm.FormFirmwareVersion = "1.0";

        vm.AddDeviceCommand.Execute(null);

        Assert.Single(vm.Devices);
        Assert.Equal("Scope", vm.Devices[0].DeviceName);
    }

    [Fact]
    public void AddDevice_ValidForm_SetsSuccessStatusMessage()
    {
        RepoMock.Setup(r => r.GetAll()).Returns(new List<DeviceModel>());
        RepoMock.Setup(r => r.Add(It.IsAny<DeviceModel>())).Verifiable();
        var vm = new MainWindowViewModel(RepoMock.Object, HalMock.Object);

        vm.FormDeviceName      = "Probe X";
        vm.FormFirmwareVersion = "2.0";

        vm.AddDeviceCommand.Execute(null);

        Assert.Contains("Probe X", vm.StatusMessage);
        Assert.Contains("added", vm.StatusMessage);
    }
}

// ---------------------------------------------------------------------------
// 5. UpdateDeviceCommand
// ---------------------------------------------------------------------------

public class UpdateDevice_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void UpdateDevice_WithNoSelection_SetsStatusMessage_AndDoesNotCallUpdate()
    {
        var vm = BuildVm();
        vm.SelectedDevice = null;

        vm.UpdateDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Update(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Select a device", vm.StatusMessage);
    }

    [Fact]
    public void UpdateDevice_ValidForm_CallsRepositoryUpdate_WithFormValues()
    {
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice      = SeedDevices[0];
        vm.FormDeviceName      = "Updated Name";
        vm.FormDeviceType      = "Updated Type";
        vm.FormFirmwareVersion = "9.9.9";
        vm.FormConnectionType  = ConnectionType.Simulator;
        vm.FormStatus          = DeviceStatus.Error;
        vm.FormNotes           = "Updated note";

        vm.UpdateDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Update(It.Is<DeviceModel>(d =>
            d.DeviceName      == "Updated Name"          &&
            d.DeviceType      == "Updated Type"          &&
            d.FirmwareVersion == "9.9.9"                 &&
            d.ConnectionType  == ConnectionType.Simulator &&
            d.Status          == DeviceStatus.Error      &&
            d.Notes           == "Updated note"
        )), Times.Once);
    }

    [Fact]
    public void UpdateDevice_ValidForm_SetsSuccessStatusMessage()
    {
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice      = SeedDevices[0];
        vm.FormDeviceName      = "Renamed Device";
        vm.FormFirmwareVersion = "5.0";

        vm.UpdateDeviceCommand.Execute(null);

        Assert.Contains("Renamed Device", vm.StatusMessage);
        Assert.Contains("updated", vm.StatusMessage);
    }
}

// ---------------------------------------------------------------------------
// 6. DeleteDeviceCommand
// ---------------------------------------------------------------------------

public class DeleteDevice_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void DeleteDevice_WithNoSelection_SetsStatusMessage_AndDoesNotCallDelete()
    {
        var vm = BuildVm();
        vm.SelectedDevice = null;

        vm.DeleteDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        Assert.Contains("Select a device", vm.StatusMessage);
    }

    [Fact]
    public void DeleteDevice_WithSelection_CallsRepositoryDelete_WithCorrectId()
    {
        RepoMock.Setup(r => r.Delete(1)).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0]; // Id = 1

        vm.DeleteDeviceCommand.Execute(null);

        RepoMock.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public void DeleteDevice_ClearsSelectedDevice_AndFormFields()
    {
        RepoMock.Setup(r => r.Delete(It.IsAny<int>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        vm.FormDeviceName = "Something";

        vm.DeleteDeviceCommand.Execute(null);

        Assert.Null(vm.SelectedDevice);
        Assert.Equal(string.Empty, vm.FormDeviceName);
        Assert.Equal(string.Empty, vm.FormFirmwareVersion);
    }

    [Fact]
    public void DeleteDevice_SetsStatusMessage_ContainingDeletedDeviceName()
    {
        RepoMock.Setup(r => r.Delete(It.IsAny<int>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1]; // "Device B"

        vm.DeleteDeviceCommand.Execute(null);

        Assert.Contains("Device B", vm.StatusMessage);
        Assert.Contains("deleted", vm.StatusMessage);
    }
}

// ---------------------------------------------------------------------------
// 7. ClearFormCommand
// ---------------------------------------------------------------------------

public class ClearForm_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void ClearForm_ClearsSelectedDevice()
    {
        var vm = BuildVm();
        vm.SelectedDevice = SeedDevices[0];

        vm.ClearFormCommand.Execute(null);

        Assert.Null(vm.SelectedDevice);
    }

    [Fact]
    public void ClearForm_ResetsAllFormFieldsToDefaults()
    {
        var vm = BuildVm();
        vm.SelectedDevice      = SeedDevices[0];
        vm.FormDeviceName      = "Something";
        vm.FormFirmwareVersion = "9.9";
        vm.FormNotes           = "Some note";

        vm.ClearFormCommand.Execute(null);

        Assert.Equal(string.Empty,              vm.FormDeviceName);
        Assert.Equal(string.Empty,              vm.FormDeviceType);
        Assert.Equal(string.Empty,              vm.FormFirmwareVersion);
        Assert.Equal(string.Empty,              vm.FormNotes);
        Assert.Equal(ConnectionType.USB,        vm.FormConnectionType);
        Assert.Equal(DeviceStatus.Disconnected, vm.FormStatus);
    }

    [Fact]
    public void ClearForm_SetsStatusMessage_FormCleared()
    {
        var vm = BuildVm();

        vm.ClearFormCommand.Execute(null);

        Assert.Contains("cleared", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClearForm_WithNoSelection_DoesNotThrow()
    {
        var vm = BuildVm();
        vm.SelectedDevice = null;

        // Should not throw even when nothing is selected
        var ex = Record.Exception(() => vm.ClearFormCommand.Execute(null));
        Assert.Null(ex);
    }
}

// ---------------------------------------------------------------------------
// 8. SimulateConnectCommand  (async)
// ---------------------------------------------------------------------------

public class SimulateConnect_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public async Task SimulateConnect_WithNoSelection_SetsStatusMessage_AndDoesNotCallHal()
    {
        var vm = BuildVm();
        vm.SelectedDevice = null;

        // Execute the async command and await it
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        HalMock.Verify(h => h.ConnectAsync(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Select a device", vm.StatusMessage);
    }

    [Fact]
    public async Task SimulateConnect_WithSelection_CallsHalConnectAsync()
    {
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Connected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];

        await vm.SimulateConnectCommand.ExecuteAsync(null);

        HalMock.Verify(h => h.ConnectAsync(SeedDevices[0]), Times.Once);
    }

    [Fact]
    public async Task SimulateConnect_UpdatesDeviceStatus_ToConnected()
    {
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Connected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        Assert.Equal(DeviceStatus.Connected, vm.FormStatus);
    }

    [Fact]
    public async Task SimulateConnect_CallsRepositoryUpdate()
    {
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Connected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        RepoMock.Verify(r => r.Update(It.IsAny<DeviceModel>()), Times.Once);
    }

    [Fact]
    public async Task SimulateConnect_IsBusy_IsFalse_AfterCompletion()
    {
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Connected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        // IsBusy must always be reset to false after the operation completes
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task SimulateConnect_SetsStatusMessage_ContainingNewStatus()
    {
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Connected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        Assert.Contains("Connected", vm.StatusMessage);
    }

    [Fact]
    public async Task SimulateConnect_WhenHalReturnsError_UpdatesFormStatus_ToError()
    {
        // Simulate a device that stays in Error (e.g. failed handshake)
        HalMock.Setup(h => h.ConnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Error);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[0];
        await vm.SimulateConnectCommand.ExecuteAsync(null);

        Assert.Equal(DeviceStatus.Error, vm.FormStatus);
        Assert.False(vm.IsBusy);
    }
}

// ---------------------------------------------------------------------------
// 9. SimulateDisconnectCommand  (async)
// ---------------------------------------------------------------------------

public class SimulateDisconnect_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public async Task SimulateDisconnect_WithNoSelection_SetsStatusMessage_AndDoesNotCallHal()
    {
        var vm = BuildVm();
        vm.SelectedDevice = null;

        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        HalMock.Verify(h => h.DisconnectAsync(It.IsAny<DeviceModel>()), Times.Never);
        Assert.Contains("Select a device", vm.StatusMessage);
    }

    [Fact]
    public async Task SimulateDisconnect_WithSelection_CallsHalDisconnectAsync()
    {
        HalMock.Setup(h => h.DisconnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Disconnected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1]; // "Device B" — currently Connected
        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        HalMock.Verify(h => h.DisconnectAsync(SeedDevices[1]), Times.Once);
    }

    [Fact]
    public async Task SimulateDisconnect_UpdatesFormStatus_ToDisconnected()
    {
        HalMock.Setup(h => h.DisconnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Disconnected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1];
        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        Assert.Equal(DeviceStatus.Disconnected, vm.FormStatus);
    }

    [Fact]
    public async Task SimulateDisconnect_IsBusy_IsFalse_AfterCompletion()
    {
        HalMock.Setup(h => h.DisconnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Disconnected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1];
        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task SimulateDisconnect_CallsRepositoryUpdate()
    {
        HalMock.Setup(h => h.DisconnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Disconnected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1];
        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        RepoMock.Verify(r => r.Update(It.IsAny<DeviceModel>()), Times.Once);
    }

    [Fact]
    public async Task SimulateDisconnect_SetsStatusMessage_ContainingNewStatus()
    {
        HalMock.Setup(h => h.DisconnectAsync(It.IsAny<DeviceModel>()))
               .ReturnsAsync(DeviceStatus.Disconnected);
        RepoMock.Setup(r => r.Update(It.IsAny<DeviceModel>())).Verifiable();
        var vm = BuildVm();

        vm.SelectedDevice = SeedDevices[1];
        await vm.SimulateDisconnectCommand.ExecuteAsync(null);

        Assert.Contains("Disconnected", vm.StatusMessage);
    }
}

// ---------------------------------------------------------------------------
// 10. ComboBox source properties
// ---------------------------------------------------------------------------

public class EnumSources_Tests : MainWindowViewModelTestBase
{
    [Fact]
    public void ConnectionTypes_ContainsAllThreeValues()
    {
        var vm = BuildVm();
        var values = vm.ConnectionTypes.ToList();

        Assert.Contains(ConnectionType.USB,       values);
        Assert.Contains(ConnectionType.API,       values);
        Assert.Contains(ConnectionType.Simulator, values);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void StatusValues_ContainsAllThreeValues()
    {
        var vm = BuildVm();
        var values = vm.StatusValues.ToList();

        Assert.Contains(DeviceStatus.Connected,    values);
        Assert.Contains(DeviceStatus.Disconnected, values);
        Assert.Contains(DeviceStatus.Error,        values);
        Assert.Equal(3, values.Count);
    }
}
