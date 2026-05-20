using Godot;

namespace Godot1.Ui;

public partial class CharacterScreen : Control
{
    private Label  _nameLabel  = null!;
    private Label  _typeLabel  = null!;
    private Label  _levelLabel = null!;
    private Label  _statsLabel = null!;

    public override void _Ready()
    {
        _nameLabel  = GetNode<Label> ("VBox/NameLabel");
        _typeLabel  = GetNode<Label> ("VBox/TypeLabel");
        _levelLabel = GetNode<Label> ("VBox/LevelLabel");
        _statsLabel = GetNode<Label> ("VBox/StatsLabel");

        GetNode<Button>("VBox/Buttons/BackButton").Pressed     += () =>
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
        GetNode<Button>("VBox/Buttons/StartRunButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://main.tscn");

        var manager = GetNode<Character.CharacterManager>("/root/CharacterManager");
        var c = manager.SelectedCharacter;
        if (c == null)
        {
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
            return;
        }

        var (hp, spd, dmg) = c.BaseStats();
        int levelsAboveOne = c.CurrentLevel - 1;
        int   totalHp  = hp  + levelsAboveOne * 5;
        float totalDmg = dmg + levelsAboveOne;

        _nameLabel.Text  = c.Name;
        _typeLabel.Text  = c.Type.ToString();
        _levelLabel.Text = $"Level {c.CurrentLevel}   XP: {c.CurrentXp}";
        _statsLabel.Text = $"HP {totalHp}   Speed {spd:F0}   Damage {totalDmg:F0}\nRuns: {c.RunsCompleted}   Coins: {c.CoinBank}";
    }
}
