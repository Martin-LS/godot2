using System.Collections.Generic;
using System.Linq;
using Godot1.Items;

namespace Godot1.Crafting;

public static class RecipeRegistry
{
    public static readonly IReadOnlyDictionary<string, RecipeData> All = new Dictionary<string, RecipeData>
    {
        ["recipe_sword_t1"]        = new("recipe_sword_t1",        "sword_t1",        new() { ["crafting_common"] = 5 }),
        ["recipe_bow_t1"]          = new("recipe_bow_t1",          "bow_t1",          new() { ["crafting_common"] = 5 }),
        ["recipe_wand_t1"]         = new("recipe_wand_t1",         "wand_t1",         new() { ["crafting_common"] = 5 }),
        ["recipe_heavy_armor_t1"]  = new("recipe_heavy_armor_t1",  "heavy_armor_t1",  new() { ["crafting_common"] = 8 }),
        ["recipe_medium_armor_t1"] = new("recipe_medium_armor_t1", "medium_armor_t1", new() { ["crafting_common"] = 8 }),
        ["recipe_light_armor_t1"]  = new("recipe_light_armor_t1",  "light_armor_t1",  new() { ["crafting_common"] = 8 }),
        ["recipe_accessory_t1"]    = new("recipe_accessory_t1",    "accessory_t1",    new() { ["crafting_common"] = 5 }),
    };

    public static RecipeData? Get(string id) => All.TryGetValue(id, out var r) ? r : null;

    public static IEnumerable<RecipeData> ForSlot(ItemSlot slot) =>
        All.Values.Where(r => ItemRegistry.Get(r.OutputItemId)?.Slot == slot);
}
