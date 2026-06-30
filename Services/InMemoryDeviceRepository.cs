using HardwareDeviceConfigManager.Models;
using System.Collections.Generic;
using System.Linq;

namespace HardwareDeviceConfigManager.Services;

public class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly List<DeviceModel> _devices =
    [
        new DeviceModel
        {
            Id = 1,
            DeviceName = "Lab ADC Board",
            DeviceType = "ADC",
            ConnectionType = ConnectionType.USB,
            FirmwareVersion = "1.2.0",
            Status = DeviceStatus.Connected,
            Notes = "Primary bench hardware"
        },
        new DeviceModel
        {
            Id = 2,
            DeviceName = "Remote Signal API",
            DeviceType = "API Service",
            ConnectionType = ConnectionType.API,
            FirmwareVersion = "2.4.1",
            Status = DeviceStatus.Disconnected,
            Notes = "Cloud endpoint profile"
        },
        new DeviceModel
        {
            Id = 3,
            DeviceName = "Device Simulator",
            DeviceType = "Simulation",
            ConnectionType = ConnectionType.Simulator,
            FirmwareVersion = "0.9.3",
            Status = DeviceStatus.Error,
            Notes = "Used for local integration testing"
        }
    ];

    public IReadOnlyList<DeviceModel> GetAll() => _devices.Select(d => d.Clone()).ToList();

    public DeviceModel Add(DeviceModel device)
    {
        var clone = device.Clone();
        _devices.Add(clone);
        return clone.Clone();
    }

    public bool Update(DeviceModel device)
    {
        var index = _devices.FindIndex(d => d.Id == device.Id);
        if (index < 0)
        {
            return false;
        }

        _devices[index] = device.Clone();
        return true;
    }

    public bool Delete(int id)
    {
        var index = _devices.FindIndex(d => d.Id == id);
        if (index < 0)
        {
            return false;
        }

        _devices.RemoveAt(index);
        return true;
    }

    public int GetNextId() => _devices.Count == 0 ? 1 : _devices.Max(d => d.Id) + 1;
}
