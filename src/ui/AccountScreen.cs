using Godot;

namespace Godot1.Ui;

public partial class AccountScreen : Control
{
    private VBoxContainer _characterList = null!;
    private Control       _createPanel   = null!;

    private Character.CharacterManager _manager = null!;

    private const string RosterBase = "VBox/HSplit/RightPanel/TabContainer/Characters/RosterView";

    public override void _Ready()
    {
        _manager = GetNode<Character.CharacterManager>("/root/CharacterManager");

        // Characters tab — roster
        _characterList = GetNode<VBoxContainer>($"{RosterBase}/Scroll/CharacterList");
        _createPanel   = GetNode<Control>      ($"{RosterBase}/CreatePanel");

        GetNode<Button>($"{RosterBase}/NewCharacterButton").Pressed          += () => GetTree().ChangeSceneToFile("res://src/ui/character_create.tscn");
        GetNode<Button>($"{RosterBase}/CreatePanel/VBox/WarriorBtn").Pressed += () => CreateAndSelect(Character.CharacterType.Warrior);
        GetNode<Button>($"{RosterBase}/CreatePanel/VBox/RogueBtn").Pressed   += () => CreateAndSelect(Character.CharacterType.Rogue);
        GetNode<Button>($"{RosterBase}/CreatePanel/VBox/MageBtn").Pressed    += () => CreateAndSelect(Character.CharacterType.Mage);
        GetNode<Button>($"{RosterBase}/CreatePanel/VBox/CancelBtn").Pressed  += () => _createPanel.Visible = false;

        GetNode<Button>("VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/main_menu.tscn");

        Refresh();
    }

    private void Refresh()
    {
        RefreshRoster();
    }

    private void RefreshRoster()
    {
        foreach (Node child in _characterList.GetChildren())
            child.QueueFree();

        foreach (var c in _manager.GetAll())
        {
            var hbox = new HBoxContainer();
            var selectBtn = new Button
            {
                Text = $"{c.Name}  [{c.Type}]  Lv.{c.CurrentLevel}  Runs: {c.RunsCompleted}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            string capturedId = c.Id;
            selectBtn.Pressed += () =>
            {
                _manager.SelectCharacter(capturedId);
                GetTree().ChangeSceneToFile("res://src/ui/character_screen.tscn");
            };
            hbox.AddChild(selectBtn);

            var deleteBtn = new Button { Text = "X" };
            deleteBtn.Pressed += () =>
            {
                _manager.Delete(capturedId);
                Refresh();
            };
            hbox.AddChild(deleteBtn);
            _characterList.AddChild(hbox);
        }
    }

    private void CreateAndSelect(Character.CharacterType type)
    {
        var c = _manager.Create(type.ToString(), type);
        _manager.SelectCharacter(c.Id);
        GetTree().ChangeSceneToFile("res://src/ui/character_screen.tscn");
    }
}
