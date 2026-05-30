namespace Godot1.Enemies;

public record EnemyData(
    string EnemyType,
    float  BaseSpeed,
    int    BaseHealth,
    int    ContactDamage,
    float  DamageInterval = 1f
);

public static class EnemyRegistry
{
    public static readonly EnemyData Standard = new("standard",  75f, 1, 10);
    public static readonly EnemyData Runner   = new("runner",   110f, 1,  8);
    public static readonly EnemyData Tank     = new("tank",      45f, 1, 18);
}
