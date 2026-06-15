using Godot;
using System.Collections.Generic;

namespace Godot1.Enemies;

public partial class EnemySpawner : Node
{
    private static readonly PackedScene EnemyScene =
        GD.Load<PackedScene>("res://src/enemies/enemy.tscn");

    [Export] public float InitialInterval = 2f;
    [Export] public float MinInterval     = 0.5f;
    [Export] public float SpawnRadius     = 350f;

    private float  _spawnTimer;
    private float  _elapsed;
    private bool   _active;
    private Node3D? _player;
    private Run.RunSession? _runSession;
    private World.DungeonGenerator? _dungeon;
    private List<EnemyPoolEntry> _pool = new() { new EnemyPoolEntry() };
    private int _poolTotal;

    public override void _Ready()
    {
        _player     = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _runSession = GetNodeOrNull<Run.RunSession>("../RunSession");
        _dungeon    = GetTree().Root.FindChild("DungeonMap", true, false) as World.DungeonGenerator;

        if (_dungeon != null)
            _dungeon.MapReady += OnMapReady;
        else
            OnMapReady(); // fallback: no dungeon, start immediately
    }

    private void OnMapReady()
    {
        // Read enemy pool from MapData if available
        var mapData = World.RunConfig.Pending ?? World.MapData.GenerateRandom();
        _pool      = mapData.EnemyPool;
        _poolTotal = 0;
        foreach (var e in _pool) _poolTotal += e.Count;
        if (_poolTotal <= 0) _poolTotal = 1;

        _spawnTimer = 0f;
        _active     = true;
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

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
        var entry = PickPoolEntry();
        var enemy = EnemyScene.Instantiate<EnemyController>();

        ApplyEntry(enemy, entry, minutes);

        GetParent().AddChild(enemy);
        enemy.GlobalPosition = _dungeon != null
            ? _dungeon.GetSpawnPointNear(_player!.GlobalPosition, SpawnRadius * 0.5f)
            : RandomRingPosition();
    }

    private EnemyPoolEntry PickPoolEntry()
    {
        int roll = (int)(GD.Randi() % (uint)_poolTotal);
        int acc  = 0;
        foreach (var e in _pool)
        {
            acc += e.Count;
            if (roll < acc) return e;
        }
        return _pool[0];
    }

    private void ApplyEntry(EnemyController enemy, EnemyPoolEntry entry, float minutes)
    {
        var data = EnemyRegistry.Get(entry.EnemyType);

        enemy.Speed              = data.BaseSpeed   + entry.SpeedBonus  + BalanceConfig.Enemies.SpeedPerMinute  * minutes;
        enemy.MaxHealth          = data.BaseHealth  + entry.HpBonus     + BalanceConfig.Enemies.HealthPerMinute * (int)minutes;
        enemy.ContactDamage      = data.ContactDamage + entry.DamageBonus;
        enemy.DamageInterval     = data.DamageInterval;
        enemy.PhysicalResistance = data.PhysicalResistance + entry.ArmorBonus * 0.01f;
        enemy.MagicResistance    = data.MagicResistance;
        enemy.ModelPath          = data.ModelPath;
        enemy.MapLevel           = _runSession?.MapLevel ?? 1;
    }

    private Vector3 RandomRingPosition()
    {
        var   center = _player!.GlobalPosition;
        float angle  = (float)GD.RandRange(0.0, Mathf.Tau);
        float dist   = (float)GD.RandRange(SpawnRadius, SpawnRadius * 1.2f);
        return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
    }
}
