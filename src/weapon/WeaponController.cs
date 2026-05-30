using Godot;
using Godot1.Skills;

namespace Godot1.Weapon;

public partial class WeaponController : Node
{
    [Signal] public delegate void SkillFiredEventHandler(int slotIndex, float cooldown);

    private static readonly PackedScene ProjectileScene =
        GD.Load<PackedScene>("res://src/weapon/projectile.tscn");

    [Export] public float Damage = 20f;

    public void SetDamage(float d) => Damage = d;
    public void AddDamage(float d) => Damage += d;

    public float              SkillBonus    { get; private set; }
    public Items.SkillCategory SkillCategory { get; private set; }

    public void SetSkill(SkillData skill, float weaponSkillBonus, Items.WeaponAffinity affinity)
    {
        SkillCategory = skill.Category;
        Cooldown      = skill.Cooldown;
        Range         = skill.Range;
        bool matches = affinity switch
        {
            Items.WeaponAffinity.Melee          => skill.Category == Items.SkillCategory.Melee,
            Items.WeaponAffinity.RangedPhysical => skill.Category == Items.SkillCategory.RangedPhysical,
            Items.WeaponAffinity.RangedMagic    => skill.Category == Items.SkillCategory.RangedMagic,
            _                                   => false,
        };
        SkillBonus = matches ? weaponSkillBonus : 0f;
    }

    public float Cooldown { get; private set; } = 0.8f;
    private float Range = 400f;

    public void SetCooldown(float value) => Cooldown = value;

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

        var dmgType = SkillCategory switch
        {
            Items.SkillCategory.RangedMagic => Items.DamageType.Magic,
            _                               => Items.DamageType.Physical,
        };
        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Initialize(direction, Damage + SkillBonus, dmgType);
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = origin;
        EmitSignal(SignalName.SkillFired, 0, Cooldown);
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
