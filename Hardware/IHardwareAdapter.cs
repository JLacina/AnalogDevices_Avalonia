// IHardwareAdapter is a simple Hardware Abstraction Layer (HAL) interface.
//
// WHY THIS EXISTS:
//   The ViewModel should not know HOW to talk to hardware.
//   It only knows that it can Connect and Disconnect.
//   This is the same principle used in embedded and desktop hardware software:
//   - The HAL abstracts the physical layer (USB HID, VISA, serial, REST API)
//   - Swap SimulatedHardwareAdapter for a real USB adapter without changing the ViewModel.
//
// EXTENSION POINTS:
//   - UsbHardwareAdapter   → wraps LibUsbDotNet or HidSharp for real USB devices
//   - VisaHardwareAdapter  → wraps NI-VISA / Keysight VISA for GPIB/LAN instruments
//   - RestApiHardwareAdapter → wraps HttpClient calls to a device REST API

using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Hardware;

public interface IHardwareAdapter
{
    /// <summary>Attempts to connect to the given device. Returns updated status.</summary>
    Task<DeviceStatus> ConnectAsync(DeviceModel device);

    /// <summary>Disconnects from the given device. Returns updated status.</summary>
    Task<DeviceStatus> DisconnectAsync(DeviceModel device);
}
