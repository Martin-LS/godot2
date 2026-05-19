using System.Collections.Generic;

namespace Godot1.Character;

public class CharacterData
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public CharacterType Type { get; set; } = CharacterType.Warrior;
    public int RunsCompleted { get; set; } = 0;
    public int TotalXpEarned { get; set; } = 0;

    public int BonusMaxHealth { get; set; } = 0;
    public float BonusSpeed { get; set; } = 0f;
    public float BonusDamage { get; set; } = 0f;

    public (int MaxHealth, float Speed, float Damage) BaseStats()
    {
        var (hp, spd, dmg) = Type switch
        {
            CharacterType.Warrior => (150, 170f, 20f),
            CharacterType.Rogue   => (80,  260f, 15f),
            CharacterType.Mage    => (100, 200f, 35f),
            _                     => (100, 200f, 20f),
        };
        return (hp + BonusMaxHealth, spd + BonusSpeed, dmg + BonusDamage);
    }

    public Dictionary<string, object?> ToDict() => new()
    {
        ["id"]             = Id,
        ["name"]           = Name,
        ["type"]           = Type.ToString(),
        ["runsCompleted"]  = RunsCompleted,
        ["totalXpEarned"]  = TotalXpEarned,
        ["bonusMaxHealth"] = BonusMaxHealth,
        ["bonusSpeed"]     = BonusSpeed,
        ["bonusDamage"]    = BonusDamage,
    };

    public static CharacterData FromDict(Dictionary<string, object?> d) => new()
    {
        Id             = (string)d["id"]!,
        Name           = (string)d["name"]!,
        Type           = System.Enum.Parse<CharacterType>((string)d["type"]!),
        RunsCompleted  = System.Convert.ToInt32(d["runsCompleted"]),
        TotalXpEarned  = System.Convert.ToInt32(d["totalXpEarned"]),
        BonusMaxHealth = System.Convert.ToInt32(d["bonusMaxHealth"]),
        BonusSpeed     = System.Convert.ToSingle(d["bonusSpeed"]),
        BonusDamage    = System.Convert.ToSingle(d["bonusDamage"]),
    };
}
