using Godot;
using System.Collections.Generic;
using System.Linq;

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

    public void RecordRunCompletion(int xpEarned)
    {
        if (SelectedCharacter == null) return;
        SelectedCharacter.RunsCompleted++;
        SelectedCharacter.TotalXpEarned += xpEarned;
        Save();
    }

    private void Save()
    {
        var list = new Godot.Collections.Array();
        foreach (var c in _characters)
        {
            var gd = new Godot.Collections.Dictionary();
            foreach (var kv in c.ToDict())
                gd[kv.Key] = Variant.From(kv.Value);
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
