using Godot;

namespace Godot1.Dev;

public partial class DevOverlay : CanvasLayer
{
    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            Hide();
            return;
        }

        var toggleBtn  = GetNode<Button>("ToggleButton");
        var devPanel   = GetNode<PanelContainer>("DevPanel");
        var speedLabel = GetNode<Label>("DevPanel/VBox/SpeedRow/SpeedLabel");
        var speedSlider = GetNode<HSlider>("DevPanel/VBox/SpeedRow/SpeedSlider");

        devPanel.Hide();
        toggleBtn.Pressed += () => devPanel.Visible = !devPanel.Visible;

        var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        if (player == null) return;

        speedSlider.MinValue = 50;
        speedSlider.MaxValue = 500;
        speedSlider.Step     = 10;
        speedSlider.Value    = player.Speed;
        speedLabel.Text      = $"Speed: {(int)player.Speed}";

        speedSlider.ValueChanged += val =>
        {
            player.Speed  = (float)val;
            speedLabel.Text = $"Speed: {(int)val}";
        };
    }
}
