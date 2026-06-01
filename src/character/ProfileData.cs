using System.Collections.Generic;
using Godot1.Items;
using Godot1.Skills;

namespace Godot1.Character;

public class ProfileData
{
    public const int MaxInventory = 50;

    public int                               CoinBank                       { get; set; } = 0;
    public Dictionary<string, int>           Materials                      { get; set; } = new();
    public List<GearItemInstance>            OwnedGearInstances             { get; set; } = new();
    public List<SkillItemInstance>           OwnedSkillInstances            { get; set; } = new();
    public List<SkillAugmentInstance>        OwnedSkillAugmentInstances     { get; set; } = new();
    public List<EquipmentAugmentInstance>    OwnedEquipmentAugmentInstances { get; set; } = new();

    public int  GetMaterial(string id)          => Materials.GetValueOrDefault(id, 0);
    public void AddMaterial(string id, int qty) => Materials[id] = GetMaterial(id) + qty;
}
