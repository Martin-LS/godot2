using Godot;
using System.Collections.Generic;
using Godot1.Enemies;
using Godot1.Player;

namespace Godot1.Hud;

public partial class WorldHud : Node2D
{
    private const float BarWidth       = 50f;
    private const float BarHeight      = 7f;
    private const float HeadScreenOffY = -48f;

    private static readonly Color PlayerBarFill = Color.FromHtml("#A32D2D");
    private static readonly Color EnemyBarFill  = Color.FromHtml("#C03030");
    private static readonly Color BarTrack      = Color.FromHtml("#181C1F");
    private static readonly Color PhysColor     = Colors.White;
    private static readonly Color MagicColor    = Colors.White;
    private static readonly Color CritColor     = Colors.White;

    private Camera3D?         _camera;
    private PlayerController? _player;
    private Font?             _dmgFont;

    private readonly HashSet<EnemyController> _connectedEnemies = new();
    private readonly List<(Vector3 pos, float cur, float max)> _liveBarData = new();

    private sealed class DeadBar
    {
        public Vector3 WorldPos;
        public float   Current;
        public float   Max;
        public float   Timer;
        public const float Lifetime = 2f;
    }
    private readonly Dictionary<ulong, DeadBar> _deadBars = new();

    public override void _Ready()
    {
        var playerNode = GetTree().GetFirstNodeInGroup("player");
        _player = playerNode as PlayerController;
        _camera = playerNode?.GetNodeOrNull<Camera3D>("Camera3D");
        _dmgFont = GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_1.ttf");

        if (_player != null)
            _player.DamageTaken += OnPlayerDamageTaken;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _liveBarData.Clear();
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyController ec) continue;
            if (!IsInstanceValid(ec) || ec.IsQueuedForDeletion()) continue;

            _liveBarData.Add((ec.GlobalPosition, ec.CurrentHealth, ec.MaxHealth));

            if (_connectedEnemies.Add(ec))
            {
                ec.DamageTaken += (dmg, isMagic, isCrit) => OnEnemyDamageTaken(ec, dmg, isMagic, isCrit);
                ec.TreeExited  += () => _connectedEnemies.Remove(ec);
            }
        }

        var expired = new List<ulong>();
        foreach (var (id, bar) in _deadBars)
        {
            bar.Timer -= dt;
            if (bar.Timer <= 0f) expired.Add(id);
        }
        foreach (var id in expired) _deadBars.Remove(id);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_camera == null) return;

        if (_player != null && IsInstanceValid(_player))
        {
            var sp = Project(_player.GlobalPosition);
            if (sp.HasValue)
                DrawBar(sp.Value + new Vector2(0f, HeadScreenOffY), _player.CurrentHealth, _player.MaxHealth, PlayerBarFill);
        }

        foreach (var (pos, cur, max) in _liveBarData)
        {
            var sp = Project(pos);
            if (sp.HasValue)
                DrawBar(sp.Value + new Vector2(0f, HeadScreenOffY), cur, max, EnemyBarFill);
        }

        foreach (var bar in _deadBars.Values)
        {
            var sp = Project(bar.WorldPos);
            if (sp.HasValue)
                DrawBar(sp.Value + new Vector2(0f, HeadScreenOffY), bar.Current, bar.Max, EnemyBarFill);
        }
    }

    private void DrawBar(Vector2 center, float current, float max, Color fillColor)
    {
        float pct = max > 0f ? Mathf.Clamp(current / max, 0f, 1f) : 0f;
        var   tl  = center - new Vector2(BarWidth / 2f, BarHeight / 2f);
        DrawRect(new Rect2(tl, new Vector2(BarWidth, BarHeight)), BarTrack);
        if (pct > 0f)
            DrawRect(new Rect2(tl, new Vector2(BarWidth * pct, BarHeight)), fillColor);
    }

    private Vector2? Project(Vector3 worldPos)
    {
        if (!IsInstanceValid(_camera)) return null;
        if (_camera!.IsPositionBehind(worldPos)) return null;
        return _camera.UnprojectPosition(worldPos);
    }

    private void OnEnemyDamageTaken(EnemyController ec, float damage, bool isMagic, bool isCrit)
    {
        float healthAfter = Mathf.Max(0f, ec.CurrentHealth - Mathf.CeilToInt(damage));

        var id = ec.GetInstanceId();
        if (_deadBars.TryGetValue(id, out var bar))
        {
            bar.Timer    = DeadBar.Lifetime;
            bar.WorldPos = ec.GlobalPosition;
            bar.Current  = healthAfter;
        }
        else
        {
            _deadBars[id] = new DeadBar
            {
                WorldPos = ec.GlobalPosition,
                Current  = healthAfter,
                Max      = ec.MaxHealth,
                Timer    = DeadBar.Lifetime,
            };
        }

        var sp = Project(ec.GlobalPosition);
        if (sp.HasValue) SpawnNum(sp.Value + new Vector2(0f, HeadScreenOffY), damage, isMagic, isCrit);
    }

    private void OnPlayerDamageTaken(float damage, bool isMagic)
    {
        if (_player == null || !IsInstanceValid(_player)) return;
        var sp = Project(_player.GlobalPosition);
        if (sp.HasValue) SpawnNum(sp.Value + new Vector2(0f, HeadScreenOffY), damage, isMagic, false);
    }

    private void SpawnNum(Vector2 screenPos, float damage, bool isMagic, bool isCrit)
    {
        var color   = isCrit ? CritColor : (isMagic ? MagicColor : PhysColor);
        float dur   = isCrit ? 1.1f : 0.7f;
        float riseY = isCrit ? -95f : -65f;
        float drift = (float)GD.RandRange(-20.0, 20.0);

        var settings = new LabelSettings
        {
            Font         = _dmgFont,
            FontSize     = isCrit ? 40 : 26,
            FontColor    = color,
            OutlineSize  = isCrit ? 5 : 3,
            OutlineColor = new Color(0f, 0f, 0f, 0.9f),
        };

        var lbl = new Label
        {
            Text                = Mathf.CeilToInt(damage).ToString(),
            LabelSettings       = settings,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Off,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        AddChild(lbl);
        // Strip any theme background the global game theme might inject into Labels
        var emptyBox = new StyleBoxEmpty();
        lbl.AddThemeStyleboxOverride("normal", emptyBox);
        lbl.AddThemeStyleboxOverride("focus", emptyBox);

        // Centre on spawn position after size is known
        var size     = lbl.GetMinimumSize();
        var startPos = screenPos - size / 2f;
        var endPos   = startPos + new Vector2(drift, riseY);
        lbl.Position = startPos;

        // D3-style scale pop: burst in large, snap back to normal
        lbl.Scale = new Vector2(0.25f, 0.25f);
        var scaleTween = lbl.CreateTween();
        scaleTween.TweenProperty(lbl, "scale", new Vector2(isCrit ? 1.35f : 1.2f, isCrit ? 1.35f : 1.2f), 0.12f)
                  .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        scaleTween.TweenProperty(lbl, "scale", Vector2.One, 0.1f)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        // Rise with easing — fast start, slow at top
        var posTween = lbl.CreateTween();
        posTween.TweenProperty(lbl, "position", endPos, dur)
                .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);

        // Fade out in the last 40%, then free
        var fadeTween = lbl.CreateTween();
        fadeTween.TweenInterval(dur * 0.6f);
        fadeTween.TweenProperty(lbl, "modulate:a", 0f, dur * 0.4f)
                 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        fadeTween.TweenCallback(Callable.From(lbl.QueueFree));
    }
}
