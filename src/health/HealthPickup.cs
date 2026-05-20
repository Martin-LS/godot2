using Godot;

namespace Godot1.Health;

public partial class HealthPickup : Area2D
{
    [Export] public int HealAmount = 15;

    private static readonly Texture2D ItemTex =
        GD.Load<Texture2D>("res://assets/kenney_topdown_rpg/Roguelike Base Pack/Spritesheet/roguelikeSheet_transparent.png");

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AddChild(new Sprite2D
        {
            Texture       = ItemTex,
            RegionEnabled = true,
            RegionRect    = new Rect2(510, 153, 16, 16),
            Scale         = new Vector2(1.5f, 1.5f)
        });
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player.PlayerController pc) return;
        pc.Heal(HealAmount);
        QueueFree();
    }
}
