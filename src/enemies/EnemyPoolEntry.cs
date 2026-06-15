namespace Godot1.Enemies;

public class EnemyPoolEntry
{
    public string EnemyType  { get; init; } = "skeleton";
    public int    Count      { get; init; } = 1;
    public int    ArmorBonus { get; init; } = 0;
    public int    HpBonus    { get; init; } = 0;
    public int    SpeedBonus { get; init; } = 0;
    public int    DamageBonus{ get; init; } = 0;
}
