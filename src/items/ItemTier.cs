using Godot;

namespace Godot1.Items;

public static class ItemTier
{
    public const int Common   = 1;
    public const int Uncommon = 2;
    public const int Rare     = 3;
    public const int Max      = Rare;

    public static string Label(int tier) => tier switch
    {
        Common   => "Common",
        Uncommon => "Uncommon",
        Rare     => "Rare",
        _        => "Unknown",
    };

    public static Color BackgroundColor(int tier) => tier switch
    {
        Common   => new Color(0.25f, 0.25f, 0.25f),
        Uncommon => new Color(0.10f, 0.45f, 0.10f),
        Rare     => new Color(0.10f, 0.20f, 0.70f),
        _        => new Color(0.25f, 0.25f, 0.25f),
    };
}
