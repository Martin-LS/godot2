using System.Collections.Generic;

namespace Godot1.Skills;

public static class AugmentRegistry
{
    private static readonly Dictionary<string, AugmentData> _all = new()
    {
        ["slow"] = new AugmentData("slow", "Slow"),
    };

    public static AugmentData?              Get(string id) => _all.TryGetValue(id, out var a) ? a : null;
    public static IEnumerable<AugmentData>  GetAll()       => _all.Values;
}
