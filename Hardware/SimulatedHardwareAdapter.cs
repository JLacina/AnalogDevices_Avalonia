// SimulatedHardwareAdapter simulates hardware connect/disconnect operations.
// Uses Task.Delay to mimic the async latency of real hardware I/O.
// In production, replace with UsbHardwareAdapter, VisaHardwareAdapter, etc.

using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Hardware;

public class SimulatedHardwareAdapter : IHardwareAdapter
{
    public async Task<DeviceStatus> ConnectAsync(DeviceModel device)
    {
        // Simulate hardware handshake delay
        await Task.Delay(600);

        // Simulators always connect; USB/API succeed unless already in Error state
        if (device.ConnectionType == ConnectionType.Simulator || device.Status != DeviceStatus.Error)
            return DeviceStatus.Connected;

        return DeviceStatus.Error;
    }

    public async Task<DeviceStatus> DisconnectAsync(DeviceModel device)
    {
        await Task.Delay(300);
        return DeviceStatus.Disconnected;
    }
}
