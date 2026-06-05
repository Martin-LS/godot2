using Godot;

namespace Godot1.Ui;

public partial class TooltipButton : Button
{
    private static readonly Color TitleColor  = new("#D4A017"); // Gold
    private static readonly Color BodyColor   = new("#8AA0AE"); // Pale Slate
    private static readonly Color BgColor     = new("#181C1F"); // Iron Black
    private static readonly Color BorderColor = new("#4A5560"); // Border Highlight

    private static FontFile? _fontRegular;
    private static FontFile? _fontBold;
    private static FontFile FontRegular => _fontRegular ??= GD.Load<FontFile>("res://assets/fonts/Exo_2/Exo_2_1.ttf");
    private static FontFile FontBold    => _fontBold    ??= GD.Load<FontFile>("res://assets/fonts/Exo_2/Exo_2_2.ttf");

    public override Control _MakeCustomTooltip(string forText)
    {
        var lines = forText.Split('\n', 2);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var title = new Label { Text = lines[0], AutowrapMode = TextServer.AutowrapMode.Off };
        title.AddThemeFontOverride("font", FontBold);
        title.AddThemeFontSizeOverride("font_size", 15);
        title.AddThemeColorOverride("font_color", TitleColor);
        vbox.AddChild(title);

        if (lines.Length > 1 && lines[1].Length > 0)
        {
            var body = new Label { Text = lines[1] };
            body.AddThemeFontOverride("font", FontRegular);
            body.AddThemeFontSizeOverride("font_size", 13);
            body.AddThemeColorOverride("font_color", BodyColor);
            vbox.AddChild(body);
        }

        var style = new StyleBoxFlat();
        style.BgColor      = BgColor;
        style.BorderColor  = BorderColor;
        style.BorderWidthLeft = style.BorderWidthTop = style.BorderWidthRight = style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = style.CornerRadiusBottomRight = style.CornerRadiusBottomLeft = 3;
        style.ContentMarginLeft = style.ContentMarginRight = 12;
        style.ContentMarginTop  = style.ContentMarginBottom = 8;

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(vbox);
        return panel;
    }
}
