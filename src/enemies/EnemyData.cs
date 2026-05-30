namespace Godot1.Enemies;

public record EnemyData(
    string EnemyType,
    float  BaseSpeed,
    int    BaseHealth,
    int    ContactDamage,
    float  DamageInterval     = 1f,
    float  PhysicalResistance = 0f,
    float  MagicResistance    = 0f
);

public static class EnemyRegistry
{
    public static readonly EnemyData Standard = new("standard",  75f, 1, 10, PhysicalResistance: 0f,    MagicResistance: 0f);
    public static readonly EnemyData Runner   = new("runner",   110f, 1,  8, PhysicalResistance: 0f,    MagicResistance: 0.15f);
    public static readonly EnemyData Tank     = new("tank",      45f, 1, 18, PhysicalResistance: 0.20f, MagicResistance: 0f);
}
