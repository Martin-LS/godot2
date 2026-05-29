using Godot;

namespace Godot1;

public partial class CheckerBackground : Node3D
{
    public override void _Ready()
    {
        const int tileCount  = 128;
        const int tilePixels = 16;
        const int imgSize    = tileCount * tilePixels;

        var img   = Image.CreateEmpty(imgSize, imgSize, false, Image.Format.Rgb8);
        var dark  = new Color(0.25f, 0.25f, 0.25f);
        var light = new Color(0.42f, 0.42f, 0.42f);

        for (int ty = 0; ty < tileCount; ty++)
            for (int tx = 0; tx < tileCount; tx++)
                img.FillRect(new Rect2I(tx * tilePixels, ty * tilePixels, tilePixels, tilePixels),
                             (tx + ty) % 2 == 0 ? dark : light);

        var mat = new StandardMaterial3D
        {
            AlbedoTexture = ImageTexture.CreateFromImage(img),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            ShadingMode   = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        var plane = new PlaneMesh { Size = new Vector2(imgSize, imgSize) };
        plane.Material = mat;
        AddChild(new MeshInstance3D { Mesh = plane });
    }
}
