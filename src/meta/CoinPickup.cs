using Godot;

namespace Godot1.Meta;

public partial class CoinPickup : Area3D
{
    [Export] public int Value = 1;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AddChild(new MeshInstance3D
        {
            Mesh             = new BoxMesh { Size = new Vector3(10f, 10f, 10f) },
            Position         = new Vector3(0f, 5f, 0f),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.85f, 0.1f) },
        });
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Player.PlayerController) return;
        GetParent().GetNodeOrNull<Run.RunSession>("RunSession")?.AddCoin(Value);
        QueueFree();
    }
}
