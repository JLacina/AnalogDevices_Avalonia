// InMemoryDeviceRepository is a simple in-memory implementation of IDeviceRepository.
// Seeded with sample data so the app is useful from first launch.
// In a real app, replace this with a SQLite, REST API, or database-backed repository.

using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.Services;

public class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly List<DeviceModel> _devices = new();
    private int _nextId = 1;

    public InMemoryDeviceRepository()
    {
        // Seed with sample devices
        _devices.AddRange(new[]
        {
            new DeviceModel { Id = _nextId++, DeviceName = "Oscilloscope A",   DeviceType = "Oscilloscope", ConnectionType = ConnectionType.USB,       FirmwareVersion = "2.1.0", Status = DeviceStatus.Connected,    Notes = "Lab bench unit" },
            new DeviceModel { Id = _nextId++, DeviceName = "Signal Gen B",     DeviceType = "Generator",    ConnectionType = ConnectionType.API,       FirmwareVersion = "1.0.3", Status = DeviceStatus.Disconnected, Notes = "Remote API device" },
            new DeviceModel { Id = _nextId++, DeviceName = "Sim Sensor C",     DeviceType = "Sensor",       ConnectionType = ConnectionType.Simulator, FirmwareVersion = "0.9.1", Status = DeviceStatus.Connected,    Notes = "Simulated test sensor" },
            new DeviceModel { Id = _nextId++, DeviceName = "USB DAQ Module D", DeviceType = "DAQ",          ConnectionType = ConnectionType.USB,       FirmwareVersion = "3.4.2", Status = DeviceStatus.Error,        Notes = "Check firmware" },
        });
    }

    public IEnumerable<DeviceModel> GetAll() => _devices.ToList();

    public DeviceModel? GetById(int id) => _devices.FirstOrDefault(d => d.Id == id);

    public void Add(DeviceModel device)
    {
        device.Id = _nextId++;
        _devices.Add(device);
    }

    public void Update(DeviceModel device)
    {
        var existing = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (existing is null) return;
        existing.DeviceName      = device.DeviceName;
        existing.DeviceType      = device.DeviceType;
        existing.ConnectionType  = device.ConnectionType;
        existing.FirmwareVersion = device.FirmwareVersion;
        existing.Status          = device.Status;
        existing.Notes           = device.Notes;
    }

    public void Delete(int id) => _devices.RemoveAll(d => d.Id == id);
}
