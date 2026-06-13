using System;

namespace Godot1.World;

public class MapData
{
    public int      Seed       { get; init; }
    public MapBiome Biome      { get; init; }
    public int      Level      { get; init; }
    public int      ChunkCount { get; init; }

    public static MapData GenerateRandom(int level = 1)
    {
        var sys = new Random();
        return new MapData
        {
            Seed       = sys.Next(),
            Biome      = MapBiome.HollowDarkForest,
            Level      = level,
            ChunkCount = sys.Next(4, 7),
        };
    }
}
