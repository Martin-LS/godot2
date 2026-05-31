using Godot;
using System.Collections.Generic;
using Godot1.Eot;

namespace Godot1.Weapon;

public partial class Projectile : Area3D
{
    public float             Damage;
    public Items.DamageType  DamageType = Items.DamageType.Physical;
    public float             Speed      = 500f;
    public float             MaxRange   = 600f;

    private Vector3       _direction;
    private float         _traveled;
    private List<string>  _eotIds = new();

    public void Initialize(Vector3 direction, float damage, Items.DamageType type = Items.DamageType.Physical, List<string>? eotIds = null)
    {
        _direction = direction.Normalized();
        Damage     = damage;
        DamageType = type;
        _eotIds    = eotIds ?? new List<string>();
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
        if (body is Enemies.EnemyController enemy)
        {
            enemy.TakeDamage(Damage, DamageType);
            foreach (var eotId in _eotIds)
            {
                var eot = EotRegistry.Get(eotId);
                if (eot != null && GD.Randf() < eot.ApplyChance)
                    enemy.ApplyEot(eot);
            }
            QueueFree();
        }
    }
}
