using System.Collections.Generic;
using System.Linq;

namespace Godot1.Items;

public static class ItemRegistry
{
    public static readonly IReadOnlyDictionary<string, ItemData> All =
        new Dictionary<string, ItemData>
        {
            // Weapons — affinity bonus to matching skill category; no base damage
            ["sword_t1"] = new("sword_t1", "Sword",  ItemSlot.Weapon, Tier: 1,
                WeaponAffinity: WeaponAffinity.Melee,           SkillBonus: 5f),
            ["bow_t1"]   = new("bow_t1",   "Bow",    ItemSlot.Weapon, Tier: 1,
                WeaponAffinity: WeaponAffinity.RangedPhysical,  SkillBonus: 5f),
            ["wand_t1"]  = new("wand_t1",  "Wand",   ItemSlot.Weapon, Tier: 1,
                WeaponAffinity: WeaponAffinity.RangedMagic,     SkillBonus: 5f),

            // Armor — HP, Speed, and damage reduction vary by category
            ["heavy_armor_t1"]  = new("heavy_armor_t1",  "Heavy Armor",  ItemSlot.Armor, Tier: 1,
                ArmorCategory: ArmorCategory.Heavy,  BonusHp: 20, BonusSpeed: -20f, DamageReduction: 0.10f),
            ["medium_armor_t1"] = new("medium_armor_t1", "Medium Armor", ItemSlot.Armor, Tier: 1,
                ArmorCategory: ArmorCategory.Medium, BonusHp: 10, BonusSpeed:   0f, DamageReduction: 0f),
            ["light_armor_t1"]  = new("light_armor_t1",  "Light Armor",  ItemSlot.Armor, Tier: 1,
                ArmorCategory: ArmorCategory.Light,  BonusHp:  0, BonusSpeed:  20f, DamageReduction: 0f),

            // Accessories — physical resistance only, no category
            ["accessory_t1"] = new("accessory_t1", "Amulet", ItemSlot.Accessory, Tier: 1,
                PhysicalResistance: 0.05f),
        };

    public static ItemData? Get(string id) => All.GetValueOrDefault(id);

    public static IEnumerable<ItemData> ForSlot(ItemSlot slot) =>
        All.Values.Where(i => i.Slot == slot);
}
