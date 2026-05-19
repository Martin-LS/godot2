using Godot;

namespace Godot1.Meta;

public partial class CoinPickup : Area2D
{
    [Export] public int Value = 1;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        QueueRedraw();
    }

    public override void _Draw() => DrawCircle(Vector2.Zero, 5f, Colors.Gold);

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player.PlayerController) return;
        GetParent().GetNodeOrNull<Run.RunSession>("RunSession")?.AddCoin(Value);
        QueueFree();
    }
}
