using System.Collections.Generic;
using Godot1.Items;
using Godot1.Stats;

namespace Godot1.Character;

public class CharacterData
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public CharacterType Type { get; set; } = CharacterType.Warrior;
    public int RunsCompleted { get; set; } = 0;

    public int CurrentLevel { get; set; } = 1;
    public int CurrentXp    { get; set; } = 0;

    // Gear instances owned by this character (moved out of profile inventory when equipped).
    public Dictionary<string, GearItemInstance> EquippedGear { get; set; } = new();

    // GUIDs referencing SkillItemInstances in ProfileData.OwnedSkillInstances.
    // Skills are not moved out of inventory when slotted — same instance can fill multiple slots.
    public List<string> SlottedSkillInstanceIds { get; set; } = new();

    public StatBlock BuildStatBlock()
    {
        var block = new StatBlock();

        // Archetype base stats — set directly, not subject to multiplier formula.
        var (baseHp, baseSpd, basePhysDmg, baseMagDmg) = Type switch
        {
            CharacterType.Warrior => (150f, 170f, 20f,  0f),
            CharacterType.Rogue   => (80f,  260f, 15f,  0f),
            CharacterType.Mage    => (100f, 200f, 0f,   35f),
            _                     => (100f, 200f, 20f,  0f),
        };
        block.SetBase(StatId.MaxHp,          baseHp);
        block.SetBase(StatId.Speed,           baseSpd);
        block.SetBase(StatId.PhysicalDamage,  basePhysDmg);
        block.SetBase(StatId.MagicDamage,     baseMagDmg);

        // Level-up flat bonuses — scaled by archetype multiplier × level.
        // Formula: bonus × (CurrentLevel × multiplier).
        // At level 1 there are no level-up bonuses (levelsAboveOne = 0), so no modifiers are added.
        int levelsAboveOne = CurrentLevel - 1;
        if (levelsAboveOne > 0)
        {
            StatId dmgStat  = Type == CharacterType.Mage ? StatId.MagicDamage : StatId.PhysicalDamage;
            float  hpBonus  = levelsAboveOne * 5f * CurrentLevel * ArchetypeMultiplierRegistry.Get(Type, StatId.MaxHp);
            float  dmgBonus = levelsAboveOne * 1f * CurrentLevel * ArchetypeMultiplierRegistry.Get(Type, dmgStat);
            block.AddModifier(new StatModifier(StatId.MaxHp, ModifierType.FlatAdd, hpBonus,  ModifierSource.Level));
            block.AddModifier(new StatModifier(dmgStat,      ModifierType.FlatAdd, dmgBonus, ModifierSource.Level));
        }

        // Item contributions — scaled by archetype multiplier × level.
        foreach (var (_, instance) in EquippedGear)
        {
            var item = instance.Definition;
            if (item == null) continue;

            if (item.Slot == ItemSlot.Armor)
            {
                if (item.BonusHp != 0)
                    block.AddModifier(new StatModifier(StatId.MaxHp, ModifierType.FlatAdd,
                        item.BonusHp * CurrentLevel * ArchetypeMultiplierRegistry.Get(Type, StatId.MaxHp),
                        ModifierSource.Item, instance.Id));
                if (item.BonusSpeed != 0f)
                    block.AddModifier(new StatModifier(StatId.Speed, ModifierType.FlatAdd,
                        item.BonusSpeed * CurrentLevel * ArchetypeMultiplierRegistry.Get(Type, StatId.Speed),
                        ModifierSource.Item, instance.Id));
            }
            else if (item.Slot == ItemSlot.Accessory && item.PhysicalResistance != 0f)
            {
                block.AddModifier(new StatModifier(StatId.PhysicalResistance, ModifierType.FlatAdd,
                    item.PhysicalResistance * CurrentLevel * ArchetypeMultiplierRegistry.Get(Type, StatId.PhysicalResistance),
                    ModifierSource.Item, instance.Id));
            }
        }

        return block;
    }
}
