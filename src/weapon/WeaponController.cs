using Godot;
using System.Collections.Generic;
using Godot1.Eot;
using Godot1.Skills;

namespace Godot1.Weapon;

public partial class WeaponController : Node
{
    [Signal] public delegate void SkillFiredEventHandler(int slotIndex, float cooldown, string delivery);
    [Signal] public delegate void SkillToggledEventHandler(int slotIndex, bool isOn);

    private static readonly PackedScene ProjectileScene =
        GD.Load<PackedScene>("res://src/weapon/projectile.tscn");

    private static readonly PackedScene ImpactHitScene =
        GD.Load<PackedScene>("res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn");

    private const float SplashRadius = 60f;

    private Player.PlayerController? _player;

    private float      _physicalDamage  = 20f;
    private float      _magicDamage     = 0f;
    private Items.DamageType _baseDamageType  = Items.DamageType.Physical;
    private float      _globalCritChance = 0f;
    private float      _critMultiplier   = BalanceConfig.SkillAugments.CritMultiplier;

    public override void _Ready() => _player = GetParent<Player.PlayerController>();

    public void SetDamage(float physicalDamage, float magicDamage)
    {
        _physicalDamage = physicalDamage;
        _magicDamage    = magicDamage;
    }

    public void SetBaseDamageType(Items.DamageType damageType) => _baseDamageType   = damageType;
    public void SetGlobalCritChance(float critChance)          => _globalCritChance = critChance;
    public void SetCritMultiplier(float multiplier)            => _critMultiplier   = multiplier;

    private struct SkillSlot
    {
        public SkillData?   Skill;
        public float        CooldownTimer;
        public List<string> EotIds;
        public bool         HasSplash;
        public bool         HasPierce;
        public bool         HasMagicDamage;
        public float        CritChanceBonus;
        public bool         AutoActivate;
        public bool         AuraActive;
        public float        AuraReserved;
        public float        DamageMultiplier;
        public bool         IsChanneling;
    }

    private readonly SkillSlot[] _slots = new SkillSlot[3];
    private float  _range             = 200f;
    private string _preferredDelivery = "Melee";

    public void SetRange(float effectiveRange)             => _range             = effectiveRange;
    public void SetPreferredDelivery(string delivery)      => _preferredDelivery = delivery;

