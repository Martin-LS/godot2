namespace Godot1.Items;

public record ItemData(
    string         Id,
    string         Name,
    ItemSlot       Slot,
    string         IconPath           = "",
    // Weapon fields
    WeaponAffinity WeaponAffinity     = WeaponAffinity.None,
    float          SkillBonus         = 0f,
    // Armor fields
    ArmorCategory  ArmorCategory      = ArmorCategory.None,
    int            BonusHp            = 0,
    float          BonusSpeed         = 0f,
    float          DamageReduction    = 0f,
    // Accessory fields
    float          PhysicalResistance = 0f
)
{
    public string[] Tags { get; init; } = System.Array.Empty<string>();
}
