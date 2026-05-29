using Godot;

namespace Godot1;

public partial class CameraFollow : Camera3D
{
    // Offset gives ~51° isometric tilt; Size controls world units visible vertically
    private static readonly Vector3 Offset = new Vector3(0f, 200f, 240f);

    private Node3D? _player;

    public override void _Ready()
    {
        Projection = ProjectionType.Orthogonal;
        Size = 200f;
        _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;
        GlobalPosition = _player.GlobalPosition + Offset;
        LookAt(_player.GlobalPosition, Vector3.Up);
    }
}
