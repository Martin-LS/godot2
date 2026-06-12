namespace Godot1;

public static class BalanceConfig
{
    public static class Weapons
    {
        public const float SwordRange = 1f;   // tiles
        public const float BowRange   = 7f;
        public const float WandRange  = 5f;

        // Base damage (tier 1) — placeholder, owned by Balancer
        public const float SwordBaseDamage  = 15f;
        public const float BowBaseDamage    = 12f;
        public const float WandBaseDamage   = 18f;

        // Identity bonuses (tier 1) — placeholder, owned by Balancer
        public const float SwordDamageBonus = 0.10f; // +10% physical damage
        public const float BowCritBonus     = 0.08f; // +8% crit chance
        public const float WandDamageBonus  = 0.10f; // +10% magic damage
    }

    public static class Armour
    {
        // Heavy — per piece (hat + body each contribute independently)
        public const int   HeavyBonusHp         = 20;
        public const float HeavyBonusSpeed       = -20f;
        public const float HeavyDamageReduction  = 0.10f;
        public const float HeavyRangeModifier    = -1.5f; // tiles; ranged weapons only

        // Medium
        public const int   MediumBonusHp         = 10;
        public const float MediumBonusSpeed       = 0f;
        public const float MediumDamageReduction  = 0f;
        public const float MediumRangeModifier    = 0f;

        // Light
        public const int   LightBonusHp          = 0;
        public const float LightBonusSpeed        = 20f;
        public const float LightDamageReduction   = 0f;
        public const float LightRangeModifier     = 1.5f;
    }

    public static class Accessories
    {
        public const float RingPhysicalResistance = 0.05f;
    }

    public static class Skills
    {
        public const float StrikeCooldown     = 0.8f;
        public const float StrikeRange        = 200f; // world units
        public const float MeleeWindupFraction = 0.35f; // fraction of cooldown before damage lands

        public const float CycloneCooldown = 0.25f; // tick interval (4 hits/sec)
        public const float CycloneRange    = 150f;

        public const float NovaCooldown = 1.5f;
        public const float NovaRange    = 300f;

        public const float DamageAuraCooldown = 1.0f; // damage tick interval
        public const float DamageAuraRange    = 250f;

        // Prototype: Fixed-Zone-Burst — test values, owned by Balancer
        public const float FixedZoneBurstCooldown    = 1.0f;
        public const float FixedZoneBurstRange       = 180f; // cast range (5 tiles)
        public const float FixedZoneBurstZoneRadius  = 72f;  // blast radius at landing (2 tiles)
        public const float FixedZoneBurstDamageMult  = 1.0f;

        // Prototype: Windup-Burst — test values, owned by Balancer
        public const float WindupBurstCooldown    = 3.0f;
        public const float WindupBurstRange       = 180f;
        public const float WindupBurstZoneRadius  = 108f; // 3 tiles
        public const float WindupBurstWindUp      = 1.5f;
        public const float WindupBurstDamageMult  = 2.0f;

        // Prototype: Tracked-Tick — test values, owned by Balancer
        public const float TrackedTickCooldown   = 3.0f;
        public const float TrackedTickRange      = 180f;
        public const float TrackedTickZoneRadius = 72f;  // 2 tiles
        public const float TrackedTickDuration   = 5.0f;
        public const float TrackedTickRate       = 1.0f;
        public const float TrackedTickDamageMult = 0.4f;

        // Prototype: Triggered-Zone-Burst — test values, owned by Balancer
        public const float TriggeredZoneBurstCooldown     = 1.5f;
        public const float TriggeredZoneBurstRange        = 180f;
        public const float TriggeredZoneBurstTriggerRadius = 36f;  // 1 tile
        public const float TriggeredZoneBurstZoneRadius   = 108f;  // 3-tile blast
        public const float TriggeredZoneBurstDuration     = 30.0f;
        public const float TriggeredZoneBurstArmTime      = 0.5f;
        public const float TriggeredZoneBurstDamageMult   = 2.0f;

        // Prototype: Stackable-Zone — test values, owned by Balancer
        public const float StackableZoneCooldown   = 2.0f;
        public const float StackableZoneRange      = 180f;
        public const float StackableZoneZoneRadius = 72f;
        public const float StackableZoneDuration   = 10.0f;
        public const float StackableZoneRate       = 1.0f;
        public const float StackableZoneDamageMult = 0.4f;

        // Prototype: Entity-Debuff — test values, owned by Balancer
        public const float EntityDebuffCooldown = 3.0f;
        public const float EntityDebuffRange    = 180f;

