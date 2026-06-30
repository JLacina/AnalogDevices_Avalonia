using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Hardware;

// IHardwareAdapter is a simple Hardware Abstraction Layer (HAL) contract.
// The ViewModel depends on this interface so the UI logic can stay independent
// from concrete USB/API implementations.
public interface IHardwareAdapter
{
    DeviceStatus Connect(DeviceModel device);
    DeviceStatus Disconnect(DeviceModel device);
}
