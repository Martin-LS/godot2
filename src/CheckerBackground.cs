using Godot;

namespace Godot1;

public partial class CheckerBackground : Node2D
{
    private const int TileSize = 64;

    public override void _Ready()
    {
        ZIndex = -1;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var center = camera.GlobalPosition;
        var viewSize = GetViewportRect().Size;

        var color1 = new Color(0.18f, 0.18f, 0.18f);
        var color2 = new Color(0.28f, 0.28f, 0.28f);

        int tilesX = (int)(viewSize.X / TileSize) + 2;
        int tilesY = (int)(viewSize.Y / TileSize) + 2;

        int startX = (int)Mathf.Floor((center.X - viewSize.X / 2) / TileSize);
        int startY = (int)Mathf.Floor((center.Y - viewSize.Y / 2) / TileSize);

        for (int x = startX; x < startX + tilesX; x++)
        {
            for (int y = startY; y < startY + tilesY; y++)
            {
                var color = (x + y) % 2 == 0 ? color1 : color2;
                DrawRect(new Rect2(x * TileSize, y * TileSize, TileSize, TileSize), color);
            }
        }
    }
}
