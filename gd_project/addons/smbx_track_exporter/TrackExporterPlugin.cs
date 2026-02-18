using Godot;

[Tool]
public partial class TrackExporterPlugin : EditorPlugin
{
    private TrackExporterDock _dock;

    public override void _EnterTree()
    {
        _dock = new TrackExporterDock(GetEditorInterface());
        AddControlToDock(DockSlot.RightUl, _dock);
    }

    public override void _ExitTree()
    {
        if (_dock != null)
        {
            RemoveControlFromDocks(_dock);
            _dock.QueueFree();
            _dock = null;
        }
    }
}
