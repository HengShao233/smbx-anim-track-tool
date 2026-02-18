using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsciiBinary;
using Godot;


[Tool]
public partial class TrackExporterDock : VBoxContainer
{
    private readonly EditorInterface _editor;
    private readonly TrackExportConfig _config = new();

    private AnimationPlayer _player;
    private readonly Label _playerLabel = new();
    private readonly OptionButton _animationList = new();
    private readonly Tree _trackTree = new();
    private readonly ScrollContainer _scroll = new();
    private readonly VBoxContainer _scrollContent = new();
    private readonly LineEdit _exportPath = new();
    private readonly FileDialog _fileDialog = new();
    private readonly Label _statusLabel = new();


    public TrackExporterDock(EditorInterface editor)
    {
        _editor = editor;
        _config.Load();
        Name = "SMBX Track Exporter";

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        CustomMinimumSize = Vector2.Zero;

        BuildUi();
        WireSignals();
    }


    private void BuildUi()
    {
        var header = new Label { Text = "SMBX 轨道动画导出器" };
        AddChild(header);

        var selectBar = new HBoxContainer();
        var selectButton = new Button { Text = "使用当前选中的 AnimationPlayer" };
        selectButton.Pressed += UseSelectedAnimationPlayer;
        selectBar.AddChild(selectButton);

        _playerLabel.Text = "未选择";
        _playerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        selectBar.AddChild(_playerLabel);
        AddChild(selectBar);

        var animBar = new HBoxContainer();
        animBar.AddChild(new Label { Text = "动画：" });
        _animationList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        animBar.AddChild(_animationList);
        var refreshButton = new Button { Text = "刷新轨道" };
        refreshButton.Pressed += RefreshTracks;
        animBar.AddChild(refreshButton);
        AddChild(animBar);

        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.CustomMinimumSize = Vector2.Zero;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        _scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        AddChild(_scroll);

        _scrollContent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scrollContent.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scrollContent.CustomMinimumSize = Vector2.Zero;
        _scroll.AddChild(_scrollContent);

        _trackTree.Columns = 7;
        _trackTree.HideRoot = true;
        _trackTree.SetColumnTitle(0, "轨道");
        _trackTree.SetColumnTitle(1, "类型");
        _trackTree.SetColumnTitle(2, "Idx");
        _trackTree.SetColumnTitle(3, "乘数");
        _trackTree.SetColumnTitle(4, "内加");
        _trackTree.SetColumnTitle(5, "外加");
        _trackTree.SetColumnTitle(6, "键数");
        _trackTree.SetColumnExpand(0, true);
        _trackTree.SetColumnExpand(1, true);
        _trackTree.SetColumnExpand(2, true);
        _trackTree.SetColumnExpand(3, true);
        _trackTree.SetColumnExpand(4, true);
        _trackTree.SetColumnExpand(5, true);
        _trackTree.SetColumnExpand(6, true);
        _trackTree.SetColumnCustomMinimumWidth(0, 140);
        _trackTree.SetColumnCustomMinimumWidth(1, 60);
        _trackTree.SetColumnCustomMinimumWidth(2, 50);
        _trackTree.SetColumnCustomMinimumWidth(3, 50);
        _trackTree.SetColumnCustomMinimumWidth(4, 50);
        _trackTree.SetColumnCustomMinimumWidth(5, 50);
        _trackTree.SetColumnCustomMinimumWidth(6, 50);
        _trackTree.CustomMinimumSize = Vector2.Zero;
        _trackTree.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _trackTree.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scrollContent.AddChild(_trackTree);


        var exportBar = new HBoxContainer();
        exportBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exportBar.AddChild(new Label { Text = "导出路径：" });
        _exportPath.PlaceholderText = "选择 .smt 导出路径";
        _exportPath.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exportBar.AddChild(_exportPath);
        var browseButton = new Button { Text = "浏览" };
        browseButton.Pressed += () => _fileDialog.PopupCenteredRatio(0.7f);
        exportBar.AddChild(browseButton);
        _scrollContent.AddChild(exportBar);

        var exportButton = new Button { Text = "导出 .smt" };
        exportButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exportButton.Pressed += Export;
        _scrollContent.AddChild(exportButton);

        _statusLabel.Text = "";
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scrollContent.AddChild(_statusLabel);


        _fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.Filters = new string[] { "*.smt" };
        _fileDialog.FileSelected += path => _exportPath.Text = path;
        AddChild(_fileDialog);

    }

