using Godot;
using System;
using System.Linq;
using Godot1.Player;
using Godot1.Weapon;

namespace Godot1.Ui;

public partial class UpgradePicker : CanvasLayer
{
    private static readonly UpgradeOption[] Pool =
    [
        new("Toughness",    "+25 Max Health",        UpgradeEffect.MaxHealth, 25f),
        new("Iron Skin",    "+50 Max Health",        UpgradeEffect.MaxHealth, 50f),
        new("Haste",        "+20 Movement Speed",    UpgradeEffect.Speed,     20f),
        new("Blitz",        "+40 Movement Speed",    UpgradeEffect.Speed,     40f),
        new("Power",        "+5 Weapon Damage",      UpgradeEffect.Damage,    5f),
        new("Brutality",    "+10 Weapon Damage",     UpgradeEffect.Damage,    10f),
        new("Rapid Fire",   "-0.1s Attack Cooldown", UpgradeEffect.FireRate,  0.1f),
        new("Swift Strike", "-0.2s Attack Cooldown", UpgradeEffect.FireRate,  0.2f),
    ];

    private Label   _title   = null!;
    private Button  _choice1 = null!, _choice2 = null!, _choice3 = null!;

    private UpgradeOption[] _current = Array.Empty<UpgradeOption>();
    private PlayerController?  _player;
    private WeaponController?  _weapon;

    public override void _Ready()
    {
        _title   = GetNode<Label>  ("Overlay/Panel/VBox/Title");
        _choice1 = GetNode<Button> ("Overlay/Panel/VBox/Choices/Choice1");
        _choice2 = GetNode<Button> ("Overlay/Panel/VBox/Choices/Choice2");
        _choice3 = GetNode<Button> ("Overlay/Panel/VBox/Choices/Choice3");

        _choice1.Pressed += () => Pick(0);
        _choice2.Pressed += () => Pick(1);
        _choice3.Pressed += () => Pick(2);

        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _weapon = _player?.GetNodeOrNull<WeaponController>("Weapon");

        if (_player != null)
            _player.LeveledUp += ShowChoices;

        Visible = false;
    }

    private void ShowChoices(int level)
    {
        if (Visible) return; // already open — ignore rapid multi-level

        _title.Text = $"Level {level}!";
        _current    = Pool.OrderBy(_ => GD.Randi()).Take(3).ToArray();
        _choice1.Text = Format(_current[0]);
        _choice2.Text = Format(_current[1]);
        _choice3.Text = Format(_current[2]);
        Visible = true;
        GetTree().Paused = true;
    }

    private void Pick(int i)
    {
        Apply(_current[i]);
        Visible = false;
        GetTree().Paused = false;
    }

    private void Apply(UpgradeOption opt)
    {
        if (_player == null) return;
        switch (opt.Effect)
        {
            case UpgradeEffect.MaxHealth:
                _player.MaxHealth += (int)opt.Value;
                _player.Heal((int)opt.Value);
                break;
            case UpgradeEffect.Speed:
                _player.Speed += opt.Value;
                break;
            case UpgradeEffect.Damage:
                if (_weapon != null) _weapon.Damage += opt.Value;
                break;
            case UpgradeEffect.FireRate:
                if (_weapon != null) _weapon.SetCooldown(MathF.Max(0.1f, _weapon.Cooldown - opt.Value));
                break;
        }
    }

    private static string Format(UpgradeOption o) => $"{o.DisplayName}\n{o.Description}";
}
