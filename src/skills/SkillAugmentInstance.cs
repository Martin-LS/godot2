namespace Godot1.Skills;

public class SkillAugmentInstance
{
    public string Id           { get; set; } = System.Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = "";

    public SkillAugmentData? Definition => SkillAugmentRegistry.Get(DefinitionId);
}
