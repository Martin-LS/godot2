using Godot;

namespace Godot1.Hud;

public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar = null!;
    private ProgressBar _xpBar = null!;
    private Label _levelLabel = null!;
    private Label _timerLabel = null!;
    private Label _coinLabel = null!;

    private Run.RunSession? _session;

    public override void _Ready()
    {
        _healthBar  = GetNode<ProgressBar>("Control/HealthBar");
        _xpBar      = GetNode<ProgressBar>("Control/XpBar");
        _levelLabel = GetNode<Label>("Control/LevelLabel");
        _timerLabel = GetNode<Label>("Control/TimerLabel");
        _coinLabel  = GetNode<Label>("Control/CoinLabel");

        StyleBar(_healthBar, new Color(0.8f, 0.15f, 0.15f));
        StyleBar(_xpBar, new Color(0.1f, 0.7f, 0.2f));

        var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        if (player != null)
        {
            _healthBar.MaxValue = player.MaxHealth;
            _healthBar.Value    = player.CurrentHealth;
            _xpBar.MaxValue     = player.XpToNextLevel;
            _xpBar.Value        = player.CurrentXp;
            _levelLabel.Text    = $"Level {player.Level}";

            player.HealthChanged += OnHealthChanged;
            player.XpChanged     += OnXpChanged;
            player.LeveledUp     += OnLeveledUp;
        }

        _session = GetParent().GetNodeOrNull<Run.RunSession>("RunSession");
        if (_session != null)
            _session.CoinChanged += OnCoinChanged;

        _coinLabel.Text  = "Coins: 0";
        _timerLabel.Text = "0:00";
    }

    public override void _Process(double delta)
    {
        if (_session == null) return;
        int secs = (int)_session.ElapsedTime;
        _timerLabel.Text = $"{secs / 60}:{secs % 60:D2}";
    }

    private void OnHealthChanged(int newHealth) => _healthBar.Value = newHealth;

    private void OnXpChanged(int currentXp, int xpToNextLevel)
    {
        _xpBar.MaxValue = xpToNextLevel;
        _xpBar.Value    = currentXp;
    }

    private void OnLeveledUp(int newLevel) => _levelLabel.Text = $"Level {newLevel}";

    private void OnCoinChanged(int total) => _coinLabel.Text = $"Coins: {total}";

    private static void StyleBar(ProgressBar bar, Color fillColor)
    {
        var fill = new StyleBoxFlat { BgColor = fillColor };
        var bg   = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f) };
        bar.AddThemeStyleboxOverride("fill", fill);
        bar.AddThemeStyleboxOverride("background", bg);
    }
}
