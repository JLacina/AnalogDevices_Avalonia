// EditingDeviceViewModel — wraps a DeviceModel for editing in the right-panel form.
//
// WHY THIS EXISTS (great MVVM interview talking point):
//   DeviceModel is a pure data POCO. It should have no UI framework dependency —
//   it might be serialized to JSON, passed over a REST API, stored in a database,
//   or unit-tested with no Avalonia context at all.
//
//   This wrapper owns the *observable*, *editable* copy of each field while the
//   user is making changes in the form. When they click Update, CommitTo() writes
//   the values back to the underlying DeviceModel and the repository in one
//   clean, atomic operation.
//
// BONUS CAPABILITIES THIS PATTERN ENABLES:
//   - IsDirty:  compare current values to the original — disable Update button
//               when nothing has changed, or show a "you have unsaved changes" prompt
//   - Revert:   reconstruct the wrapper from the original model — "Cancel" for free
//   - Field-level validation: add [NotifyDataErrorInfo] per field in the future
//   - Audit/diff: show exactly which fields changed before committing

using CommunityToolkit.Mvvm.ComponentModel;
using HardwareDeviceConfigManager.Models;

namespace HardwareDeviceConfigManager.ViewModels;

public partial class EditingDeviceViewModel : ObservableObject
{
    // ── The original model (never mutated until CommitTo is called) ──────────
    private readonly DeviceModel _source;

    // ── Observable editing fields — the right-panel form binds to these ─────
    [ObservableProperty] private string         _deviceName;
    [ObservableProperty] private string         _deviceType;
    [ObservableProperty] private string         _firmwareVersion;
    [ObservableProperty] private string         _notes;
    [ObservableProperty] private ConnectionType _connectionType;
    [ObservableProperty] private DeviceStatus   _status;

    // ── Read-only passthrough — Id never changes while editing ───────────────
    public int Id => _source.Id;

    // ── Constructor: copy model values into the observable fields ────────────
    public EditingDeviceViewModel(DeviceModel source)
    {
        _source          = source;
        _deviceName      = source.DeviceName;
        _deviceType      = source.DeviceType;
        _firmwareVersion = source.FirmwareVersion;
        _notes           = source.Notes;
        _connectionType  = source.ConnectionType;
        _status          = source.Status;
    }

    // ── IsDirty: true when any field differs from the saved model ────────────
    // Use this to drive: UpdateCommand.CanExecute, "unsaved changes" banners,
    // or a confirmation dialog on window close.
    public bool IsDirty =>
        DeviceName      != _source.DeviceName      ||
        DeviceType      != _source.DeviceType      ||
        FirmwareVersion != _source.FirmwareVersion ||
        Notes           != _source.Notes           ||
        ConnectionType  != _source.ConnectionType  ||
        Status          != _source.Status;

    // ── CommitTo: write edited values back to a DeviceModel ──────────────────
    // Called by UpdateDeviceCommand. Keeps all mutation in one place.
    // Accepts a target so it can also be used when creating a new device.
    public void CommitTo(DeviceModel target)
    {
        target.DeviceName      = DeviceName;
        target.DeviceType      = DeviceType;
        target.FirmwareVersion = FirmwareVersion;
        target.Notes           = Notes;
        target.ConnectionType  = ConnectionType;
        target.Status          = Status;
    }

    // ── Revert: reset all fields back to the last saved model state ──────────
    // Call this for a "Cancel" button — no repository round-trip needed.
    public void Revert()
    {
        DeviceName      = _source.DeviceName;
        DeviceType      = _source.DeviceType;
        FirmwareVersion = _source.FirmwareVersion;
        Notes           = _source.Notes;
        ConnectionType  = _source.ConnectionType;
        Status          = _source.Status;
    }
}
