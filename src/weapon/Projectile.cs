using Godot;
using System.Collections.Generic;
using Godot1.Eot;

namespace Godot1.Weapon;

public partial class Projectile : Area3D
{
    public float             Damage;
    public Items.DamageType  DamageType  = Items.DamageType.Physical;
    public float             Speed       = 500f;
    public float             MaxRange    = 600f;
    public bool              HasSplash;
    public bool              HasPierce;
    public bool              IsMelee;

    private const float SplashRadius = 60f;

    private Vector3               _direction;
    private float                 _traveled;
    private List<string>          _eotIds   = new();
    private readonly HashSet<ulong> _hitIds = new();

    public void Initialize(Vector3 direction, float damage, Items.DamageType type = Items.DamageType.Physical,
        List<string>? eotIds = null, bool hasSplash = false, bool hasPierce = false)
    {
        _direction = direction.Normalized();
        Damage     = damage;
        DamageType = type;
        _eotIds    = eotIds ?? new List<string>();
        HasSplash  = hasSplash;
        HasPierce  = hasPierce;
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 5f, Height = 10f },
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        var step = _direction * Speed * (float)delta;
        GlobalPosition += step;
        _traveled += step.Length();

        if (_traveled >= MaxRange)
            QueueFree();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Enemies.EnemyController enemy) return;
        if (!_hitIds.Add(enemy.GetInstanceId())) return;

        HitEnemy(enemy, GlobalPosition);

        if (!HasPierce)
            QueueFree();
    }

    private static readonly PackedScene ImpactHitScene =
        GD.Load<PackedScene>("res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn");

    private void HitEnemy(Enemies.EnemyController enemy, Vector3 hitPos)
    {
        enemy.TakeDamage(Damage, DamageType);
        ApplyEots(enemy);

        try
        {
            var fx = ImpactHitScene.Instantiate<GpuParticles3D>();
            var mat = (ParticleProcessMaterial)fx.ProcessMaterial.Duplicate();
            mat.ScaleMin = 40f;
            mat.ScaleMax = 80f;
            fx.ProcessMaterial = mat;
            GetTree().Root.AddChild(fx);
            fx.GlobalPosition = hitPos;
            fx.Call("activate_effects");
            GetTree().CreateTimer(2.0).Timeout += fx.QueueFree;
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"HitEffect failed: {e.Message}");
        }

        if (HasSplash)
        {
            foreach (var node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemies.EnemyController splash) continue;
                if (!_hitIds.Add(splash.GetInstanceId())) continue;
                if (splash.GlobalPosition.DistanceTo(hitPos) <= SplashRadius)
                {
                    splash.TakeDamage(Damage, DamageType);
                    ApplyEots(splash);
                }
            }
        }
    }

    private void ApplyEots(Enemies.EnemyController enemy)
    {
        foreach (var eotId in _eotIds)
        {
            var eot = EotRegistry.Get(eotId);
            if (eot != null && GD.Randf() < eot.ApplyChance)
                enemy.ApplyEot(eot);
        }
    }
}
