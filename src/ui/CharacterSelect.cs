using Godot;
using Godot1.Character;

namespace Godot1.Ui;

public partial class CharacterSelect : Control
{
    private VBoxContainer _characterList = null!;

    public override void _Ready()
    {
        _characterList = GetNode<VBoxContainer>("VBox/Scroll/CharacterList");

        GetNode<Button>("VBox/NewCharacterButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/character_create.tscn");

        GetNode<Button>("VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/main_menu.tscn");

        RefreshList();
    }

    private void RefreshList()
    {
        foreach (Node child in _characterList.GetChildren())
            child.QueueFree();

        var manager = GetNode<CharacterManager>("/root/CharacterManager");
        foreach (var c in manager.GetAll())
            _characterList.AddChild(CreateCard(c, manager));
    }

    private Control CreateCard(CharacterData c, CharacterManager manager)
    {
        var hbox = new HBoxContainer();
        hbox.CustomMinimumSize = new Vector2(0, 48);

        var selectBtn = new Button
        {
            Text = $"{c.Name}  [{c.Type}]  Lv.{c.CurrentLevel}  Runs: {c.RunsCompleted}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        selectBtn.Pressed += () =>
        {
            manager.SelectCharacter(c.Id);
            GetTree().ChangeSceneToFile("res://src/ui/character_screen.tscn");
        };
        hbox.AddChild(selectBtn);

        var deleteBtn = new Button { Text = "X" };
        deleteBtn.Pressed += () =>
        {
            manager.Delete(c.Id);
            RefreshList();
        };
        hbox.AddChild(deleteBtn);

        return hbox;
    }
}