    public void SetSlot(int slotIndex, SkillData skill,
        List<string>? eotIds = null, bool hasSplash = false, bool hasPierce = false,
        bool hasMagicDamage = false, float critChanceBonus = 0f)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        _slots[slotIndex].Skill            = skill;
        _slots[slotIndex].CooldownTimer    = 0f;
        _slots[slotIndex].EotIds           = eotIds ?? new List<string>();
        _slots[slotIndex].HasSplash        = hasSplash;
        _slots[slotIndex].HasPierce        = hasPierce;
        _slots[slotIndex].HasMagicDamage   = hasMagicDamage;
        _slots[slotIndex].CritChanceBonus  = critChanceBonus;
        _slots[slotIndex].AutoActivate  = true;
        _slots[slotIndex].AuraActive    = false;
        _slots[slotIndex].AuraReserved  = 0f;
        _slots[slotIndex].IsChanneling  = false;
        _slots[slotIndex].DamageMultiplier = skill.Type switch
        {
            SkillType.Channeled => BalanceConfig.Focus.CycloneDamageMultiplier,
            SkillType.Aura      => BalanceConfig.Focus.AuraDamageMultiplier,
            _                   => 1.0f,
        };
    }

    public void SetSlotAutoActivate(int slotIndex, bool autoActivate)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        _slots[slotIndex].AutoActivate = autoActivate;
    }

    public void ReduceCooldowns(float amount)
    {
        for (int i = 0; i < 3; i++)
            _slots[i].CooldownTimer = Mathf.Max(0f, _slots[i].CooldownTimer - amount);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < 3; i++)
        {
            if (_slots[i].Skill == null) continue;

            if (_slots[i].CooldownTimer > 0f)
                _slots[i].CooldownTimer -= dt;

            if (_slots[i].Skill!.Type == SkillType.Channeled && _slots[i].AutoActivate)
                _slots[i].IsChanneling = FindNearestEnemy(_range) != null;

            bool active = (_slots[i].AutoActivate && _slots[i].Skill!.Type != SkillType.Channeled) ||
                          (_slots[i].Skill!.Type == SkillType.Channeled && _slots[i].IsChanneling) ||
                          (_slots[i].Skill!.Type == SkillType.Aura      && _slots[i].AuraActive);
            if (!active) continue;

            switch (_slots[i].Skill!.Type)
            {
                case SkillType.Aura:
                    ProcessAuraSlot(i, dt);
                    break;
                case SkillType.Channeled:
                    ProcessChanneledSlot(i, dt);
                    break;
                default:
                    ProcessActiveSlot(i, dt);
                    break;
            }
        }
    }

    private bool IsAnySlotChanneling()
    {
        for (int i = 0; i < 3; i++)
            if (_slots[i].Skill?.Type == SkillType.Channeled && _slots[i].IsChanneling)
                return true;
        return false;
    }

    private void ProcessActiveSlot(int i, float dt)
    {
        if (IsAnySlotChanneling()) return;
        if (_slots[i].CooldownTimer > 0f) return;

        bool isBurst = System.Array.Exists(_slots[i].Skill!.Tags, t => t == "Burst");
        if (isBurst)
        {
            if (FindNearestEnemy(_slots[i].Skill!.Range) == null) return;
            if (_player != null && !_player.TrySpendFocus(_slots[i].Skill!.FocusCost)) return;
            FireNova(i);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
        else
        {
            var target = FindNearestEnemy(_range);
            if (target == null) return;
            if (_player != null && !_player.TrySpendFocus(_slots[i].Skill!.FocusCost)) return;
            FireAt(i, target);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
    }

    private void ProcessChanneledSlot(int i, float dt)
    {
        if (_slots[i].CooldownTimer > 0f) return;
        var target = FindNearestEnemy(_range);
        if (target == null) return;
        float drain = _slots[i].Skill!.FocusCost * _slots[i].Skill!.Cooldown;
        if (_player != null && !_player.TrySpendFocus(drain)) return;
        FireAt(i, target);
        _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
    }

    private void ProcessAuraSlot(int i, float dt)
    {
        if (!_slots[i].AuraActive) return;

        if (_slots[i].CooldownTimer > 0f) return;
        FireAura(i);
        _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
    }

    public void ReleaseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        if (_slots[slotIndex].Skill?.Type == SkillType.Channeled)
            _slots[slotIndex].IsChanneling = false;
    }

    public void TryFireSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        ref var slot = ref _slots[slotIndex];
        if (slot.Skill == null) return;

        if (slot.Skill.Type == SkillType.Aura)
        {
            if (slot.AuraActive)
            {
                _player?.UnreserveFocus(slot.AuraReserved);
                slot.AuraActive   = false;
                slot.AuraReserved = 0f;
                EmitSignal(SignalName.SkillToggled, slotIndex, false);
            }
            else if (_player != null)
            {
                float reserve = slot.Skill.FocusCost * _player.MaxFocus;
                if (_player.GetAvailableFocus() >= reserve)
                {
                    _player.ReserveFocus(reserve);
                    slot.AuraActive   = true;
                    slot.AuraReserved = reserve;
                    EmitSignal(SignalName.SkillToggled, slotIndex, true);
                }
            }
            return;
        }

        if (slot.CooldownTimer > 0f) return;

        if (slot.Skill.Type == SkillType.Channeled)
        {
            slot.IsChanneling = true;
            return;
        }

        if (System.Array.Exists(slot.Skill.Tags, t => t == "Burst"))
        {
            if (FindNearestEnemy(slot.Skill.Range) == null) return;
            if (_player != null && !_player.TrySpendFocus(slot.Skill.FocusCost)) return;
            FireNova(slotIndex);
            slot.CooldownTimer = slot.Skill.Cooldown;
            return;
        }

        if (_player != null && !_player.TrySpendFocus(slot.Skill.FocusCost)) return;
        var tgt = FindNearestEnemy(_range);
        if (tgt == null) return;
        FireAt(slotIndex, tgt);
        slot.CooldownTimer = slot.Skill.Cooldown;
    }

    private void FireAt(int slotIndex, Enemies.EnemyController target)
    {
        var slot = _slots[slotIndex];

        bool  isMagic   = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
        bool  hasMelee  = System.Array.Exists(slot.Skill!.Tags, t => t == "Melee");
        bool  hasRange  = System.Array.Exists(slot.Skill!.Tags, t => t == "Range");
        // Weapon-adaptive: no delivery tag → inherit weapon's PreferredDelivery
        bool  isMelee   = hasMelee || (!hasRange && _preferredDelivery == "Melee");
        string delivery = isMelee ? "Melee"
            : (_preferredDelivery == "RangeMagic" ? "RangeMagic" : "Range");
        var   dmgType   = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg   = isMagic ? _magicDamage : _physicalDamage;

        float critChance      = _globalCritChance + slot.CritChanceBonus;
        float critMultiplier  = 1.0f;
        if (critChance > 0f && GD.Randf() < critChance)
            critMultiplier = _critMultiplier;
        baseDmg *= critMultiplier * slot.DamageMultiplier;

        if (isMelee)
        {
            float windupDelay = slot.Skill!.Cooldown * BalanceConfig.Skills.MeleeWindupFraction;
            GetTree().CreateTimer(windupDelay).Timeout +=
                () => { if (!target.IsQueuedForDeletion()) HitMelee(target, baseDmg, dmgType, slot.EotIds, slot.HasSplash, critMultiplier); };
        }
        else
        {
            var origin    = GetParent<Node3D>().GlobalPosition;
            var diff      = target.GlobalPosition - origin;
            var direction = new Vector3(diff.X, 0f, diff.Z).Normalized();

            var projectile = ProjectileScene.Instantiate<Projectile>();
            projectile.Initialize(direction, baseDmg, dmgType, slot.EotIds, slot.HasSplash, slot.HasPierce, critMultiplier);
            GetTree().Root.AddChild(projectile);
            projectile.GlobalPosition = origin;
        }

        EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill.Cooldown, delivery);
    }

    private void FireAura(int slotIndex)
    {
        ref var slot   = ref _slots[slotIndex];
        var     origin = GetParent<Node3D>().GlobalPosition;

        bool  isMagic  = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
        var   dmgType  = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg  = (isMagic ? _magicDamage : _physicalDamage) * slot.DamageMultiplier;

        float critChance = _globalCritChance + slot.CritChanceBonus;
        float critMult   = 1.0f;
        if (critChance > 0f && GD.Randf() < critChance)
            critMult = _critMultiplier;
        baseDmg *= critMult;

        bool hit = false;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemies.EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
            if (origin.DistanceTo(enemy.GlobalPosition) > slot.Skill!.Range) continue;
            enemy.TakeDamage(baseDmg, dmgType, critMult > 1f);
            ApplyEots(enemy, slot.EotIds, critMult);
            hit = true;
        }

        if (hit)
            EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill!.Cooldown, "Aura");
    }

    private void FireNova(int slotIndex)
    {
        ref var slot   = ref _slots[slotIndex];
        var     origin = GetParent<Node3D>().GlobalPosition;

        bool  isMagic = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
        var   dmgType = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg = (isMagic ? _magicDamage : _physicalDamage) * BalanceConfig.Focus.NovaDamageMultiplier;

        float critChance = _globalCritChance + slot.CritChanceBonus;
        float critMult   = 1.0f;
        if (critChance > 0f && GD.Randf() < critChance)
            critMult = _critMultiplier;
        baseDmg *= critMult;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemies.EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
            if (origin.DistanceTo(enemy.GlobalPosition) > slot.Skill!.Range) continue;
            enemy.TakeDamage(baseDmg, dmgType, critMult > 1f);
            ApplyEots(enemy, slot.EotIds, critMult);
        }

        EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill!.Cooldown, "Nova");
    }

    private void HitMelee(Enemies.EnemyController target, float damage, Items.DamageType dmgType,
        List<string> eotIds, bool hasSplash, float critMultiplier)
    {
        bool   isCrit  = critMultiplier > 1f;
        var    hitPos  = target.GlobalPosition;
        target.TakeDamage(damage, dmgType, isCrit);
        ApplyEots(target, eotIds, critMultiplier);
        SpawnHitVfx(hitPos);

        if (hasSplash)
        {
            foreach (var node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemies.EnemyController splash) continue;
                if (splash.GlobalPosition.DistanceTo(hitPos) <= SplashRadius)
                {
                    splash.TakeDamage(damage, dmgType, isCrit);
                    ApplyEots(splash, eotIds, critMultiplier);
                }
            }
        }
    }

    private void ApplyEots(Enemies.EnemyController enemy, List<string> eotIds, float critMultiplier)
    {
        foreach (var eotId in eotIds)
        {
            var eot = EotRegistry.Get(eotId);
            if (eot != null && GD.Randf() < eot.ApplyChance)
                enemy.ApplyEot(eot, critMultiplier);
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
