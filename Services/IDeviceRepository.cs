// IDeviceRepository defines the contract for device data storage.
// The ViewModel depends on this interface — not on a concrete class.
// This makes it easy to swap in a SQLite or REST-backed repository later.

using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Services;

public interface IDeviceRepository
{
    IEnumerable<DeviceModel> GetAll();
    DeviceModel? GetById(int id);
    void Add(DeviceModel device);
    void Update(DeviceModel device);
    void Delete(int id);
}
