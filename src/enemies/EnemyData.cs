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
    public static readonly EnemyData Skeleton = new("skeleton",
        BalanceConfig.Enemies.Skeleton.BaseSpeed,
        BalanceConfig.Enemies.Skeleton.BaseHealth,
        BalanceConfig.Enemies.Skeleton.ContactDamage,
        PhysicalResistance: BalanceConfig.Enemies.Skeleton.PhysicalResistance,
        MagicResistance: 0f,
        ModelPath: "res://assets/models/characters/enemy_skeleton.glb");

    public static EnemyData Get(string enemyType) => enemyType switch
    {
        "skeleton" => Skeleton,
        _          => Skeleton, // fallback to skeleton for unknown types
    };
}
