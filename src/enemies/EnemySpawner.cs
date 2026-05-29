using Godot;

namespace Godot1.Enemies;

public partial class EnemySpawner : Node
{
    private static readonly PackedScene EnemyScene =
        GD.Load<PackedScene>("res://src/enemies/enemy.tscn");

    [Export] public float InitialInterval = 1f;
    [Export] public float MinInterval     = 0.3f;
    [Export] public float SpawnRadius     = 350f;

    private float  _spawnTimer;
    private float  _elapsed;
    private Node3D? _player;
    private Run.RunSession? _runSession;

    public override void _Ready()
    {
        _player     = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _runSession = GetNodeOrNull<Run.RunSession>("../RunSession");
        _spawnTimer = 0f;
    }

    public override void _Process(double delta)
    {
        _elapsed    += (float)delta;
        _spawnTimer -= (float)delta;

        if (_spawnTimer <= 0f)
        {
            SpawnEnemy();
            _spawnTimer = CurrentInterval();
        }
    }

    private float CurrentInterval()
    {
        float minutes = _elapsed / 60f;
        return Mathf.Max(MinInterval, InitialInterval / (1f + minutes * 0.5f));
    }

    private void SpawnEnemy()
    {
        if (_player == null) return;

        float minutes = _elapsed / 60f;
        var enemy = EnemyScene.Instantiate<EnemyController>();

        ApplyType(enemy, minutes);

        enemy.Speed     += 10f * minutes;
        enemy.MaxHealth += 5 * (int)minutes;
        enemy.MapLevel   = _runSession?.MapLevel ?? 1;

        GetParent().AddChild(enemy);
        enemy.GlobalPosition = RandomRingPosition();
    }

    private static void ApplyType(EnemyController enemy, float minutes)
    {
        bool runnerUnlocked = minutes >= 1f;
        bool tankUnlocked   = minutes >= 2f;

        int pool = 1 + (runnerUnlocked ? 1 : 0) + (tankUnlocked ? 1 : 0);
        int roll = GD.RandRange(0, pool - 1);

        EnemyData data;
        if (roll == 0)
            data = EnemyRegistry.Standard;
        else if (roll == 1 && runnerUnlocked)
            data = EnemyRegistry.Runner;
        else
            data = EnemyRegistry.Tank;

        enemy.Speed          = data.BaseSpeed;
        enemy.MaxHealth      = data.BaseHealth;
        enemy.ContactDamage  = data.ContactDamage;
        enemy.DamageInterval = data.DamageInterval;
    }

    private Vector3 RandomRingPosition()
    {
        var   center = _player!.GlobalPosition;
        float angle  = (float)GD.RandRange(0.0, Mathf.Tau);
        float dist   = (float)GD.RandRange(SpawnRadius, SpawnRadius * 1.2f);
        return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
    }
}
