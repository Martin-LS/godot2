using Godot;
using System.Collections.Generic;
using Godot1.Eot;

namespace Godot1.Enemies;


public partial class EnemyController : CharacterBody3D
{
    private static readonly PackedScene ShardScene =
        GD.Load<PackedScene>("res://src/xp/xp_shard.tscn");
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
    public float PhysicalResistance = 0f;
    public float MagicResistance    = 0f;

    private int _currentHealth;
    private CharacterBody3D? _player;
    private float _damageCooldown;
    private readonly Dictionary<string, EotInstance> _activeEots = new();
    private float _baseSpeed;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _baseSpeed     = Speed;
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
                pc.TakeDamage(ContactDamage, Items.DamageType.Physical);
            _damageCooldown = DamageInterval;
        }

        TickEots((float)delta);
    }

    public void ApplyEot(EotData eot)
    {
        if (_activeEots.TryGetValue(eot.Id, out var existing))
        {
            existing.TimeRemaining = eot.Duration;
            return;
        }
        _activeEots[eot.Id] = new EotInstance
        {
            DefinitionId  = eot.Id,
            TimeRemaining = eot.Duration,
            TickTimer     = eot.TickRate,
        };
        ApplyEotEffect(eot);
    }

    private void TickEots(float delta)
    {
        var expired = new System.Collections.Generic.List<string>();
        foreach (var (id, inst) in _activeEots)
        {
            inst.TimeRemaining -= delta;
            if (inst.TimeRemaining <= 0f)
            {
                expired.Add(id);
                continue;
            }
            var eot = EotRegistry.Get(id);
            if (eot is { IsDamageEot: true })
            {
                inst.TickTimer -= delta;
                if (inst.TickTimer <= 0f)
                {
                    TakeDamage(eot.DamagePerTick, Items.DamageType.Magic);
                    inst.TickTimer = eot.TickRate;
                }
            }
        }
        foreach (var id in expired)
        {
            var eot = EotRegistry.Get(id);
            if (eot != null) RemoveEotEffect(eot);
            _activeEots.Remove(id);
        }
    }

    private void ApplyEotEffect(EotData eot)
    {
        if (eot.Id == "slow")
            Speed = _baseSpeed * (1f - eot.SlowFraction);
    }

    private void RemoveEotEffect(EotData eot)
    {
        if (eot.Id == "slow")
            Speed = _baseSpeed;
    }

    public void TakeDamage(float rawAmount, Items.DamageType type)
    {
        float resistance = type == Items.DamageType.Physical ? PhysicalResistance : MagicResistance;
        float effective  = rawAmount * (1f - resistance);
        _currentHealth  -= Mathf.CeilToInt(effective);
        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        EmitSignal(SignalName.Died, GlobalPosition);

        if (_player is Player.PlayerController pc)
            pc.CollectXp(MapLevel);

        var shard = ShardScene.Instantiate<Xp.XpShard>();
        GetParent().AddChild(shard);
        shard.GlobalPosition = GlobalPosition;

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
