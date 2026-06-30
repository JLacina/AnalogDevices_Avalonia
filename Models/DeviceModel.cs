namespace HardwareDeviceConfigManager.Models;

public enum ConnectionType
{
    USB,
    API,
    Simulator
}

public enum DeviceStatus
{
    Connected,
    Disconnected,
    Error
}

public class DeviceModel
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public ConnectionType ConnectionType { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;

    public DeviceModel Clone()
    {
        return new DeviceModel
        {
            Id = Id,
            DeviceName = DeviceName,
            DeviceType = DeviceType,
            ConnectionType = ConnectionType,
            FirmwareVersion = FirmwareVersion,
            Status = Status,
            Notes = Notes
        };
    }
}
