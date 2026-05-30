using System.Collections.Generic;
using System.Linq;
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
    public int CurrentXp { get; set; } = 0;

    public Dictionary<string, string> EquippedItems { get; set; } = new();
    public List<string> SlottedSkillIds { get; set; } = new();

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

        foreach (var (_, id) in EquippedItems)
        {
            var item = ItemRegistry.Get(id);
            if (item == null || item.Slot != ItemSlot.Armor) continue;
            if (item.BonusHp    != 0)  block.AddModifier(new StatModifier(StatId.MaxHp, ModifierType.FlatAdd, item.BonusHp,    ModifierSource.Item, id));
            if (item.BonusSpeed != 0f) block.AddModifier(new StatModifier(StatId.Speed, ModifierType.FlatAdd, item.BonusSpeed, ModifierSource.Item, id));
        }

        return block;
    }

    public Dictionary<string, object?> ToDict() => new()
    {
        ["id"]            = Id,
        ["name"]          = Name,
        ["type"]          = Type.ToString(),
        ["runsCompleted"] = RunsCompleted,
        ["currentLevel"]  = CurrentLevel,
        ["currentXp"]     = CurrentXp,
        ["equippedItems"]   = EquippedItems,
        ["slottedSkillIds"] = SlottedSkillIds,
    };

    public static CharacterData FromDict(Dictionary<string, object?> d) => new()
    {
        Id            = (string)d["id"]!,
        Name          = (string)d["name"]!,
        Type          = System.Enum.Parse<CharacterType>((string)d["type"]!),
        RunsCompleted = System.Convert.ToInt32(d["runsCompleted"]),
        CurrentLevel  = d.ContainsKey("currentLevel") ? System.Convert.ToInt32(d["currentLevel"]) : 1,
        CurrentXp     = d.ContainsKey("currentXp")    ? System.Convert.ToInt32(d["currentXp"])    : 0,
        EquippedItems = d.ContainsKey("equippedItems") && d["equippedItems"] is Dictionary<string, object?> eq
            ? eq.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")
            : new(),
        SlottedSkillIds = d.ContainsKey("slottedSkillIds") && d["slottedSkillIds"] is List<string> skills
            ? skills
            : new(),
    };
}
