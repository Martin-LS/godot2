using System.Collections.Generic;

namespace Godot1.Skills;

public static class SupportRegistry
{
    private static readonly Dictionary<string, SupportData> _all = new()
    {
        ["splash"] = new SupportData("splash", "Splash", new[] { "Melee" },   EotId: null),
        ["pierce"] = new SupportData("pierce", "Pierce", new[] { "Ranged" },  EotId: null),
        ["slow"]   = new SupportData("slow",   "Slow",   new[] { "Attack" },  EotId: "slow"),
    };

    public static SupportData?             Get(string id) => _all.TryGetValue(id, out var s) ? s : null;
    public static IEnumerable<SupportData> GetAll()       => _all.Values;
}
