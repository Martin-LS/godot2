namespace Godot1.Skills;

public class SkillItemInstance
{
    public string  Id              { get; set; } = System.Guid.NewGuid().ToString();
    public string  DefinitionId    { get; set; } = "";
    public int     Tier            { get; set; } = 1;
    public string? Augment         { get; set; } = null;
    public string? ChainInstanceId { get; set; } = null;

    public SkillData? Definition => SkillRegistry.Get(DefinitionId);
}
