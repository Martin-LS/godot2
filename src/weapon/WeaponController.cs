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

    private static readonly PackedScene CycloneVfxScene =
        GD.Load<PackedScene>("res://src/vfx/cyclone_vfx.tscn");

    private static readonly PackedScene FixedZoneBurstVfxScene =
        GD.Load<PackedScene>("res://src/vfx/fixed_zone_burst_vfx.tscn");

    private static readonly PackedScene FixedZoneTickVfxScene =
        GD.Load<PackedScene>("res://src/vfx/fixed_zone_tick_vfx.tscn");

    private const float SplashRadius = 60f;

    private Player.PlayerController? _player;
    private GpuParticles3D? _cycloneVfx;
    private bool _wasChanneling;

    private float      _physicalDamage  = 20f;
    private float      _magicDamage     = 0f;
    private Items.DamageType _baseDamageType  = Items.DamageType.Physical;
    private float      _globalCritChance = 0f;
    private float      _critMultiplier   = BalanceConfig.SkillAugments.CritMultiplier;

    public override void _Ready()
    {
        _player = GetParent<Player.PlayerController>();

        if (CycloneVfxScene != null)
        {
            var vfxRoot = CycloneVfxScene.Instantiate<Node3D>();
            _cycloneVfx = vfxRoot.GetNodeOrNull<GpuParticles3D>("Whirl");
            if (_cycloneVfx != null) _cycloneVfx.Emitting = false;
            _player?.CallDeferred(Node.MethodName.AddChild, vfxRoot);
        }
    }

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
        public List<Node3D> ActiveZones;
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
        var inherent = skill.InherentEotIds ?? System.Array.Empty<string>();
        var merged   = new List<string>(inherent);
        if (eotIds != null) merged.AddRange(eotIds);
        _slots[slotIndex].EotIds           = merged;
        _slots[slotIndex].HasSplash        = hasSplash;
        _slots[slotIndex].HasPierce        = hasPierce;
        _slots[slotIndex].HasMagicDamage   = hasMagicDamage;
        _slots[slotIndex].CritChanceBonus  = critChanceBonus;
        _slots[slotIndex].AutoActivate  = true;
        _slots[slotIndex].AuraActive    = false;
        _slots[slotIndex].AuraReserved  = 0f;
        _slots[slotIndex].IsChanneling  = false;
        _slots[slotIndex].ActiveZones   = new List<Node3D>();
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

    public bool HasAnyPositionSkill()
    {
        for (int i = 0; i < 3; i++)
            if (_slots[i].Skill?.TargetingShape == SkillTargetingShape.Position) return true;
        return false;
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

        if (_cycloneVfx != null)
        {
            bool channeling = IsAnySlotChanneling();
            if (channeling != _wasChanneling)
            {
                if (!channeling)
                {
                    _cycloneVfx.Restart();
                    _cycloneVfx.Emitting = false;
                }
                else
                {
                    _cycloneVfx.Emitting = true;
                }
                _wasChanneling = channeling;
            }
            if (channeling)
                _cycloneVfx.GetParent<Node3D>().RotateY(Mathf.Tau * 1.0f * dt);
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

        var shape = _slots[i].Skill!.TargetingShape;

        if (shape == SkillTargetingShape.Self)
        {
            if (FindNearestEnemy(_slots[i].Skill!.Range) == null) return;
            if (_player != null && !_player.TrySpendFocus(_slots[i].Skill!.FocusCost)) return;
            FireNova(i);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
        else if (shape == SkillTargetingShape.Entity)
        {
            var target = _player?.LockedTarget;
            if (target == null || target.IsQueuedForDeletion()) return;
            var origin    = GetParent<Node3D>().GlobalPosition;
            float maxRange = _slots[i].Skill!.Range > 0f ? _slots[i].Skill!.Range : _range;
            if (origin.DistanceTo(target.GlobalPosition) > maxRange) return;
            if (_player != null && !_player.TrySpendFocus(_slots[i].Skill!.FocusCost)) return;
            FireAt(i, target);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
        else if (shape == SkillTargetingShape.Position)
        {
            if (_player == null) return;
            var origin  = GetParent<Node3D>().GlobalPosition;
            var firePos = ClampToSkillRange(origin, _player.TargetPosition, _slots[i].Skill!.Range);
            if (!_player.TrySpendFocus(_slots[i].Skill!.FocusCost)) return;
            FireAtPosition(i, firePos);
            _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
        }
    }

    private void ProcessChanneledSlot(int i, float dt)
    {
        if (_slots[i].CooldownTimer > 0f) return;
        if (FindNearestEnemy(_slots[i].Skill!.Range) == null) return;
        float drain = _slots[i].Skill!.FocusCost * _slots[i].Skill!.Cooldown;
        if (_player != null && !_player.TrySpendFocus(drain)) { _slots[i].IsChanneling = false; return; }
        FireCyclone(i);
        _slots[i].CooldownTimer = _slots[i].Skill!.Cooldown;
    }

    private void FireCyclone(int slotIndex)
    {
        ref var slot   = ref _slots[slotIndex];
        var     origin = GetParent<Node3D>().GlobalPosition;

        bool  isMagic = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
        var   dmgType = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
        float baseDmg = (isMagic ? _magicDamage : _physicalDamage) * slot.DamageMultiplier;

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
            EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill!.Cooldown, "Melee");
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

        var shape = slot.Skill.TargetingShape;

        if (shape == SkillTargetingShape.Self)
        {
            if (FindNearestEnemy(slot.Skill.Range) == null) return;
            if (_player != null && !_player.TrySpendFocus(slot.Skill.FocusCost)) return;
            FireNova(slotIndex);
            slot.CooldownTimer = slot.Skill.Cooldown;
            return;
        }

        if (shape == SkillTargetingShape.Entity)
        {
            var tgt = _player?.LockedTarget;
            if (tgt == null || tgt.IsQueuedForDeletion()) return;
            var origin    = GetParent<Node3D>().GlobalPosition;
            float maxRange = slot.Skill.Range > 0f ? slot.Skill.Range : _range;
            if (origin.DistanceTo(tgt.GlobalPosition) > maxRange) return;
            if (_player != null && !_player.TrySpendFocus(slot.Skill.FocusCost)) return;
            FireAt(slotIndex, tgt);
            slot.CooldownTimer = slot.Skill.Cooldown;
            return;
        }

        if (shape == SkillTargetingShape.Position)
        {
            if (_player == null) return;
            var origin  = GetParent<Node3D>().GlobalPosition;
            var firePos = ClampToSkillRange(origin, _player.TargetPosition, slot.Skill.Range);
            if (!_player.TrySpendFocus(slot.Skill.FocusCost)) return;
            FireAtPosition(slotIndex, firePos);
            slot.CooldownTimer = slot.Skill.Cooldown;
        }
    }

    private void FireAt(int slotIndex, Enemies.EnemyController target)
    {
        var slot = _slots[slotIndex];

        if (slot.Skill!.DamagePattern == SkillDamagePattern.None)
        {
            // Pure debuff: apply inherent EoTs at 100% chance (no probability roll)
            foreach (var eotId in slot.EotIds)
            {
                var eot = EotRegistry.Get(eotId);
                if (eot != null) target.ApplyEot(eot, 1.0f);
            }
            EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill.Cooldown, "Debuff");
            return;
        }

        if (slot.Skill.DamagePattern == SkillDamagePattern.Tick && slot.Skill.ZoneTracksEntity)
        {
            bool  ttMagic  = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
            var   ttType   = ttMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
            float ttDmg    = (ttMagic ? _magicDamage : _physicalDamage)
                             * slot.DamageMultiplier * BalanceConfig.Skills.TrackedTickDamageMult;
            float ttCritChance = _globalCritChance + slot.CritChanceBonus;
            float ttCrit   = 1.0f;
            if (ttCritChance > 0f && GD.Randf() < ttCritChance)
                ttCrit = _critMultiplier;
            float radius = slot.Skill.ZoneRadius > 0f ? slot.Skill.ZoneRadius : _range;

            var zone = new TrackedTick
            {
                Target         = target,
                Damage         = ttDmg * ttCrit,
                DmgType        = ttType,
                Radius         = radius,
                Duration       = slot.Skill.Duration,
                TickInterval   = BalanceConfig.Skills.TrackedTickRate,
                EotIds         = slot.EotIds,
                CritMultiplier = ttCrit,
            };
            GetTree().Root.AddChild(zone);
            zone.GlobalPosition = new Vector3(target.GlobalPosition.X, 10.0f, target.GlobalPosition.Z);
            EmitSignal(SignalName.SkillFired, slotIndex, slot.Skill.Cooldown, "Attack");
            return;
        }

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

    private void FireAtPosition(int slotIndex, Vector3 worldPos)
    {
        ref var slot = ref _slots[slotIndex];

        if (slot.Skill!.TriggerRadius > 0f)
        {
            bool  isMagic  = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
            var   dmgType  = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
            float baseDmg  = (isMagic ? _magicDamage : _physicalDamage)
                             * BalanceConfig.Skills.TriggeredZoneBurstDamageMult;

            float critChance = _globalCritChance + slot.CritChanceBonus;
            float critMult   = 1.0f;
            if (critChance > 0f && GD.Randf() < critChance)
                critMult = _critMultiplier;
            baseDmg *= critMult;

            slot.ActiveZones.RemoveAll(z => z == null || !GodotObject.IsInstanceValid(z) || z.IsQueuedForDeletion());
            if (slot.Skill.StackLimit > 1 && slot.ActiveZones.Count >= slot.Skill.StackLimit)
            {
                slot.ActiveZones[0].QueueFree();
                slot.ActiveZones.RemoveAt(0);
            }

            float blastRadius = slot.Skill.ZoneRadius > 0f ? slot.Skill.ZoneRadius : _range;
            var trap = new TriggerZone
            {
                Damage        = baseDmg,
                DmgType       = dmgType,
                BlastRadius   = blastRadius,
                TriggerRadius = slot.Skill.TriggerRadius,
                Duration      = slot.Skill.Duration,
                ArmTime       = slot.Skill.ArmTime,
                EotIds        = slot.EotIds,
                CritMultiplier = critMult,
            };
            GetTree().Root.AddChild(trap);
            trap.GlobalPosition = new Vector3(worldPos.X, 10.0f, worldPos.Z);
            slot.ActiveZones.Add(trap);
        }
        else if (slot.Skill!.DamagePattern == SkillDamagePattern.Burst)
        {
            bool  isMagic    = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
            var   dmgType    = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
            float burstMult  = slot.Skill.WindUp > 0f
                ? BalanceConfig.Skills.WindupBurstDamageMult
                : BalanceConfig.Skills.FixedZoneBurstDamageMult;
            float baseDmg    = (isMagic ? _magicDamage : _physicalDamage) * burstMult;

            float critChance = _globalCritChance + slot.CritChanceBonus;
            float critMult   = 1.0f;
            if (critChance > 0f && GD.Randf() < critChance)
                critMult = _critMultiplier;
            baseDmg *= critMult;

            float radius = slot.Skill!.ZoneRadius > 0f ? slot.Skill!.ZoneRadius : _range;
            bool  isCrit = critMult > 1f;

            if (slot.Skill.WindUp > 0f)
            {
                // Capture everything — lambdas can't close over ref locals
                float            capDmg    = baseDmg;
                Items.DamageType capType   = dmgType;
                float            capCrit   = critMult;
                bool             capIsCrit = isCrit;
                float            capRadius = radius;
                var              capEots   = slot.EotIds;
                Vector3          capPos    = worldPos;
                float            capWindUp = slot.Skill.WindUp;

                var telegraph = new WindupTelegraph { Radius = capRadius, Duration = capWindUp };
                GetTree().Root.AddChild(telegraph);
                telegraph.GlobalPosition = new Vector3(capPos.X, 10.0f, capPos.Z);

                GetTree().CreateTimer(capWindUp).Timeout += () =>
                {
                    foreach (var node in GetTree().GetNodesInGroup("enemies"))
                    {
                        if (node is not Enemies.EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
                        if (capPos.DistanceTo(enemy.GlobalPosition) > capRadius) continue;
                        enemy.TakeDamage(capDmg, capType, capIsCrit);
                        ApplyEots(enemy, capEots, capCrit);
                    }
                    SpawnZoneBurstVfx(capPos);
                };
            }
            else
            {
                foreach (var node in GetTree().GetNodesInGroup("enemies"))
                {
                    if (node is not Enemies.EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
                    if (worldPos.DistanceTo(enemy.GlobalPosition) > radius) continue;
                    enemy.TakeDamage(baseDmg, dmgType, isCrit);
                    ApplyEots(enemy, slot.EotIds, critMult);
                }
                SpawnZoneBurstVfx(worldPos);
            }
        }
        else if (slot.Skill!.DamagePattern == SkillDamagePattern.Tick)
        {
            bool  isMagic = (_baseDamageType == Items.DamageType.Magic) || slot.HasMagicDamage;
            var   dmgType = isMagic ? Items.DamageType.Magic : Items.DamageType.Physical;
            float damageMult = slot.Skill.StackLimit > 1
                ? BalanceConfig.Skills.StackableZoneDamageMult
                : BalanceConfig.Skills.FixedZoneTickDamageMult;
            float baseDmg = (isMagic ? _magicDamage : _physicalDamage) * damageMult;

            float critChance = _globalCritChance + slot.CritChanceBonus;
            float critMult   = 1.0f;
            if (critChance > 0f && GD.Randf() < critChance)
                critMult = _critMultiplier;
            baseDmg *= critMult;

            float radius = slot.Skill!.ZoneRadius > 0f ? slot.Skill!.ZoneRadius : _range;

            if (slot.Skill.StackLimit > 1)
            {
                slot.ActiveZones.RemoveAll(z => z == null || !GodotObject.IsInstanceValid(z) || z.IsQueuedForDeletion());
                if (slot.ActiveZones.Count >= slot.Skill.StackLimit)
                {
                    slot.ActiveZones[0].QueueFree();
                    slot.ActiveZones.RemoveAt(0);
                }

                var zone = new StackableZone
                {
                    Damage         = baseDmg,
                    DmgType        = dmgType,
                    Radius         = radius,
                    Duration       = slot.Skill!.Duration,
                    TickInterval   = BalanceConfig.Skills.StackableZoneRate,
                    EotIds         = slot.EotIds,
                    CritMultiplier = critMult,
                };
                GetTree().Root.AddChild(zone);
                zone.GlobalPosition = new Vector3(worldPos.X, 10.0f, worldPos.Z);
                slot.ActiveZones.Add(zone);
            }
            else
            {
                var zone = new FixedZoneTick
                {
                    Damage         = baseDmg,
                    DmgType        = dmgType,
                    Radius         = radius,
                    Duration       = slot.Skill!.Duration,
                    TickInterval   = BalanceConfig.Skills.FixedZoneTickRate,
                    EotIds         = slot.EotIds,
                    CritMultiplier = critMult,
                };
                GetTree().Root.AddChild(zone);
                zone.GlobalPosition = worldPos;
                SpawnZoneTickVfx(worldPos, slot.Skill!.Duration);
            }
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

    private void SpawnZoneBurstVfx(Vector3 worldPos)
    {
        if (FixedZoneBurstVfxScene == null) return;
        var vfx   = FixedZoneBurstVfxScene.Instantiate<Node3D>();
        var whirl = vfx.GetNodeOrNull<GpuParticles3D>("Whirl");
        GetTree().Root.AddChild(vfx);
        vfx.GlobalPosition = worldPos;
        if (whirl != null) whirl.Emitting = true;
        GetTree().CreateTimer(1.5).Timeout += vfx.QueueFree;
    }

    private void SpawnZoneTickVfx(Vector3 worldPos, float duration)
    {
        if (FixedZoneTickVfxScene == null) return;
        var vfx   = FixedZoneTickVfxScene.Instantiate<Node3D>();
        var whirl = vfx.GetNodeOrNull<GpuParticles3D>("Whirl");
        GetTree().Root.AddChild(vfx);
        vfx.GlobalPosition = worldPos;
        if (whirl != null) whirl.Emitting = true;
        GetTree().CreateTimer(duration + 0.5).Timeout += vfx.QueueFree;
    }

    private static Vector3 ClampToSkillRange(Vector3 origin, Vector3 target, float range)
    {
        var flat = new Vector3(target.X - origin.X, 0f, target.Z - origin.Z);
        float dist = flat.Length();
        if (dist <= range || dist < 0.001f) return target;
        return origin + flat / dist * range;
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
