using System.Collections.Generic;
using System.Linq;
using Godot1.Items;

namespace Godot1.Crafting;

public static class RecipeRegistry
{
    public static readonly IReadOnlyDictionary<string, RecipeData> All = new Dictionary<string, RecipeData>
    {
        // Gear recipes
        ["recipe_sword_t1"]        = new("recipe_sword_t1",        "sword_t1",        RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_bow_t1"]          = new("recipe_bow_t1",          "bow_t1",          RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_wand_t1"]         = new("recipe_wand_t1",         "wand_t1",         RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_heavy_hat_t1"]    = new("recipe_heavy_hat_t1",    "heavy_hat_t1",    RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_medium_hat_t1"]   = new("recipe_medium_hat_t1",   "medium_hat_t1",   RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_light_hat_t1"]    = new("recipe_light_hat_t1",    "light_hat_t1",    RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_heavy_body_t1"]   = new("recipe_heavy_body_t1",   "heavy_body_t1",   RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_medium_body_t1"]  = new("recipe_medium_body_t1",  "medium_body_t1",  RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_light_body_t1"]   = new("recipe_light_body_t1",   "light_body_t1",   RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_ring_t1"]         = new("recipe_ring_t1",         "ring_t1",         RecipeType.Gear,    new() { ["crafting_common"] = 1 }),

        // Skill recipes — prototype library (one added per test session)
        ["recipe_tracked_tick"]     = new("recipe_tracked_tick",     "tracked_tick",     RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_stackable_zone"]       = new("recipe_stackable_zone",       "stackable_zone",       RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_triggered_zone_burst"] = new("recipe_triggered_zone_burst", "triggered_zone_burst", RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_entity_debuff"]    = new("recipe_entity_debuff",    "entity_debuff",    RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_windup_burst"]     = new("recipe_windup_burst",     "windup_burst",     RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_fixed_zone_burst"] = new("recipe_fixed_zone_burst", "fixed_zone_burst", RecipeType.Skill, new() { ["crafting_common"] = 1 }),
        ["recipe_fixed_zone_tick"]  = new("recipe_fixed_zone_tick",  "fixed_zone_tick",  RecipeType.Skill, new() { ["crafting_common"] = 1 }),

        // Skill Augment recipes
        ["recipe_splash"]          = new("recipe_splash",          "splash",          RecipeType.SkillAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_pierce"]          = new("recipe_pierce",          "pierce",          RecipeType.SkillAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_slow"]            = new("recipe_slow",            "slow",            RecipeType.SkillAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_critical_strike"] = new("recipe_critical_strike", "critical_strike", RecipeType.SkillAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_magic_damage"]    = new("recipe_magic_damage",    "magic_damage",    RecipeType.SkillAugment, new() { ["crafting_common"] = 1 }),

        // Equipment Augment recipes
        ["recipe_retaliation"] = new("recipe_retaliation", "retaliation", RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_fortify"]     = new("recipe_fortify",     "fortify",     RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_dash_reflex"] = new("recipe_dash_reflex", "dash_reflex", RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_ghost_step"]  = new("recipe_ghost_step",  "ghost_step",  RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_mending"]     = new("recipe_mending",     "mending",     RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
        ["recipe_adaptation"]  = new("recipe_adaptation",  "adaptation",  RecipeType.EquipmentAugment, new() { ["crafting_common"] = 1 }),
    };

    public static RecipeData? Get(string id) => All.TryGetValue(id, out var r) ? r : null;

    public static IEnumerable<RecipeData> ForSlot(ItemSlot slot) =>
        All.Values.Where(r => r.Type == RecipeType.Gear && ItemRegistry.Get(r.OutputItemId)?.Slot == slot);

    public static IEnumerable<RecipeData> ForType(RecipeType type) =>
        All.Values.Where(r => r.Type == type);
}
