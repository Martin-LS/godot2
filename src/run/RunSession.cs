using Godot;
using Godot1.Player;

namespace Godot1.Run;

public partial class RunSession : Node
{
    [Export] public float RunDuration = 5f;

    [Signal] public delegate void RunEndedEventHandler(bool won, int levelReached, float elapsed);
    [Signal] public delegate void CoinChangedEventHandler(int total);

    public int CoinsEarned { get; private set; }
    public float ElapsedTime => _elapsed;

    private float _elapsed;
    private bool  _ended;

    public override void _Ready()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (player != null)
            player.PlayerDied += () => EndRun(false);
    }

    public override void _Process(double delta)
    {
        if (_ended) return;
        _elapsed += (float)delta;
        if (_elapsed >= RunDuration)
            EndRun(true);
    }

    public void AddCoin(int amount)
    {
        CoinsEarned += amount;
        EmitSignal(SignalName.CoinChanged, CoinsEarned);
    }

    private void EndRun(bool won)
    {
        if (_ended) return;
        _ended = true;

        var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        int level = player?.Level ?? 1;
        EmitSignal(SignalName.RunEnded, won, level, _elapsed);
    }
}
