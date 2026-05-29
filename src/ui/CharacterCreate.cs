using Godot;
using Godot1.Character;

namespace Godot1.Ui;

public partial class CharacterCreate : Control
{
    private CharacterType _pendingType = CharacterType.Warrior;

    public override void _Ready()
    {
        var nameInput  = GetNode<LineEdit>("VBox/NameInput");
        var confirmBtn = GetNode<Button>("VBox/ConfirmBtn");

        nameInput.TextChanged += text => confirmBtn.Disabled = text.Trim().Length == 0;

        GetNode<Button>("VBox/WarriorBtn").Pressed += () => _pendingType = CharacterType.Warrior;
        GetNode<Button>("VBox/RogueBtn").Pressed   += () => _pendingType = CharacterType.Rogue;
        GetNode<Button>("VBox/MageBtn").Pressed    += () => _pendingType = CharacterType.Mage;

        confirmBtn.Pressed += () =>
        {
            var name = nameInput.Text.Trim();
            if (name.Length == 0) return;
            GetNode<CharacterManager>("/root/CharacterManager").Create(name, _pendingType);
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
        };

        GetNode<Button>("VBox/CancelBtn").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
    }
}
