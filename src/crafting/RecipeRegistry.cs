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
        ["recipe_heavy_armor_t1"]  = new("recipe_heavy_armor_t1",  "heavy_armor_t1",  RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_medium_armor_t1"] = new("recipe_medium_armor_t1", "medium_armor_t1", RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_light_armor_t1"]  = new("recipe_light_armor_t1",  "light_armor_t1",  RecipeType.Gear,    new() { ["crafting_common"] = 1 }),
        ["recipe_accessory_t1"]    = new("recipe_accessory_t1",    "accessory_t1",    RecipeType.Gear,    new() { ["crafting_common"] = 1 }),

        // Skill recipes
        ["recipe_strike"] = new("recipe_strike", "strike", RecipeType.Skill,   new() { ["crafting_common"] = 1 }),
        ["recipe_arrow"]  = new("recipe_arrow",  "arrow",  RecipeType.Skill,   new() { ["crafting_common"] = 1 }),
        ["recipe_bolt"]   = new("recipe_bolt",   "bolt",   RecipeType.Skill,   new() { ["crafting_common"] = 1 }),

        // Support recipes
        ["recipe_splash"] = new("recipe_splash", "splash", RecipeType.Support, new() { ["crafting_common"] = 1 }),
        ["recipe_pierce"] = new("recipe_pierce", "pierce", RecipeType.Support, new() { ["crafting_common"] = 1 }),
        ["recipe_slow"]   = new("recipe_slow",   "slow",   RecipeType.Support, new() { ["crafting_common"] = 1 }),

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
