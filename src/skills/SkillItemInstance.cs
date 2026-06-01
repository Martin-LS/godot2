using System.Collections.Generic;

namespace Godot1.Skills;

public class SkillItemInstance
{
    public string       Id                    { get; set; } = System.Guid.NewGuid().ToString();
    public string       DefinitionId          { get; set; } = "";
    public int          Tier                  { get; set; } = 1;
    public List<string> SocketedSkillAugmentIds { get; set; } = new();

    public SkillData? Definition        => SkillRegistry.Get(DefinitionId);
    public int        MaxSkillAugmentSlots => Tier; // 1 / 2 / 3 for Common / Uncommon / Rare
}
