using System.Collections.Generic;

namespace Godot1.Skills;

public static class SkillRegistry
{
    private static readonly Dictionary<string, SkillData> All = new()
    {
        ["entity_burst"] = new SkillData(
            "entity_burst", "Entity Burst", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.StrikeCooldown, Range: BalanceConfig.Skills.StrikeRange,
            FocusCost: BalanceConfig.Focus.StrikeFocusCost,
            IconPath: "res://assets/icons/items/battle_axe.png",
            Description: "Proves Entity targeting and weapon-adaptive delivery. Universal starter — fires at locked target using equipped weapon.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Entity,
            DamagePattern: SkillDamagePattern.Burst),

        ["self_channeled_tick"] = new SkillData(
            "self_channeled_tick", "Self Channeled Tick", SkillType.Channeled,
            Tags: new[] { "Melee", "Attack" },
            Cooldown: BalanceConfig.Skills.CycloneCooldown, Range: BalanceConfig.Skills.CycloneRange,
            FocusCost: BalanceConfig.Focus.CycloneFocusCostPerSec,
            Description: "Proves Channeled skill type with Self targeting. Continuous ticking damage while held; drains Focus over time.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Self,
            DamagePattern: SkillDamagePattern.Tick),

        ["self_burst"] = new SkillData(
            "self_burst", "Self Burst", SkillType.Active,
            Tags: new[] { "Attack", "Burst" },
            Cooldown: BalanceConfig.Skills.NovaCooldown, Range: BalanceConfig.Skills.NovaRange,
            FocusCost: BalanceConfig.Focus.NovaFocusCost,
            Description: "Proves Active Self burst. Instant explosion centered on player; flat Focus cost.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Self,
            DamagePattern: SkillDamagePattern.Burst),

        ["self_aura_tick"] = new SkillData(
            "self_aura_tick", "Self Aura Tick", SkillType.Aura,
            Tags: new[] { "Aura" },
            Cooldown: BalanceConfig.Skills.DamageAuraCooldown, Range: BalanceConfig.Skills.DamageAuraRange,
            FocusCost: BalanceConfig.Focus.DamageAuraReservation,
            Description: "Proves Aura skill type with Focus reservation. Always-on Self zone that ticks damage while toggled on.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Self,
            DamagePattern: SkillDamagePattern.Tick),

        // --- Prototype skill library ---

        ["tracked_tick"] = new SkillData(
            "tracked_tick", "Tracked Tick", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.TrackedTickCooldown,
            Range: BalanceConfig.Skills.TrackedTickRange,
            FocusCost: BalanceConfig.Focus.TrackedTickFocusCost,
            Description: "Attaches a ticking damage zone to the locked enemy. Zone follows the enemy and expires when they die. Proves ZoneTracksEntity and entity death expiry.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Entity,
            DamagePattern: SkillDamagePattern.Tick,
            ZoneTracksEntity: true,
            Duration: BalanceConfig.Skills.TrackedTickDuration,
            ZoneRadius: BalanceConfig.Skills.TrackedTickZoneRadius),

        ["triggered_zone_burst"] = new SkillData(
            "triggered_zone_burst", "Triggered Zone Burst", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.TriggeredZoneBurstCooldown,
            Range: BalanceConfig.Skills.TriggeredZoneBurstRange,
            FocusCost: BalanceConfig.Focus.TriggeredZoneBurstFocusCost,
            Description: "Places a dormant trap; arms after 0.5s then fires once when an enemy enters the trigger radius. Proves TriggerRadius/ArmTime/TriggerCount.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Position,
            DamagePattern: SkillDamagePattern.Burst,
            StackLimit: 3,
            Duration: BalanceConfig.Skills.TriggeredZoneBurstDuration,
            ZoneRadius: BalanceConfig.Skills.TriggeredZoneBurstZoneRadius,
            TriggerRadius: BalanceConfig.Skills.TriggeredZoneBurstTriggerRadius,
            ArmTime: BalanceConfig.Skills.TriggeredZoneBurstArmTime,
            TriggerCount: 1),

        ["stackable_zone"] = new SkillData(
            "stackable_zone", "Stackable Zone", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.StackableZoneCooldown,
            Range: BalanceConfig.Skills.StackableZoneRange,
            FocusCost: BalanceConfig.Focus.StackableZoneFocusCost,
            Description: "Each cast places an independent ticking zone; up to 3 active simultaneously. A 4th cast despawns the oldest. Proves StackLimit and oldest-despawn on cap.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Position,
            DamagePattern: SkillDamagePattern.Tick,
            StackLimit: 3,
            Duration: BalanceConfig.Skills.StackableZoneDuration,
            ZoneRadius: BalanceConfig.Skills.StackableZoneZoneRadius),

        ["entity_debuff"] = new SkillData(
            "entity_debuff", "Entity Debuff", SkillType.Active,
            Tags: new[] { "Debuff" },
            Cooldown: BalanceConfig.Skills.EntityDebuffCooldown,
            Range: BalanceConfig.Skills.EntityDebuffRange,
            FocusCost: BalanceConfig.Focus.EntityDebuffFocusCost,
            Description: "Applies slow to locked target for 6s with no damage. Proves Entity targeting with pure debuff output.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Entity,
            DamagePattern: SkillDamagePattern.None,
            InherentEotIds: new[] { "slow" }),

        ["windup_burst"] = new SkillData(
            "windup_burst", "Windup Burst", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.WindupBurstCooldown,
            Range: BalanceConfig.Skills.WindupBurstRange,
            FocusCost: BalanceConfig.Focus.WindupBurstFocusCost,
            Description: "Telegraphed 1.5s wind-up before a high-damage burst at cursor position. Proves wind-up mechanic.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Position,
            DamagePattern: SkillDamagePattern.Burst,
            WindUp: BalanceConfig.Skills.WindupBurstWindUp,
            StackLimit: 1,
            Duration: 0f,
            ZoneRadius: BalanceConfig.Skills.WindupBurstZoneRadius),

        ["fixed_zone_burst"] = new SkillData(
            "fixed_zone_burst", "Fixed Zone Burst", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.FixedZoneBurstCooldown,
            Range: BalanceConfig.Skills.FixedZoneBurstRange,
            FocusCost: BalanceConfig.Focus.FixedZoneBurstFocusCost,
            Description: "Instant explosion at locked target's position. Proves Position targeting resolves to enemy location, not player.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Position,
            DamagePattern: SkillDamagePattern.Burst,
            StackLimit: 1,
            Duration: 0f,
            ZoneRadius: BalanceConfig.Skills.FixedZoneBurstZoneRadius),

        ["fixed_zone_tick"] = new SkillData(
            "fixed_zone_tick", "Fixed Zone Tick", SkillType.Active,
            Tags: new[] { "Attack" },
            Cooldown: BalanceConfig.Skills.FixedZoneTickCooldown,
            Range: BalanceConfig.Skills.FixedZoneTickRange,
            FocusCost: BalanceConfig.Focus.FixedZoneTickFocusCost,
            Description: "Persistent ticking zone at locked target's position. Proves Position targeting with duration and tick damage.",
            IsPrototype: true,
            TargetingShape: SkillTargetingShape.Position,
            DamagePattern: SkillDamagePattern.Tick,
            StackLimit: 1,
            Duration: BalanceConfig.Skills.FixedZoneTickDuration,
            ZoneRadius: BalanceConfig.Skills.FixedZoneTickZoneRadius),
    };

    public static SkillData? Get(string id) => All.TryGetValue(id, out var s) ? s : null;
}
