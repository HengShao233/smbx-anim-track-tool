using Godot;

[Tool]
public partial class TrackExporterPlugin : EditorPlugin
{
    private TrackExporterDock _dock;
    private EditorDock _editorDock;

    public override void _EnterTree()
    {
        if (_editorDock != null)
        {
            return;
        }

        _dock = new TrackExporterDock(EditorInterface.Singleton);
        _dock.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _dock.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        _editorDock = new EditorDock
        {
            Name = "SMBX Track Exporter"
        };
        _editorDock.AddChild(_dock);

        AddDock(_editorDock);
    }

    public override void _ExitTree()
    {
        if (_editorDock != null)
        {
            RemoveDock(_editorDock);
            _editorDock.QueueFree();
            _editorDock = null;
        }

        _dock = null;
    }
}
