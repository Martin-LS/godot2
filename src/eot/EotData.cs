namespace Godot1.Eot;

public record EotData(
    string Id,
    string Name,
    float  ApplyChance,
    float  Duration,
    bool   IsDamageEot,
    float  TickRate      = 0f,
    float  DamagePerTick = 0f,
    float  SlowFraction  = 0f
);
