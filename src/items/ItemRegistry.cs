using System.Collections.Generic;
using System.Linq;

namespace Godot1.Items;

public static class ItemRegistry
{
    public static readonly IReadOnlyDictionary<string, ItemData> All =
        new Dictionary<string, ItemData>
        {
            // Weapons
            ["sword_t1"] = new("sword_t1", "Sword", ItemSlot.Weapon,
                IconPath: "res://assets/icons/items/iron_sword.png",
                WeaponAffinity: WeaponAffinity.Melee,  SkillBonus: 5f) { Tags = new[] { "Melee" } },
            ["bow_t1"]   = new("bow_t1",   "Bow",   ItemSlot.Weapon,
                WeaponAffinity: WeaponAffinity.Ranged, SkillBonus: 5f) { Tags = new[] { "Ranged" } },
            ["wand_t1"]  = new("wand_t1",  "Wand",  ItemSlot.Weapon,
                IconPath: "res://assets/icons/items/enchanted_blade.png",
                WeaponAffinity: WeaponAffinity.Magic,  SkillBonus: 5f) { Tags = new[] { "Magic" } },

            // Armor
            ["heavy_armor_t1"]  = new("heavy_armor_t1",  "Heavy Armor",  ItemSlot.Armor,
                IconPath: "res://assets/icons/items/chain_mail.png",
                ArmorCategory: ArmorCategory.Heavy,  BonusHp: 20, BonusSpeed: -20f, DamageReduction: 0.10f) { Tags = new[] { "Heavy" } },
            ["medium_armor_t1"] = new("medium_armor_t1", "Medium Armor", ItemSlot.Armor,
                IconPath: "res://assets/icons/items/mage_robe.png",
                ArmorCategory: ArmorCategory.Medium, BonusHp: 10, BonusSpeed:   0f, DamageReduction: 0f)   { Tags = new[] { "Medium" } },
            ["light_armor_t1"]  = new("light_armor_t1",  "Light Armor",  ItemSlot.Armor,
                IconPath: "res://assets/icons/items/leather_vest.png",
                ArmorCategory: ArmorCategory.Light,  BonusHp:  0, BonusSpeed:  20f, DamageReduction: 0f)   { Tags = new[] { "Light" } },

            // Accessories (no tags — universal augments only)
            ["accessory_t1"] = new("accessory_t1", "Amulet", ItemSlot.Accessory,
                IconPath: "res://assets/icons/items/swift_ring.png",
                PhysicalResistance: 0.05f),
        };

    public static ItemData? Get(string id) => All.GetValueOrDefault(id);

    public static IEnumerable<ItemData> ForSlot(ItemSlot slot) =>
        All.Values.Where(i => i.Slot == slot);
}