    private void WireSignals()
    {
        _trackTree.ItemEdited += OnTrackItemEdited;
        _animationList.ItemSelected += _ => RefreshTracks();
    }

    private void UseSelectedAnimationPlayer()
    {
        var selection = _editor.GetSelection();
        var selected = selection.GetSelectedNodes();
        foreach (var node in selected)
        {
            if (node is AnimationPlayer ap)
            {
                _player = ap;
                _playerLabel.Text = ap.Name;
                _playerLabel.TooltipText = ap.GetPath();
                RefreshAnimationList();
                return;

            }
        }

        _statusLabel.Text = "请在场景树中选中一个 AnimationPlayer。";
    }

    private void RefreshAnimationList()
    {
        _animationList.Clear();
        if (_player == null)
        {
            _statusLabel.Text = "未找到 AnimationPlayer。";
            return;
        }

        var animations = _player.GetAnimationList();
        foreach (var name in animations)
        {
            _animationList.AddItem(name);
        }

        if (animations.Length > 0)
        {
            _animationList.Select(0);
            RefreshTracks();
        }

    }

    private void RefreshTracks()
    {
        _trackTree.Clear();
        var root = _trackTree.CreateItem();

        if (_player == null || _animationList.ItemCount == 0)
        {
            _statusLabel.Text = "请选择 AnimationPlayer 和动画。";
            return;
        }

        var animName = _animationList.GetItemText(_animationList.Selected);
        var animation = _player.GetAnimation(animName);
        if (animation == null)
        {
            _statusLabel.Text = "动画不存在。";
            return;
        }

        var trackCount = animation.GetTrackCount();
        for (var i = 0; i < trackCount; i++)
        {
            var item = _trackTree.CreateItem(root);
            var path = animation.TrackGetPath(i).ToString();
            var type = animation.TrackGetType(i).ToString();
            var keyCount = animation.TrackGetKeyCount(i);

            var settings = GetSettings(animName, path);

            item.SetText(0, path);
            item.SetText(1, type);
            item.SetText(2, settings.Idx.ToString());
            item.SetText(3, settings.Multiplier.ToString());
            item.SetText(4, settings.InnerAdd.ToString());
            item.SetText(5, settings.OuterAdd.ToString());
            item.SetText(6, keyCount.ToString());

            item.SetEditable(2, true);
            item.SetEditable(3, true);
            item.SetEditable(4, true);
            item.SetEditable(5, true);

            item.SetMetadata(0, path);
        }

        _statusLabel.Text = $"已加载 {trackCount} 条轨道。";
    }

    private void OnTrackItemEdited()
    {
        var item = _trackTree.GetEdited();
        if (item == null || _animationList.ItemCount == 0) return;

        var animName = _animationList.GetItemText(_animationList.Selected);
        var path = item.GetMetadata(0).AsString();
        var settings = GetSettings(animName, path);

        settings.Idx = ParseInt(item.GetText(2), settings.Idx);
        settings.Multiplier = ParseInt(item.GetText(3), settings.Multiplier);
        settings.InnerAdd = ParseInt(item.GetText(4), settings.InnerAdd);
        settings.OuterAdd = ParseInt(item.GetText(5), settings.OuterAdd);

        SaveSettings(animName, path, settings);
        _config.Save();
    }

    private void Export()
    {
        if (_player == null || _animationList.ItemCount == 0)
        {
            _statusLabel.Text = "请先选择 AnimationPlayer 与动画。";
            return;
        }

        var outputPath = _exportPath.Text;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            _statusLabel.Text = "请选择导出路径。";
            return;
        }

        var animName = _animationList.GetItemText(_animationList.Selected);
        var animation = _player.GetAnimation(animName);
        if (animation == null)
        {
            _statusLabel.Text = "动画不存在。";
            return;
        }

        var warnings = new List<string>();
        var encoded = ExportAnimation(animation, animName, warnings);
        if (encoded == null)
        {
            _statusLabel.Text = "导出失败：请检查轨道设置。";
            return;
        }

