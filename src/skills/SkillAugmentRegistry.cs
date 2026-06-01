using System.Collections.Generic;

namespace Godot1.Skills;

public static class SkillAugmentRegistry
{
    private static readonly Dictionary<string, SkillAugmentData> _all = new()
    {
        ["splash"] = new SkillAugmentData("splash", "Splash", new[] { "Melee" },   EotId: null),
        ["pierce"] = new SkillAugmentData("pierce", "Pierce", new[] { "Ranged" },  EotId: null),
        ["slow"]   = new SkillAugmentData("slow",   "Slow",   new[] { "Attack" },  EotId: "slow"),
    };

    public static SkillAugmentData?             Get(string id) => _all.TryGetValue(id, out var s) ? s : null;
    public static IEnumerable<SkillAugmentData> GetAll()       => _all.Values;
}
