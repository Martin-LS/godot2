using Godot;

namespace Godot1.Enemies;


public partial class EnemyController : CharacterBody2D
{
    private static readonly PackedScene GemScene =
        GD.Load<PackedScene>("res://src/xp/xp_gem.tscn");
    private static readonly PackedScene CoinScene =
        GD.Load<PackedScene>("res://src/meta/coin_pickup.tscn");

    [Signal] public delegate void DiedEventHandler(Vector2 position);

    [Export] public float Speed = 80f;
    [Export] public int MaxHealth = 30;
    [Export] public int ContactDamage = 10;
    [Export] public float DamageInterval = 1f;

    private int _currentHealth;
    private CharacterBody2D? _player;
    private float _damageCooldown;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
        AddToGroup("enemies");
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 14f, Colors.Tomato);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;

        var direction = (_player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * Speed;
        MoveAndSlide();

        _damageCooldown -= (float)delta;
        if (_damageCooldown <= 0f && GlobalPosition.DistanceTo(_player.GlobalPosition) < 32f)
        {
            if (_player is Godot1.Player.PlayerController pc)
                pc.TakeDamage(ContactDamage);
            _damageCooldown = DamageInterval;
        }
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        EmitSignal(SignalName.Died, GlobalPosition);

        var gem = GemScene.Instantiate<Xp.XpGem>();
        gem.GlobalPosition = GlobalPosition;
        GetParent().AddChild(gem);

        if (GD.Randf() < 0.25f)
        {
            var coin = CoinScene.Instantiate<Meta.CoinPickup>();
            coin.GlobalPosition = GlobalPosition;
            GetParent().AddChild(coin);
        }

        QueueFree();
    }
}
