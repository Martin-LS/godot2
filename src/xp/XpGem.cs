using Godot;

namespace Godot1.Xp;

public partial class XpGem : Area2D
{
    [Export] public int Value = 5;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 6f, Colors.LimeGreen);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player.PlayerController pc)
        {
            pc.CollectXp(Value);
            QueueFree();
        }
    }
}
