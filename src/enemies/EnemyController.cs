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
    [Signal] public delegate void DamageTakenEventHandler(float effectiveDamage, bool isMagic, bool isCrit);

    [Export] public float Speed = 160f;
    [Export] public int MaxHealth = 1;
    [Export] public int ContactDamage = 10;
    [Export] public float DamageInterval = 1f;
    public int MapLevel = 1;
    public float PhysicalResistance = 0f;
    public float MagicResistance    = 0f;
    public string ModelPath = "res://assets/models/characters/enemy_generic.glb";

    private int _currentHealth;
    public  int CurrentHealth => _currentHealth;
    private CharacterBody3D? _player;
    private float _damageCooldown;
    private readonly Dictionary<string, EotInstance> _activeEots = new();
    private float _baseSpeed;
    private AnimationNodeStateMachinePlayback? _smPlayback;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _baseSpeed     = Speed;
        _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody3D;
        AddToGroup("enemies");
        var enemyModel = GD.Load<PackedScene>(ModelPath).Instantiate<Node3D>();
        enemyModel.Scale = new Vector3(9f, 9f, 9f);
        AddChild(enemyModel);

        var animPlayer = enemyModel.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (animPlayer == null)
        {
            animPlayer = new AnimationPlayer();
            enemyModel.AddChild(animPlayer);
        }
        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_general.glb",  "Idle_A",               "idle",   Animation.LoopModeEnum.Linear);
        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_movement.glb", "Running_A",            "walk",   Animation.LoopModeEnum.Linear);
        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_melee.glb",    "Melee_1H_Attack_Chop", "attack", Animation.LoopModeEnum.None);

        var animTree = GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree != null)
        {
            animTree.AnimPlayer = animTree.GetPathTo(animPlayer);
            animTree.Active = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_smPlayback == null)
        {
            var at = GetNodeOrNull<AnimationTree>("AnimationTree");
            if (at != null)
                _smPlayback = at.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        }

        if (_player == null) return;

        var diff = _player.GlobalPosition - GlobalPosition;
        var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();
        Velocity = direction * Speed;
        MoveAndSlide();

        if (direction.LengthSquared() > 0.01f)
            LookAt(GlobalPosition + direction, Vector3.Up);

        var current = _smPlayback?.GetCurrentNode() ?? "";
        if (current != "attack")
            _smPlayback?.Travel("walk");

        _damageCooldown -= (float)delta;
        if (_damageCooldown <= 0f && GlobalPosition.DistanceTo(_player.GlobalPosition) < BalanceConfig.Enemies.MeleeContactRange)
        {
            if (_player is Godot1.Player.PlayerController pc)
                pc.TakeDamage(ContactDamage, Items.DamageType.Physical, this);
            _damageCooldown = DamageInterval;
            _smPlayback?.Travel("attack");
        }

        TickEots((float)delta);
    }

    public void ApplyEot(EotData eot, float critMultiplier = 1.0f)
    {
        if (_activeEots.TryGetValue(eot.Id, out var existing))
        {
            existing.TimeRemaining = eot.Duration;
            if (eot.IsDamageEot) existing.CritMultiplier = critMultiplier;
            return;
        }
        _activeEots[eot.Id] = new EotInstance
        {
            DefinitionId   = eot.Id,
            TimeRemaining  = eot.Duration,
            TickTimer      = eot.TickRate,
            CritMultiplier = eot.IsDamageEot ? critMultiplier : 1.0f,
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
                    TakeDamage(eot.DamagePerTick * inst.CritMultiplier, Items.DamageType.Magic, inst.CritMultiplier > 1f);
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

    public void TakeDamage(float rawAmount, Items.DamageType type, bool isCrit = false)
    {
        float resistance = type == Items.DamageType.Physical ? PhysicalResistance : MagicResistance;
        float effective  = rawAmount * (1f - resistance);
        EmitSignal(SignalName.DamageTaken, effective, type == Items.DamageType.Magic, isCrit);
        _currentHealth  -= Mathf.CeilToInt(effective);
        if (_currentHealth <= 0)
            Die();
    }

    private void LoadAnimClip(AnimationPlayer target, string sourcePath, string sourceName, string targetName, Animation.LoopModeEnum loop)
    {
        var sourceScene = GD.Load<PackedScene>(sourcePath);
        if (sourceScene == null) return;
        var sourceRoot = sourceScene.Instantiate<Node3D>();
        AddChild(sourceRoot);
        var sourcePlayer = sourceRoot.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (sourcePlayer != null && sourcePlayer.HasAnimation(sourceName))
        {
            var copy = (Animation)sourcePlayer.GetAnimation(sourceName).Duplicate();
            copy.LoopMode = loop;
            if (!target.HasAnimationLibrary(""))
                target.AddAnimationLibrary("", new AnimationLibrary());
            target.GetAnimationLibrary("").AddAnimation(targetName, copy);
        }
        sourceRoot.QueueFree();
    }

    private void Die()
    {
        EmitSignal(SignalName.Died, GlobalPosition);

        if (_player is Player.PlayerController pc)
        {
            pc.CollectXp(MapLevel);
            pc.OnEnemyKilled();
        }

        var shard = ShardScene.Instantiate<Xp.XpShard>();
        GetParent().AddChild(shard);
        shard.GlobalPosition = GlobalPosition;

        if (GD.Randf() < BalanceConfig.Drops.CoinChance)
        {
            var coin = CoinScene.Instantiate<Meta.CoinPickup>();
            GetParent().AddChild(coin);
            coin.GlobalPosition = GlobalPosition;
        }

        if (GD.Randf() < BalanceConfig.Drops.HealthChance)
        {
            var hp = HealthScene.Instantiate<Health.HealthPickup>();
            GetParent().AddChild(hp);
            hp.GlobalPosition = GlobalPosition;
        }

        if (GD.Randf() < BalanceConfig.Drops.CraftingChance)
        {
            var session = GetParent().GetNodeOrNull<Run.RunSession>("RunSession");
            session?.AddCraftingCurrency1(1);
        }

        QueueFree();
    }
}
