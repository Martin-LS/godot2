using Godot;

namespace Godot1;

public partial class CameraFollow : Camera3D
{
    public override void _Ready()
    {
        Projection = ProjectionType.Perspective;
        Fov = 45f;
        Position = new Vector3(0f, 225f, 130f);
        RotationDegrees = new Vector3(-60f, 0f, 0f);
    }
}
