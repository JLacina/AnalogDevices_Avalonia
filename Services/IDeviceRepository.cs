using HardwareDeviceConfigManager.Models;
using System.Collections.Generic;

namespace HardwareDeviceConfigManager.Services;

public interface IDeviceRepository
{
    IReadOnlyList<DeviceModel> GetAll();
    DeviceModel Add(DeviceModel device);
    bool Update(DeviceModel device);
    bool Delete(int id);
    int GetNextId();
}
