using System.Collections.Generic;

namespace Godot1.Eot;

public static class EotRegistry
{
    private static readonly Dictionary<string, EotData> _all = new()
    {
        ["slow"] = new EotData(
            Id:           "slow",
            Name:         "Slow",
            ApplyChance:  0.3f,
            Duration:     3f,
            IsDamageEot:  false,
            SlowFraction: 0.4f
        ),
        ["burn"] = new EotData(
            Id:           "burn",
            Name:         "Burn",
            ApplyChance:  0.25f,
            Duration:     4f,
            IsDamageEot:  true,
            TickRate:     0.5f,
            DamagePerTick: 5f
        ),
    };

    public static EotData?             Get(string id) => _all.TryGetValue(id, out var e) ? e : null;
    public static IEnumerable<EotData> GetAll()       => _all.Values;
}