        // Prototype: Fixed-Zone-Tick — test values, owned by Balancer
        public const float FixedZoneTickCooldown    = 4.0f;
        public const float FixedZoneTickRange       = 180f;
        public const float FixedZoneTickZoneRadius  = 72f;
        public const float FixedZoneTickDuration    = 5.0f;
        public const float FixedZoneTickRate        = 1.0f; // seconds between ticks
        public const float FixedZoneTickDamageMult  = 0.4f; // per tick, hits multiple times
    }

    public static class Focus
    {
        // Per-archetype base pool sizes and regen rates — placeholder, owned by Balancer
        public const float WarriorMaxFocus    = 80f;
        public const float WarriorRegenPerSec = 12f;
        public const float RogueMaxFocus      = 100f;
        public const float RogueRegenPerSec   = 15f;
        public const float MageMaxFocus       = 150f;
        public const float MageRegenPerSec    = 10f;

        // Focus Shield (all archetypes) — placeholder, owned by Balancer
        public const float ShieldFraction    = 0.30f;
        public const float ShieldRegenPerSec = 5f;

        // Per-skill focus costs — placeholder, owned by Balancer
        public const float StrikeFocusCost        = 5f;
        public const float CycloneFocusCostPerSec = 12f;  // drain per second while channeled
        public const float NovaFocusCost          = 20f;
        public const float DamageAuraReservation  = 0.25f; // fraction of MaxFocus

        // Prototype skill focus costs — test values, owned by Balancer
        public const float TrackedTickFocusCost     = 15f;
        public const float TriggeredZoneBurstFocusCost = 15f;
        public const float StackableZoneFocusCost      = 15f;
        public const float EntityDebuffFocusCost   = 10f;
        public const float WindupBurstFocusCost    = 20f;
        public const float FixedZoneBurstFocusCost = 15f;
        public const float FixedZoneTickFocusCost  = 20f;

        // Per-skill type damage multipliers — placeholder, owned by Balancer
        public const float CycloneDamageMultiplier = 0.4f;
        public const float AuraDamageMultiplier    = 0.2f;
        public const float NovaDamageMultiplier    = 0.8f;
    }

    public static class Eots
    {
        public const float SlowApplyChance   = 0.30f;
        public const float SlowDuration      = 3f;    // seconds
        public const float SlowFraction      = 0.75f; // speed reduction

        public const float BurnApplyChance   = 0.25f;
        public const float BurnDuration      = 4f;
        public const float BurnTickRate      = 0.5f;
        public const float BurnDamagePerTick = 5f;
    }

    public static class Enemies
    {
        public static class Skeleton
        {
            public const float BaseSpeed          = 65f;
            public const int   BaseHealth         = 2;
            public const int   ContactDamage      = 5;
            public const float PhysicalResistance = 0.10f;
        }

        public const float SpeedPerMinute      = 5f;  // added to base speed each minute
        public const int   HealthPerMinute     = 3;   // added to base health each minute
        public const float MeleeContactRange   = 32f; // world units; enemy start-hit proximity
    }

    public static class Drops
    {
        public const float CoinChance     = 0.25f;
        public const float HealthChance   = 0.10f;
        public const float CraftingChance = 0.20f;
    }

    public static class Pickups
    {
        public const int XpShardValue    = 5;
        public const int HealthHealAmount = 15;
    }

    public static class Archetypes
    {
        public const float DefaultMultiplier = 0.1f;

        public static class Warrior
        {
            public const float MaxHp                    = 150f;
            public const float Speed                    = 170f;
            public const float PhysicalDamageMultiplier = 1.5f;
            public const float MagicDamageMultiplier    = 0.5f;
        }

        public static class Rogue
        {
            public const float MaxHp                    = 80f;
            public const float Speed                    = 260f;
            public const float PhysicalDamageMultiplier = 1.0f;
            public const float MagicDamageMultiplier    = 0.5f;
        }

        public static class Mage
        {
            public const float MaxHp                    = 100f;
            public const float Speed                    = 200f;
            public const float PhysicalDamageMultiplier = 0.5f;
            public const float MagicDamageMultiplier    = 1.5f;
        }
    }

    public static class LevelUp
    {
        public const float HpBonusPerLevel     = 5f;
        public const float DamageBonusPerLevel = 0.02f; // +2% per level, cumulative — placeholder, owned by Balancer
    }

    public static class SkillAugments
    {
        public const float CritChance     = 0.15f; // TBD — Balancer
        public const float CritMultiplier = 1.5f;  // TBD — Balancer
    }
}
