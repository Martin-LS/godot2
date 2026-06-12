using Godot;

namespace Godot1.Weapon;

public partial class WindupTelegraph : Node3D
{
    public float Radius;
    public float Duration;

    private float _elapsed;

    public override void _Ready()
    {
        var disc = new CylinderMesh
        {
            TopRadius      = Radius,
            BottomRadius   = Radius,
            Height         = 20f,
            RadialSegments = 64,
            Rings          = 1,
        };
        var mat = new StandardMaterial3D
        {
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor  = new Color(1f, 0.2f, 0.0f, 0.7f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled,
        };
        var mesh = new MeshInstance3D { Mesh = disc, MaterialOverride = mat };
        AddChild(mesh);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float pulse = 1f + 0.08f * Mathf.Sin(_elapsed / Duration * Mathf.Pi * 6f);
        Scale = new Vector3(pulse, 1f, pulse);
        if (_elapsed >= Duration)
            QueueFree();
    }
}
