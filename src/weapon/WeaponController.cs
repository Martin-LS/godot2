using Godot;

namespace Godot1.Weapon;

public partial class WeaponController : Node
{
    private static readonly PackedScene ProjectileScene =
        GD.Load<PackedScene>("res://src/weapon/projectile.tscn");

    [Export] public float Damage = 20f;

    public void SetDamage(float d) => Damage = d;
    public void AddDamage(float d) => Damage += d;
    [Export] public float Cooldown = 0.8f;
    [Export] public float Range = 400f;

    private float _cooldownTimer;

    public override void _PhysicsProcess(double delta)
    {
        _cooldownTimer -= (float)delta;
        if (_cooldownTimer > 0f) return;

        var target = FindNearestEnemy();
        if (target == null) return;

        FireAt(target);
        _cooldownTimer = Cooldown;
    }

    private void FireAt(Enemies.EnemyController target)
    {
        var origin = GetParent<Node3D>().GlobalPosition;
        var diff = target.GlobalPosition - origin;
        var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Initialize(direction, Damage);
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = origin;
    }

    private Enemies.EnemyController? FindNearestEnemy()
    {
        var enemies = GetTree().GetNodesInGroup("enemies");
        Enemies.EnemyController? nearest = null;
        float nearestDist = Range;

        var origin = GetParent<Node3D>().GlobalPosition;

        foreach (var node in enemies)
        {
            if (node is not Enemies.EnemyController enemy) continue;
            float dist = origin.DistanceTo(enemy.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
