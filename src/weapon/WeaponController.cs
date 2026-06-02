using Godot;
using System.Collections.Generic;
using Godot1.Skills;

namespace Godot1.Weapon;

public partial class WeaponController : Node
{
    [Signal] public delegate void SkillFiredEventHandler(int slotIndex, float cooldown);

    private static readonly PackedScene ProjectileScene =
        GD.Load<PackedScene>("res://src/weapon/projectile.tscn");

    private float _physicalDamage = 20f;
    private float _magicDamage    = 0f;

    public void SetDamage(float physicalDamage, float magicDamage)
    {
        _physicalDamage = physicalDamage;
        _magicDamage    = magicDamage;
    }

    private struct SkillSlot
    {
        public SkillData?   Skill;
        public float        CooldownTimer;
        public float        SkillBonus;
        public List<string> EotIds;
        public bool         HasSplash;
        public bool         HasPierce;
    }

    private readonly SkillSlot[] _slots = new SkillSlot[3];

    public void SetSlot(int slotIndex, SkillData skill, float weaponSkillBonus,
        List<string>? eotIds = null, bool hasSplash = false, bool hasPierce = false)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        _slots[slotIndex].Skill         = skill;
        _slots[slotIndex].SkillBonus    = weaponSkillBonus;
        _slots[slotIndex].CooldownTimer = 0f;
        _slots[slotIndex].EotIds        = eotIds ?? new List<string>();
        _slots[slotIndex].HasSplash     = hasSplash;
        _slots[slotIndex].HasPierce     = hasPierce;
    }

    public void ReduceCooldowns(float amount)
    {
        for (int i = 0; i < 3; i++)
            _slots[i].CooldownTimer = Mathf.Max(0f, _slots[i].CooldownTimer - amount);
    }

    public override void _PhysicsProcess(double delta)
    {
        for (int i = 0; i < 3; i++)
        {
            if (_slots[i].Skill == null) continue;
            _slots[i].CooldownTimer -= (float)delta;
            if (_slots[i].CooldownTimer > 0f) continue;

            var target = FindNearestEnemy(_slots[i].Skill!.Range);
            if (target == null) continue;

            FireAt(i, target);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
    }

    private void FireAt(int slotIndex, Enemies.EnemyController target)
    {
        var slot      = _slots[slotIndex];
        var origin    = GetParent<Node3D>().GlobalPosition;
        var diff      = target.GlobalPosition - origin;
        var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();

        bool  isMagic = System.Array.Exists(slot.Skill!.Tags, t => t == "Magic");
        bool  isMelee = System.Array.Exists(slot.Skill!.Tags, t => t == "Melee");
        var   dmgType = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg = isMagic ? _magicDamage : _physicalDamage;

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Initialize(direction, baseDmg + slot.SkillBonus, dmgType, slot.EotIds, slot.HasSplash, slot.HasPierce);
        projectile.IsMelee = isMelee;
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = origin;

        EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill.Cooldown);
    }

    private Enemies.EnemyController? FindNearestEnemy(float range)
    {
        var enemies = GetTree().GetNodesInGroup("enemies");
        Enemies.EnemyController? nearest = null;
        float nearestDist = range;

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
