using Godot;

namespace Godot1.Enemies;

public partial class EnemySpawner : Node
{
    private static readonly PackedScene EnemyScene =
        GD.Load<PackedScene>("res://src/enemies/enemy.tscn");

    [Export] public float InitialInterval = 2f;
    [Export] public float MinInterval = 0.3f;

    private float _spawnTimer;
    private float _elapsed;
    private Node2D? _player;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        _spawnTimer = InitialInterval;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
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
        enemy.GlobalPosition = RandomEdgePosition();
        enemy.Speed += 10f * minutes;
        enemy.MaxHealth += 5 * (int)minutes;

        GetParent().AddChild(enemy);
    }

    private Vector2 RandomEdgePosition()
    {
        var viewSize = GetViewport().GetVisibleRect().Size;
        float margin = 80f;
        float halfW = viewSize.X / 2f + margin;
        float halfH = viewSize.Y / 2f + margin;
        var center = _player!.GlobalPosition;

        return GD.RandRange(0, 3) switch
        {
            0 => new Vector2(center.X + (float)GD.RandRange(-halfW, halfW), center.Y - halfH),
            1 => new Vector2(center.X + halfW, center.Y + (float)GD.RandRange(-halfH, halfH)),
            2 => new Vector2(center.X + (float)GD.RandRange(-halfW, halfW), center.Y + halfH),
            _ => new Vector2(center.X - halfW, center.Y + (float)GD.RandRange(-halfH, halfH)),
        };
    }
}
