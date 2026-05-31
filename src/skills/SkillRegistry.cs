using System.Collections.Generic;

namespace Godot1.Skills;

public static class SkillRegistry
{
    private static readonly Dictionary<string, SkillData> All = new()
    {
        ["strike"] = new SkillData(
            "strike", "Strike", SkillType.Active,
            Tags: new[] { "Melee", "Attack" },
            Cooldown: 0.8f, Range: 200f,
            IconPath: "res://assets/icons/items/battle_axe.png"),

        ["arrow"] = new SkillData(
            "arrow", "Arrow", SkillType.Active,
            Tags: new[] { "Ranged", "Attack" },
            Cooldown: 0.8f, Range: 400f,
            IconPath: "res://assets/icons/items/war_band.png"),

        ["bolt"] = new SkillData(
            "bolt", "Bolt", SkillType.Active,
            Tags: new[] { "Ranged", "Magic", "Spell" },
            Cooldown: 0.8f, Range: 400f,
            IconPath: "res://assets/icons/items/enchanted_blade.png"),
    };

    public static SkillData? Get(string id) => All.TryGetValue(id, out var s) ? s : null;
}
