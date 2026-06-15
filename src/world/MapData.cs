using System;
using System.Collections.Generic;
using Godot1.Enemies;

namespace Godot1.World;

public class MapData
{
    public int      Seed       { get; init; }
    public MapBiome Biome      { get; init; }
    public int      Level      { get; init; }
    public int      ChunkCount { get; init; }

    // Wave-spawn pool: what enemy variants can appear on this map.
    // Spawner draws randomly weighted by Count. v1: single skeleton entry.
    public List<EnemyPoolEntry> EnemyPool { get; init; } = new()
    {
        new EnemyPoolEntry { EnemyType = "skeleton", Count = 1 }
    };

    public static MapData GenerateRandom(int level = 1)
    {
        var sys = new Random();
        return new MapData
        {
            Seed       = sys.Next(),
            Biome      = MapBiome.HollowDarkForest,
            Level      = level,
            ChunkCount = sys.Next(7, 12),
        };
    }
}
