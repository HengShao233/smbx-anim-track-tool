#nullable enable
using Godot;

namespace gd_project.Utils;

/// <summary>
/// 节点连接线,连接两个 Node2D 节点,并在编辑器状态下实时更新位置
/// 使用方法:
/// 1. 在场景中添加此节点作为子节点
/// 2. 设置 StartNodePath 和 EndNodePath 属性指向要连接的节点
/// </summary>
[Tool]
public partial class NodeConnectionLine : Line2D
{
    [ExportCategory("连接设置")]
    [Export]
    public NodePath? StartNodePath { get; set; }

    [Export]
    public NodePath? EndNodePath { get; set; }

    private Node2D? _startNode;
    private Node2D? _endNode;

    private Callable _onStartNodeExiting;
    private Callable _onEndNodeExiting;

    public override void _EnterTree()
    {
        base._EnterTree();
        GD.Print($"connection line node enter tree : {Name} ");
    }

    public override void _Ready()
    {
        Points = new Vector2[2];
        ConnectNodes();
        UpdateLinePosition();

        GD.Print($"connection line node ready : {Name} ");

        _onStartNodeExiting = Callable.From(OnStartNodeExiting);
        _onEndNodeExiting = Callable.From(OnEndNodeExiting);
    }

    public override void _Process(double delta)
    {
        // 在编辑器模式下也需要更新
        if (Points is not { Length: 2 }) Points = new Vector2[2];
        UpdateLinePosition();
    }

    private void ConnectNodes()
    {
        if (StartNodePath != null)
        {
            _startNode = GetNodeOrNull<Node2D>(StartNodePath);
            if (_startNode != null)
            {
                // 监听节点变化
                if (
                    _onStartNodeExiting.Target != null &&
                    !_startNode.IsConnected("tree_exiting", _onStartNodeExiting)
                ) {
                    _startNode.Connect("tree_exiting", _onStartNodeExiting);
                }
            }
        }

        if (EndNodePath != null)
        {
            _endNode = GetNodeOrNull<Node2D>(EndNodePath);
            if (
                _onStartNodeExiting.Target != null &&
                !_endNode.IsConnected("tree_exiting", _onEndNodeExiting)
            ) {
                _endNode.Connect("tree_exiting", _onEndNodeExiting);
            }
        }
    }

    private void OnStartNodeExiting()
    {
        if (_startNode == null) return;
        _startNode.TreeExiting -= OnStartNodeExiting;
        _startNode = null;
    }

    private void OnEndNodeExiting()
    {
        if (_endNode == null) return;
        _endNode.TreeExiting -= OnEndNodeExiting;
        _endNode = null;
    }

    private void UpdateLinePosition()
    {
        if ((_startNode == null && StartNodePath != null) || (_endNode == null && EndNodePath != null))
            ConnectNodes();

        if (_startNode == null || _endNode == null)
        {
            ClearPoints();
            return;
        }

        // 将全局坐标转换为相对于 Line2D 的局部坐标
        var localStart = ToLocal(_startNode.GlobalPosition);
        var localEnd = ToLocal(_endNode.GlobalPosition);

        // 更新线条端点
        if (GetPointCount() >= 2)
        {
            SetPointPosition(0, localStart);
            SetPointPosition(1, localEnd);
        }
        else
        {
            ClearPoints();
            AddPoint(localStart);
            AddPoint(localEnd);
        }

        QueueRedraw();
    }

    // 重新连接节点(当节点路径改变时调用)
    public void Reconnect()
    {
        if (_startNode != null)
        {
            _startNode.TreeExiting -= OnStartNodeExiting;
        }
        if (_endNode != null)
        {
            _endNode.TreeExiting -= OnEndNodeExiting;
        }
        _startNode = null;
        _endNode = null;
        ConnectNodes();
        UpdateLinePosition();
    }
}