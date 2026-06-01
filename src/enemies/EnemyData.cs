namespace Godot1.Enemies;

public record EnemyData(
    string EnemyType,
    float  BaseSpeed,
    int    BaseHealth,
    int    ContactDamage,
    float  DamageInterval     = 1f,
    float  PhysicalResistance = 0f,
    float  MagicResistance    = 0f,
    string ModelPath          = "res://assets/models/characters/enemy_generic.glb"
);

public static class EnemyRegistry
{
    public static readonly EnemyData Skeleton = new("skeleton",  65f, 2, 5, PhysicalResistance: 0.10f, MagicResistance: 0f,
        ModelPath: "res://assets/models/characters/enemy_skeleton.glb");
}
