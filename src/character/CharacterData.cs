using System.Collections.Generic;

namespace Godot1.Character;

public class CharacterData
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public CharacterType Type { get; set; } = CharacterType.Warrior;
    public int RunsCompleted { get; set; } = 0;

    public int CurrentLevel { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;

    public int CoinBank { get; set; } = 0;

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
        ["currentLevel"]   = CurrentLevel,
        ["currentXp"]      = CurrentXp,
        ["coinBank"]       = CoinBank,
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
        CurrentLevel   = d.ContainsKey("currentLevel") ? System.Convert.ToInt32(d["currentLevel"]) : 1,
        CurrentXp      = d.ContainsKey("currentXp")    ? System.Convert.ToInt32(d["currentXp"])    : 0,
        CoinBank       = d.ContainsKey("coinBank")      ? System.Convert.ToInt32(d["coinBank"])      : 0,
        BonusMaxHealth = System.Convert.ToInt32(d["bonusMaxHealth"]),
        BonusSpeed     = System.Convert.ToSingle(d["bonusSpeed"]),
        BonusDamage    = System.Convert.ToSingle(d["bonusDamage"]),
    };
}
