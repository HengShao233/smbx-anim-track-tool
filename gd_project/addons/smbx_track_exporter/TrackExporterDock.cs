#nullable enable
using Godot;

namespace gd_project.addons.smbx_track_exporter;

[Tool]
// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class TrackExporterDock : VBoxContainer
{
    private readonly EditorInterface _editor;
    private readonly TrackExporterConfig _config = new();

    private AnimationPlayer? _player;
    private readonly Label _playerLabel = new();
    private readonly OptionButton _animationList = new();
    private readonly LineEdit _animPrefix = new();
    private readonly LineEdit _animTemplate = new();
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
        
        var nameBar = new HBoxContainer();
        nameBar.AddChild(new Label { Text = "动画名前缀: " });
        _animPrefix.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _animPrefix.Text = GetSettingPrefix();
        _animPrefix.TextSubmitted += _ =>
        {
            SaveSettingPrefix(_animPrefix.Text);
            _config.Save();
        };
        nameBar.AddChild(_animPrefix);
        AddChild(nameBar);

        var selectBar = new HBoxContainer();
        var selectButton = new Button { Text = "使用当前选中的 AnimationPlayer" };
        selectButton.Pressed += UseSelectedAnimationPlayer;
        selectBar.AddChild(selectButton);

        _playerLabel.Text = "未选择";
        _playerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        selectBar.AddChild(_playerLabel);
        AddChild(selectBar);

        var animBar = new HBoxContainer();
        animBar.AddChild(new Label { Text = "动画: " });
        _animationList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        animBar.AddChild(_animationList);
        var refreshButton = new Button { Text = "刷新轨道" };
        refreshButton.Pressed += () => RefreshTracks();
        animBar.AddChild(refreshButton);
        AddChild(animBar);
        
        var templateBar = new HBoxContainer();
        templateBar.AddChild(new Label { Text = "配置模板: " });
        _animTemplate.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        templateBar.AddChild(_animTemplate);
        AddChild(templateBar);

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

        _trackTree.Columns = 5;
        _trackTree.HideRoot = true;
        _trackTree.SetColumnExpand(0, true);
        _trackTree.SetColumnExpand(1, false);
        _trackTree.SetColumnExpand(2, true);
        _trackTree.SetColumnExpand(3, true);
        _trackTree.SetColumnExpand(4, true);
        _trackTree.SetColumnCustomMinimumWidth(0, 140);
        _trackTree.SetColumnCustomMinimumWidth(1, 0);
        _trackTree.SetColumnCustomMinimumWidth(2, 50);
        _trackTree.SetColumnCustomMinimumWidth(3, 50);
        _trackTree.SetColumnCustomMinimumWidth(4, 50);
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
        _fileDialog.Filters = ["*.smt"];
        _fileDialog.FileSelected += path =>
        {
            _exportPath.Text = path;
            var animData = GetAnimationConfig();
            animData["@export_path@"] = path;
        };
        AddChild(_fileDialog);

        // 初始化保存的配置
        {
            var animData = GetAnimationConfig();
            if (animData.TryGetValue("@export_path@", out var path)) _exportPath.Text = path.ToString();
        }
    }

    private void WireSignals()
    {
        _animTemplate.TextSubmitted += OnTemplateEdited;
        _trackTree.ItemEdited += OnTrackItemEdited;
        _animationList.ItemSelected += _ => RefreshTracks();
    }

    private void UseSelectedAnimationPlayer()
    {
        var selection = _editor.GetSelection();
        var selected = selection.GetSelectedNodes();
        foreach (var node in selected)
        {
            if (node is not AnimationPlayer ap) continue;

            _player = ap;
            _playerLabel.Text = ap.Name;
            _playerLabel.TooltipText = ap.GetPath();
            RefreshAnimationList();
            return;
        }
        _statusLabel.Text = "⚠ 请在场景树中选中一个 AnimationPlayer";
    }

    private void RefreshAnimationList()
    {
        _animPrefix.Text = GetSettingPrefix();
        _animationList.Clear();
        if (_player == null)
        {
            _statusLabel.Text = "⚠ 未找到 AnimationPlayer";
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

    private void RefreshTracks(bool updateTemplate = true)
    {
        _trackTree.Clear();
        var root = _trackTree.CreateItem();

        if (_player == null || _animationList.ItemCount == 0)
        {
            _statusLabel.Text = "⚠ 请选择 AnimationPlayer 和动画";
            return;
        }

        var animName = _animationList.GetItemText(_animationList.Selected);
        if (updateTemplate)
        {
            _animTemplate.Text = GetSettingTemplateName(animName, true);
        }
        var animation = _player.GetAnimation(animName);
        if (animation == null)
        {
            _statusLabel.Text = $"⚠ 动画不存在, {animName}";
            return;
        }

        var titleItem = _trackTree.CreateItem(root);
        var idx = 0;
        titleItem.SetText(idx++, "轨道");
        titleItem.SetText(idx++, "类型");
        titleItem.SetText(idx++, "索引");
        titleItem.SetText(idx++, "乘数");
        titleItem.SetText(idx, "模板");

        var trackCount = animation.GetTrackCount();
        for (var i = 0; i < trackCount; i++)
        {
            var item = _trackTree.CreateItem(root);
            var srcPath = animation.TrackGetPath(i);
            var name = srcPath.GetName(srcPath.GetNameCount() - 1);
            var path = srcPath.ToString();
            var type = animation.TrackGetType(i).ToString();

            var settings = GetSettings(animName, path);

            item.SetChecked(0, true);
            var idxInner = 0;
            item.SetText(idxInner++, $"{name}({path})");
            item.SetText(idxInner++, type);
            item.SetText(idxInner++, settings.Idx.ToString());
            item.SetText(idxInner++, settings.Multiplier);
            item.SetText(idxInner, settings.Template);

            item.SetEditable(2, true);
            item.SetEditable(3, true);
            item.SetEditable(4, true);

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

        var idx = ParseInt(item.GetText(2), settings.Idx);
        if (idx >= 64)
        {
            idx = 63;
            item.SetText(2, "63");
            _statusLabel.Text = $"⚠ 轨道数不允许超过 64 条, {animName}";
        }
        _statusLabel.Text = "";
        
        settings.Idx = idx;
        settings.Multiplier = item.GetText(3);
        settings.Template = item.GetText(4);
        
        SaveSettings(animName, path, settings);
        _config.Save();
    }

    private void OnTemplateEdited(string _)
    {
        var animName = _animationList.GetItemText(_animationList.Selected);
        SaveSettingTemplate(animName, _animTemplate.GetText());
        RefreshTracks(false);
        _config.Save();
    }

    private TrackSettings GetSettings(string animName, string trackPath)
    {
        animName = GetSettingTemplateName(animName);
        trackPath = $"@track_settings@{trackPath}";

        var animations = GetAnimationConfig();
        if (!animations.ContainsKey(animName))
        {
            animations[animName] = new Godot.Collections.Dictionary();
        }
        var animDict = (Godot.Collections.Dictionary)animations[animName];
        if (!animDict.ContainsKey(trackPath))
        {
            var data = TrackSettings.ToDictionary();
            if (data == null) return TrackSettings.FromDictionary(null);
            animDict[trackPath] = data;
        }
        var dict = (Godot.Collections.Dictionary)animDict[trackPath];
        return TrackSettings.FromDictionary(dict);
    }

    private string GetSettingTemplateName(string animName, bool pure = false)
    {
        var animations = GetAnimationConfig();
        var srcName = $"@src_settings@{animName}";
        if (!animations.ContainsKey(srcName))
        {
            animations[srcName] = new Godot.Collections.Dictionary();
        }
        var animDict = (Godot.Collections.Dictionary)animations[srcName];
        return !animDict.TryGetValue("@template@", out var templateNameVar)
            ? pure ? "" : srcName
            : pure ? templateNameVar.ToString() : $"@template@{templateNameVar.ToString()}";
    }

    private void SaveSettingTemplate(string animName, string? templateName)
    {
        animName = $"@src_settings@{animName}";
        var animations = GetAnimationConfig();
        if (!animations.ContainsKey(animName))
        {
            animations[animName] = new Godot.Collections.Dictionary();
        }
        var animDict = (Godot.Collections.Dictionary)animations[animName];
        if (string.IsNullOrWhiteSpace(templateName)) animDict.Remove("@template@");
        else animDict["@template@"] = $"{templateName}";
    }
    
    private string GetSettingPrefix()
    {
        var animName = _player?.Name.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(animName)) return "";
        
        var animations = GetAnimationConfig();
        var key = $"@prefix@{animName}";
        return animations.TryGetValue(key, out var value) ? value.ToString() : "";
    }
    
    private void SaveSettingPrefix(string? prefixName)
    {
        var animName = _player?.Name.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(animName)) return;
        
        var animations = GetAnimationConfig();
        var key = $"@prefix@{animName}";
        prefixName = prefixName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(prefixName)) animations.Remove(key);
        else animations[key] = $"{prefixName}";
    }

    private void SaveSettings(string animName, string trackPath, in TrackSettings settings)
    {
        animName = GetSettingTemplateName(animName);
        trackPath = $"@track_settings@{trackPath}";
        
        var animations = GetAnimationConfig();
        if (!animations.ContainsKey(animName))
        {
            animations[animName] = new Godot.Collections.Dictionary();
        }
        var animDict = (Godot.Collections.Dictionary)animations[animName];
        var data = TrackSettings.ToDictionary(settings);
        if (data != null) animDict[trackPath] = data;
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
}