namespace Godot1.Items;

public class GearItemInstance
{
    public string Id           { get; set; } = System.Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = "";
    public int    Tier         { get; set; } = 1;

    public ItemData? Definition => ItemRegistry.Get(DefinitionId);
}
