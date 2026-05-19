using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot1.Meta;

namespace Godot1.Character;

public partial class CharacterManager : Node
{
    private const string SavePath = "user://characters.json";

    private List<CharacterData> _characters = new();

    public CharacterData? SelectedCharacter { get; private set; }

    public override void _Ready() => Load();

    public IReadOnlyList<CharacterData> GetAll() => _characters;

    public CharacterData Create(string name, CharacterType type)
    {
        var c = new CharacterData { Name = name, Type = type };
        _characters.Add(c);
        Save();
        return c;
    }

    public void Delete(string id)
    {
        _characters.RemoveAll(c => c.Id == id);
        if (SelectedCharacter?.Id == id)
            SelectedCharacter = null;
        Save();
    }

    public void SelectCharacter(string id) =>
        SelectedCharacter = _characters.FirstOrDefault(c => c.Id == id);

    public void RecordRunCompletion(int xpEarned, int coinsEarned)
    {
        if (SelectedCharacter == null) return;
        SelectedCharacter.RunsCompleted++;
        SelectedCharacter.TotalXpEarned += xpEarned;
        SelectedCharacter.CoinBank += coinsEarned;
        Save();
    }

    public bool PurchaseUpgrade(string characterId, MetaUpgradeType type)
    {
        var c = _characters.FirstOrDefault(x => x.Id == characterId);
        if (c == null) return false;

        int level = type switch
        {
            MetaUpgradeType.MaxHealth => c.BonusMaxHealth / 10,
            MetaUpgradeType.Speed     => (int)(c.BonusSpeed / 10f),
            MetaUpgradeType.Damage    => (int)(c.BonusDamage / 2f),
            _                         => 0
        };

        if (level >= 5) return false;
        int cost = (level + 1) * 50;
        if (c.CoinBank < cost) return false;

        c.CoinBank -= cost;
        switch (type)
        {
            case MetaUpgradeType.MaxHealth: c.BonusMaxHealth += 10;  break;
            case MetaUpgradeType.Speed:     c.BonusSpeed     += 10f; break;
            case MetaUpgradeType.Damage:    c.BonusDamage    += 2f;  break;
        }
        Save();
        return true;
    }

    private void Save()
    {
        var list = new Godot.Collections.Array();
        foreach (var c in _characters)
        {
            var gd = new Godot.Collections.Dictionary
            {
                ["id"]             = c.Id,
                ["name"]           = c.Name,
                ["type"]           = c.Type.ToString(),
                ["runsCompleted"]  = c.RunsCompleted,
                ["totalXpEarned"]  = c.TotalXpEarned,
                ["coinBank"]       = c.CoinBank,
                ["bonusMaxHealth"] = c.BonusMaxHealth,
                ["bonusSpeed"]     = c.BonusSpeed,
                ["bonusDamage"]    = c.BonusDamage,
            };
            list.Add(gd);
        }

        var root = new Godot.Collections.Dictionary { ["characters"] = list };
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file?.StoreString(Json.Stringify(root));
    }

    private void Load()
    {
        if (!FileAccess.FileExists(SavePath)) return;
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null) return;

        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.Obj is not Godot.Collections.Dictionary root) return;
        if (root["characters"].Obj is not Godot.Collections.Array list) return;

        _characters.Clear();
        foreach (var item in list)
        {
            if (item.Obj is not Godot.Collections.Dictionary gd) continue;
            var d = new Dictionary<string, object?>();
            foreach (var kv in gd)
                d[kv.Key.ToString()!] = kv.Value.Obj;
            _characters.Add(CharacterData.FromDict(d));
        }
    }
}
