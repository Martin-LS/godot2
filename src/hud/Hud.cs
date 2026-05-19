using Godot;

namespace Godot1.Hud;

public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar = null!;
    private ProgressBar _xpBar = null!;
    private Label _levelLabel = null!;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("Control/HealthBar");
        _xpBar = GetNode<ProgressBar>("Control/XpBar");
        _levelLabel = GetNode<Label>("Control/LevelLabel");

        StyleBar(_healthBar, new Color(0.8f, 0.15f, 0.15f));
        StyleBar(_xpBar, new Color(0.1f, 0.7f, 0.2f));

        var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        if (player == null) return;

        _healthBar.MaxValue = player.MaxHealth;
        _healthBar.Value = player.CurrentHealth;
        _xpBar.MaxValue = player.XpToNextLevel;
        _xpBar.Value = player.CurrentXp;
        _levelLabel.Text = $"Level {player.Level}";

        player.HealthChanged += OnHealthChanged;
        player.XpChanged += OnXpChanged;
        player.LeveledUp += OnLeveledUp;
    }

    private void OnHealthChanged(int newHealth) => _healthBar.Value = newHealth;

    private void OnXpChanged(int currentXp, int xpToNextLevel)
    {
        _xpBar.MaxValue = xpToNextLevel;
        _xpBar.Value = currentXp;
    }

    private void OnLeveledUp(int newLevel) => _levelLabel.Text = $"Level {newLevel}";

    private static void StyleBar(ProgressBar bar, Color fillColor)
    {
        var fill = new StyleBoxFlat { BgColor = fillColor };
        var bg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f) };
        bar.AddThemeStyleboxOverride("fill", fill);
        bar.AddThemeStyleboxOverride("background", bg);
    }
}
