using System.Collections.Generic;

namespace Godot1.Character;

public class ProfileData
{
    public const int MaxInventory = 50;

    public int CoinBank { get; set; } = 0;
    public Dictionary<string, int> Materials { get; set; } = new();
    public List<string> OwnedItemIds { get; set; } = new();

    public int  GetMaterial(string id)            => Materials.GetValueOrDefault(id, 0);
    public void AddMaterial(string id, int qty)   => Materials[id] = GetMaterial(id) + qty;
}
