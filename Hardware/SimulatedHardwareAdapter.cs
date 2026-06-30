using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Hardware;

// This simulated adapter stands in for real hardware communication.
// In production this same interface could be implemented with USB drivers,
// REST API calls, SDK libraries, retry logic, and diagnostics.
public class SimulatedHardwareAdapter : IHardwareAdapter
{
    public DeviceStatus Connect(DeviceModel device)
    {
        return device.ConnectionType == ConnectionType.Simulator
            ? DeviceStatus.Connected
            : DeviceStatus.Connected;
    }

    public DeviceStatus Disconnect(DeviceModel device)
    {
        return DeviceStatus.Disconnected;
    }
}
