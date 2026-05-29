using Godot;

namespace Godot1.Ui;

public partial class PauseMenu : CanvasLayer
{
    public override void _Ready()
    {
        Visible = false;

        GetNode<Button>("Panel/VBox/ResumeButton").Pressed  += Toggle;
        GetNode<Button>("Panel/VBox/HBox/EndRunButton").Pressed += EndRun;
    }

    public override void _Input(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Toggle()
    {
        Visible = !Visible;
        GetTree().Paused = Visible;
    }

    private void EndRun()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://src/ui/character_screen.tscn");
    }
}
