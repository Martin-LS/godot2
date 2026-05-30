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

        var (baseHp, baseSpd, baseDmg) = Type switch
        {
            CharacterType.Warrior => (150f, 170f, 20f),
            CharacterType.Rogue   => (80f,  260f, 15f),
            CharacterType.Mage    => (100f, 200f, 35f),
            _                     => (100f, 200f, 20f),
        };
        block.SetBase(StatId.MaxHp,  baseHp);
        block.SetBase(StatId.Speed,  baseSpd);
        block.SetBase(StatId.Damage, baseDmg);

        int levelsAboveOne = CurrentLevel - 1;
        if (levelsAboveOne > 0)
        {
            block.AddModifier(new StatModifier(StatId.MaxHp,  ModifierType.FlatAdd, levelsAboveOne * 5f, ModifierSource.Level));
            block.AddModifier(new StatModifier(StatId.Damage, ModifierType.FlatAdd, levelsAboveOne * 1f, ModifierSource.Level));
        }

        foreach (var (_, instance) in EquippedGear)
        {
            var item = instance.Definition;
            if (item == null || item.Slot != ItemSlot.Armor) continue;
            if (item.BonusHp    != 0)  block.AddModifier(new StatModifier(StatId.MaxHp, ModifierType.FlatAdd, item.BonusHp,    ModifierSource.Item, instance.Id));
            if (item.BonusSpeed != 0f) block.AddModifier(new StatModifier(StatId.Speed, ModifierType.FlatAdd, item.BonusSpeed, ModifierSource.Item, instance.Id));
        }

        return block;
    }
}
