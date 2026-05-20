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
    private Node2D? _player;

    public override void _Ready()
    {
        _player     = GetTree().GetFirstNodeInGroup("player") as Node2D;
        _spawnTimer = 0f; // spawn immediately on run start
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
        enemy.GlobalPosition = RandomRingPosition();

        ApplyType(enemy, minutes);

        enemy.Speed     += 10f * minutes;
        enemy.MaxHealth += 5 * (int)minutes;

        GetParent().AddChild(enemy);
    }

    private static void ApplyType(EnemyController enemy, float minutes)
    {
        bool runnerUnlocked = minutes >= 1f;
        bool tankUnlocked   = minutes >= 2f;

        int pool = 1 + (runnerUnlocked ? 1 : 0) + (tankUnlocked ? 1 : 0);
        int roll = GD.RandRange(0, pool - 1);

        if (roll == 0)
        {
            enemy.SpriteRow     = 6;
            enemy.Speed         = 130f;
            enemy.MaxHealth     = 30;
            enemy.ContactDamage = 10;
        }
        else if (roll == 1 && runnerUnlocked)
        {
            enemy.SpriteRow     = 4;
            enemy.Speed         = 200f;
            enemy.MaxHealth     = 15;
            enemy.ContactDamage = 8;
        }
        else
        {
            enemy.SpriteRow     = 2;
            enemy.Speed         = 80f;
            enemy.MaxHealth     = 80;
            enemy.ContactDamage = 18;
        }
    }

    // Spawns at a fixed radius ring around the player so engagement distance is
    // consistent regardless of viewport size. Switch to viewport-edge for production.
    private Vector2 RandomRingPosition()
    {
        var   center = _player!.GlobalPosition;
        float angle  = (float)GD.RandRange(0.0, Mathf.Tau);
        float dist   = (float)GD.RandRange(SpawnRadius, SpawnRadius * 1.2f);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }
}
