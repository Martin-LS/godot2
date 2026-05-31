using System.Collections.Generic;

namespace Godot1.Items;

public static class EquipmentAugmentRegistry
{
    private static readonly Dictionary<string, EquipmentAugmentData> _all = new()
    {
        ["retaliation"] = new("retaliation", "Retaliation", new[] { "Heavy" }),
        ["fortify"]     = new("fortify",     "Fortify",     new[] { "Heavy" }),
        ["dash_reflex"] = new("dash_reflex", "Dash Reflex", new[] { "Light" }),
        ["ghost_step"]  = new("ghost_step",  "Ghost Step",  new[] { "Light" }),
        ["mending"]     = new("mending",     "Mending",     new[] { "Medium" }),
        ["adaptation"]  = new("adaptation",  "Adaptation",  new[] { "Medium" }),
    };

    public static EquipmentAugmentData?             Get(string id) => _all.TryGetValue(id, out var a) ? a : null;
    public static IEnumerable<EquipmentAugmentData> GetAll()       => _all.Values;
}
