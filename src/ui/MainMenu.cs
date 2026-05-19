using Godot;

namespace Godot1.Ui;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBox/PlayButton").Pressed += OnPlayPressed;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
    }
}
