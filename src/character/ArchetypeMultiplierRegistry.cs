using System.Collections.Generic;
using Godot1.Stats;

namespace Godot1.Character;

public static class ArchetypeMultiplierRegistry
{
    private const float Default = 0.1f;

    private static readonly Dictionary<(CharacterType, StatId), float> _overrides = new()
    {
        // Warrior — physical tank
        [(CharacterType.Warrior, StatId.MaxHp)]              = Default, // TBD — Balancer
        [(CharacterType.Warrior, StatId.PhysicalDamage)]     = Default, // TBD — Balancer
        [(CharacterType.Warrior, StatId.PhysicalResistance)] = Default, // TBD — Balancer
        // Rogue — speed kiter
        [(CharacterType.Rogue,   StatId.Speed)]              = Default, // TBD — Balancer
        [(CharacterType.Rogue,   StatId.PhysicalDamage)]     = Default, // TBD — Balancer
        // Mage — magic glass cannon
        [(CharacterType.Mage,    StatId.MagicDamage)]        = Default, // TBD — Balancer
        [(CharacterType.Mage,    StatId.MagicResistance)]    = Default, // TBD — Balancer
    };

    public static float Get(CharacterType type, StatId stat) =>
        _overrides.TryGetValue((type, stat), out var v) ? v : Default;
}
