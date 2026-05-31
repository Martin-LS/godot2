namespace Godot1.Skills;

public class SupportItemInstance
{
    public string Id           { get; set; } = System.Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = "";

    public SupportData? Definition => SupportRegistry.Get(DefinitionId);
}