        var script = BuildSmtScript(animName, encoded);
        using var file = FileAccess.Open(outputPath, FileAccess.ModeFlags.Write);
        file.StoreString(script);

        _statusLabel.Text = warnings.Count == 0 ? "导出完成。" : "导出完成（存在警告）。";
        if (warnings.Count > 0)
        {
            GD.Print("[SMBX Exporter] Warnings:\n" + string.Join("\n", warnings));
        }
    }

    private byte[] ExportAnimation(Animation animation, string animName, List<string> warnings)
    {
        var trackCount = animation.GetTrackCount();
        var tracks = new SortedDictionary<int, List<ushort>>();

        var fps = animation.Step > 0 ? Mathf.RoundToInt(1f / animation.Step) : 60;
        fps = Mathf.Clamp(fps, 1, 240);
        var totalFrames = Mathf.Clamp(Mathf.RoundToInt((float)animation.Length * fps), 0, 4095);

        for (var i = 0; i < trackCount; i++)
        {
            if (animation.TrackGetType(i) != Animation.TrackType.Value)
            {
                warnings.Add($"跳过非 Value 轨道: {animation.TrackGetPath(i)}");
                continue;
            }

            var path = animation.TrackGetPath(i).ToString();
            var settings = GetSettings(animName, path);
            if (settings.Idx < 0)
            {
                warnings.Add($"轨道未设置 idx，已跳过: {path}");
                continue;
            }

            var keyCount = animation.TrackGetKeyCount(i);
            if (keyCount == 0)
            {
                warnings.Add($"轨道无关键帧，已跳过: {path}");
                continue;
            }

            var data = new List<ushort>();
            var usedKeys = 0;

            for (var k = 0; k < keyCount; k++)
            {
                var keyValue = animation.TrackGetKeyValue(i, k);
                if (!TryGetNumber(keyValue, out var value))
                {
                    warnings.Add($"轨道关键帧非数值，已跳过: {path} @ {k}");
                    continue;
                }

                var time = animation.TrackGetKeyTime(i, k);
                var frame = Mathf.Clamp(Mathf.RoundToInt((float)time * fps), 1, 4096);
                var interp = MapInterpolation(animation.TrackGetInterpolationType(i));
                var keySetting = EncodeKeyframeSetting(interp, frame);

                var stored = ComputeStoredValue(value, settings);
                var valueEncoded = EncodeInt(stored, warnings, path);

                data.Add(keySetting);
                data.Add(valueEncoded);
                usedKeys++;
            }

            if (usedKeys == 0)
            {
                warnings.Add($"轨道未产生有效关键帧，已跳过: {path}");
                continue;
            }

            var header = new List<ushort>
            {
                EncodeInt(totalFrames, warnings, path),
                EncodeInt(fps, warnings, path),
                EncodeInt(usedKeys, warnings, path),
                EncodeInt(settings.Multiplier, warnings, path),
                EncodeInt(settings.InnerAdd, warnings, path),
                EncodeInt(settings.OuterAdd, warnings, path)
            };
            header.AddRange(data);

            tracks[settings.Idx] = header;
        }

        if (tracks.Count == 0)
        {
            warnings.Add("没有可导出的轨道。");
            return null;
        }

        var body = new List<ushort>();
        var offsets = new List<ushort>();
        foreach (var kv in tracks)
        {
            offsets.Add((ushort)body.Count);
            body.AddRange(kv.Value);
        }

        var finalData = new List<ushort>
        {
            EncodeInt(tracks.Count, warnings, "")
        };
        finalData.AddRange(offsets.Select(o => EncodeInt(o, warnings, "")));
        finalData.AddRange(body);

        var ulongs = finalData.Select(v => (ulong)v).ToArray();
        System.ReadOnlySpan<ulong> sp = ulongs;
        return AscBin.Encode(sp);



    }

    private string BuildSmtScript(string animName, byte[] encoded)
    {
        var ascii = Encoding.ASCII.GetString(encoded);
        var sb = new StringBuilder();
        sb.AppendLine("' SMBX Track Script (generated by Godot)");
        sb.AppendLine($"' Animation: {animName}");
        sb.AppendLine("' Paste TRACK_DATA into your TeaScript loader");
        sb.AppendLine("Const TRACK_DATA = \"" + ascii + "\"");
        return sb.ToString();
    }

    private static bool TryGetNumber(Variant value, out double result)
    {
        switch (value.VariantType)
        {
            case Variant.Type.Int:
                result = value.AsInt64();
                return true;
            case Variant.Type.Float:
                result = value.AsDouble();
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static int ComputeStoredValue(double value, TrackSettings settings)
    {
        if (settings.Multiplier == 0) return 0;
        var raw = (value - settings.OuterAdd) / settings.Multiplier - settings.InnerAdd;
        return (int)Math.Round(raw);
    }

    private static ushort EncodeInt(int value, List<string> warnings, string path)
    {
        var raw = value + 1600;
        if (raw < 0 || raw > 16383)
        {
            warnings.Add($"数值超出范围并被截断: {path} -> {value}");
            raw = Mathf.Clamp(raw, 0, 16383);
        }
        return (ushort)(raw & 0x3FFF);
    }

    private static ushort EncodeKeyframeSetting(int interp, int frame)
    {
        var mode = Mathf.Clamp(interp, 0, 3);
        var f = Mathf.Clamp(frame - 1, 0, 4095);
        return (ushort)((0b11 << 14) | (mode << 12) | f);
    }

    private static int MapInterpolation(Animation.InterpolationType type)
    {
        return type switch
        {
            Animation.InterpolationType.Nearest => 0,
            Animation.InterpolationType.Linear => 1,
            Animation.InterpolationType.LinearAngle => 1,
            Animation.InterpolationType.Cubic => 3,
            Animation.InterpolationType.CubicAngle => 3,
            _ => 1
        };
    }

    private TrackSettings GetSettings(string animName, string trackPath)
    {
        var animations = GetAnimationConfig();
        if (!animations.ContainsKey(animName))
        {
            animations[animName] = new Godot.Collections.Dictionary();
        }

        var animDict = (Godot.Collections.Dictionary)animations[animName];
        if (!animDict.ContainsKey(trackPath))
        {
            animDict[trackPath] = TrackSettings.ToDictionary(new TrackSettings());
        }

        var dict = (Godot.Collections.Dictionary)animDict[trackPath];

        return TrackSettings.FromDictionary(dict);
    }

    private void SaveSettings(string animName, string trackPath, TrackSettings settings)
    {
        var animations = GetAnimationConfig();
        if (!animations.ContainsKey(animName))
        {
            animations[animName] = new Godot.Collections.Dictionary();
        }
        var animDict = (Godot.Collections.Dictionary)animations[animName];
        animDict[trackPath] = TrackSettings.ToDictionary(settings);

    }

    private Godot.Collections.Dictionary GetAnimationConfig()
    {
        if (!_config.Data.ContainsKey("animations"))
        {
            _config.Data["animations"] = new Godot.Collections.Dictionary();
        }
        return (Godot.Collections.Dictionary)_config.Data["animations"];
    }


    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, out var v) ? v : fallback;
    }

    private class TrackSettings
    {
        public int Idx = -1;
        public int Multiplier = 1;
        public int InnerAdd = 0;
        public int OuterAdd = 0;

        public static TrackSettings FromDictionary(Godot.Collections.Dictionary dict)
        {
            return new TrackSettings
            {
                Idx = GetInt(dict, "idx", -1),
                Multiplier = GetInt(dict, "mult", 1),
                InnerAdd = GetInt(dict, "inner", 0),
                OuterAdd = GetInt(dict, "outer", 0)
            };
        }

        public static Godot.Collections.Dictionary ToDictionary(TrackSettings settings)
        {
            return new Godot.Collections.Dictionary
            {
                { "idx", settings.Idx },
                { "mult", settings.Multiplier },
                { "inner", settings.InnerAdd },
                { "outer", settings.OuterAdd }
            };
        }

        private static int GetInt(Godot.Collections.Dictionary dict, string key, int fallback)
        {
            return dict.ContainsKey(key) ? (int)dict[key] : fallback;
        }

    }
}
