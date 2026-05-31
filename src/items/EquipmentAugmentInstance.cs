namespace Godot1.Items;

public class EquipmentAugmentInstance
{
    public string Id           { get; set; } = System.Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = "";

    public EquipmentAugmentData? Definition => EquipmentAugmentRegistry.Get(DefinitionId);
}
