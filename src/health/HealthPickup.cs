using Godot;

namespace Godot1.Health;

public partial class HealthPickup : Area3D
{
    [Export] public int HealAmount = 15;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AddChild(new MeshInstance3D
        {
            Mesh             = new BoxMesh { Size = new Vector3(10f, 10f, 10f) },
            Position         = new Vector3(0f, 5f, 0f),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.15f, 0.15f) },
        });
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Player.PlayerController pc) return;
        pc.Heal(HealAmount);
        QueueFree();
    }
}
