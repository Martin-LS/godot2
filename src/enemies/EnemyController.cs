using Godot;

namespace Godot1.Enemies;


public partial class EnemyController : CharacterBody3D
{
    private static readonly PackedScene GemScene =
        GD.Load<PackedScene>("res://src/xp/xp_gem.tscn");
    private static readonly PackedScene CoinScene =
        GD.Load<PackedScene>("res://src/meta/coin_pickup.tscn");
    private static readonly PackedScene HealthScene =
        GD.Load<PackedScene>("res://src/health/health_pickup.tscn");
    [Signal] public delegate void DiedEventHandler(Vector3 position);

    [Export] public float Speed = 160f;
    [Export] public int MaxHealth = 1;
    [Export] public int ContactDamage = 10;
    [Export] public float DamageInterval = 1f;
    public int MapLevel = 1;

    private int _currentHealth;
    private CharacterBody3D? _player;
    private float _damageCooldown;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody3D;
        AddToGroup("enemies");
        var enemyModel = GD.Load<PackedScene>("res://assets/models/enemies/Skeleton_Minion.glb").Instantiate<Node3D>();
        enemyModel.Scale = new Vector3(10f, 10f, 10f);
        AddChild(enemyModel);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;

        var diff = _player.GlobalPosition - GlobalPosition;
        var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();
        Velocity = direction * Speed;
        MoveAndSlide();

        if (direction.LengthSquared() > 0.01f)
            LookAt(GlobalPosition + direction, Vector3.Up);

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

        if (_player is Player.PlayerController pc)
            pc.CollectXp(MapLevel);

        var gem = GemScene.Instantiate<Xp.XpGem>();
        GetParent().AddChild(gem);
        gem.GlobalPosition = GlobalPosition;

        if (GD.Randf() < 0.25f)
        {
            var coin = CoinScene.Instantiate<Meta.CoinPickup>();
            GetParent().AddChild(coin);
            coin.GlobalPosition = GlobalPosition;
        }

        if (GD.Randf() < 0.10f)
        {
            var hp = HealthScene.Instantiate<Health.HealthPickup>();
            GetParent().AddChild(hp);
            hp.GlobalPosition = GlobalPosition;
        }

        if (GD.Randf() < 0.20f)
        {
            var session = GetParent().GetNodeOrNull<Run.RunSession>("RunSession");
            session?.AddCraftingCurrency1(1);
        }

        QueueFree();
    }
}
