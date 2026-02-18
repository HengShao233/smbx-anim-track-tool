using Godot;


public partial class TrackExportConfig : RefCounted
{
    private const string ConfigPath = "user://smbx_track_exporter_config.json";

    public Godot.Collections.Dictionary Data { get; private set; } = new();


    public void Load()
    {
        if (!FileAccess.FileExists(ConfigPath))
        {
            Data = new Godot.Collections.Dictionary();
            return;
        }


        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        var jsonText = file.GetAsText();
        var parsed = Json.ParseString(jsonText);
        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            Data = parsed.AsGodotDictionary();
        }
        else
        {
            Data = new Godot.Collections.Dictionary();
        }

    }

    public void Save()
    {
        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        file.StoreString(Json.Stringify(Data, "  "));
    }
}
