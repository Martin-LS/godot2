using Godot;
using System.Collections.Generic;
using Godot1.Eot;
using Godot1.Skills;

namespace Godot1.Weapon;

public partial class WeaponController : Node
{
    [Signal] public delegate void SkillFiredEventHandler(int slotIndex, float cooldown, bool isMelee);

    private static readonly PackedScene ProjectileScene =
        GD.Load<PackedScene>("res://src/weapon/projectile.tscn");

    private static readonly PackedScene ImpactHitScene =
        GD.Load<PackedScene>("res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn");

    private const float SplashRadius = 60f;

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
        public List<string> EotIds;
        public bool         HasSplash;
        public bool         HasPierce;
    }

    private readonly SkillSlot[] _slots = new SkillSlot[3];
    private float _range = 200f;

    public void SetRange(float effectiveRange) => _range = effectiveRange;

    public void SetSlot(int slotIndex, SkillData skill,
        List<string>? eotIds = null, bool hasSplash = false, bool hasPierce = false)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        _slots[slotIndex].Skill         = skill;
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

            var target = FindNearestEnemy(_range);
            if (target == null) continue;

            FireAt(i, target);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
    }

    private void FireAt(int slotIndex, Enemies.EnemyController target)
    {
        var slot = _slots[slotIndex];

        bool  isMagic = System.Array.Exists(slot.Skill!.Tags, t => t == "Magic");
        bool  isMelee = System.Array.Exists(slot.Skill!.Tags, t => t == "Melee");
        var   dmgType = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg = isMagic ? _magicDamage : _physicalDamage;

        if (isMelee)
        {
            HitMelee(target, baseDmg, dmgType, slot.EotIds, slot.HasSplash);
        }
        else
        {
            var origin    = GetParent<Node3D>().GlobalPosition;
            var diff      = target.GlobalPosition - origin;
            var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();

            var projectile = ProjectileScene.Instantiate<Projectile>();
            projectile.Initialize(direction, baseDmg, dmgType, slot.EotIds, slot.HasSplash, slot.HasPierce);
            GetTree().Root.AddChild(projectile);
            projectile.GlobalPosition = origin;
        }

        EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill.Cooldown, isMelee);
    }

    private void HitMelee(Enemies.EnemyController target, float damage, Items.DamageType dmgType,
        List<string> eotIds, bool hasSplash)
    {
        var hitPos = target.GlobalPosition;
        target.TakeDamage(damage, dmgType);
        ApplyEots(target, eotIds);
        SpawnHitVfx(hitPos);

        if (hasSplash)
        {
            foreach (var node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemies.EnemyController splash) continue;
                if (splash.GlobalPosition.DistanceTo(hitPos) <= SplashRadius)
                {
                    splash.TakeDamage(damage, dmgType);
                    ApplyEots(splash, eotIds);
                }
            }
        }
    }

    private void ApplyEots(Enemies.EnemyController enemy, List<string> eotIds)
    {
        foreach (var eotId in eotIds)
        {
            var eot = EotRegistry.Get(eotId);
            if (eot != null && GD.Randf() < eot.ApplyChance)
                enemy.ApplyEot(eot);
        }
    }

    private void SpawnHitVfx(Vector3 hitPos)
    {
        try
        {
            var fx  = ImpactHitScene.Instantiate<GpuParticles3D>();
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
