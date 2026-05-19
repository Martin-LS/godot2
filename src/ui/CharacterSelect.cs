using Godot;
using Godot1.Character;
using System.Linq;

namespace Godot1.Ui;

public partial class CharacterSelect : Control
{
    private VBoxContainer _characterList = null!;
    private Button _startRunButton = null!;
    private Button _upgradesButton = null!;
    private Panel _createPanel = null!;
    private MetaUpgradesPanel _metaPanel = null!;
    private LineEdit _nameInput = null!;
    private CharacterType _pendingType = CharacterType.Warrior;
    private string? _selectedId;

    public override void _Ready()
    {
        _characterList  = GetNode<VBoxContainer>("HSplit/Left/Scroll/CharacterList");
        _startRunButton = GetNode<Button>("HSplit/Left/StartRunButton");
        _upgradesButton = GetNode<Button>("HSplit/Left/UpgradesButton");
        _createPanel    = GetNode<Panel>("HSplit/Right/CreatePanel");
        _metaPanel      = GetNode<MetaUpgradesPanel>("HSplit/Right/MetaUpgradesPanel");
        _nameInput      = GetNode<LineEdit>("HSplit/Right/CreatePanel/VBox/NameInput");

        GetNode<Button>("HSplit/Left/NewCharacterButton").Pressed += () =>
        {
            _metaPanel.Visible = false;
            _createPanel.Visible = true;
        };

        GetNode<Button>("HSplit/Right/CreatePanel/VBox/WarriorBtn").Pressed += () => _pendingType = CharacterType.Warrior;
        GetNode<Button>("HSplit/Right/CreatePanel/VBox/RogueBtn").Pressed   += () => _pendingType = CharacterType.Rogue;
        GetNode<Button>("HSplit/Right/CreatePanel/VBox/MageBtn").Pressed    += () => _pendingType = CharacterType.Mage;
        GetNode<Button>("HSplit/Right/CreatePanel/VBox/ConfirmBtn").Pressed += OnConfirmCreate;
        GetNode<Button>("HSplit/Right/CreatePanel/VBox/CancelBtn").Pressed  += () => _createPanel.Visible = false;

        _startRunButton.Pressed  += OnStartRun;
        _upgradesButton.Pressed  += OnUpgrades;

        _startRunButton.Disabled = true;
        _upgradesButton.Disabled = true;
        _createPanel.Visible = false;
        RefreshList();
    }

    private void OnConfirmCreate()
    {
        var name = _nameInput.Text.Trim();
        if (name.Length == 0) return;

        GetNode<CharacterManager>("/root/CharacterManager").Create(name, _pendingType);
        _nameInput.Text = "";
        _createPanel.Visible = false;
        RefreshList();
    }

    private void OnUpgrades()
    {
        _createPanel.Visible = false;
        var manager = GetNode<CharacterManager>("/root/CharacterManager");
        var c = manager.GetAll().FirstOrDefault(x => x.Id == _selectedId);
        if (c == null) return;
        _metaPanel.Refresh(c);
        _metaPanel.Visible = true;
    }

    private void RefreshList()
    {
        foreach (Node child in _characterList.GetChildren())
            child.QueueFree();

        var manager = GetNode<CharacterManager>("/root/CharacterManager");
        foreach (var c in manager.GetAll())
            _characterList.AddChild(CreateCard(c, manager));

        bool valid = _selectedId != null && manager.GetAll().Any(c => c.Id == _selectedId);
        _startRunButton.Disabled  = !valid;
        _upgradesButton.Disabled  = !valid;
    }

    private Control CreateCard(CharacterData c, CharacterManager manager)
    {
        var hbox = new HBoxContainer();
        hbox.CustomMinimumSize = new Vector2(0, 48);

        var selectBtn = new Button
        {
            Text = $"{c.Name}  [{c.Type}]  Runs: {c.RunsCompleted}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        selectBtn.Pressed += () =>
        {
            _selectedId = c.Id;
            manager.SelectCharacter(c.Id);
            _startRunButton.Disabled = false;
            _upgradesButton.Disabled = false;
        };
        hbox.AddChild(selectBtn);

        var deleteBtn = new Button { Text = "X" };
        deleteBtn.Pressed += () =>
        {
            manager.Delete(c.Id);
            if (_selectedId == c.Id)
            {
                _selectedId = null;
                _startRunButton.Disabled = true;
                _upgradesButton.Disabled = true;
                _metaPanel.Visible = false;
            }
            RefreshList();
        };
        hbox.AddChild(deleteBtn);

        return hbox;
    }

    private void OnStartRun()
    {
        GetTree().ChangeSceneToFile("res://main.tscn");
    }
}
