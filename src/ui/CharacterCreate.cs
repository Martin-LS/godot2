using Godot;
using System.Linq;
using System.Text.RegularExpressions;
using Godot1.Character;

namespace Godot1.Ui;

public partial class CharacterCreate : Control
{
    private CharacterType _pendingType = CharacterType.Warrior;

    private static readonly Regex AlphaNumeric = new(@"^[a-zA-Z0-9]+$");

    public override void _Ready()
    {
        var nameInput  = GetNode<LineEdit>("VBox/NameInput");
        var confirmBtn = GetNode<Button>("VBox/ConfirmBtn");
        var errorLabel = GetNode<Label>("VBox/ErrorLabel");
        var manager    = GetNode<CharacterManager>("/root/CharacterManager");

        nameInput.TextChanged += text =>
        {
            var error = Validate(text, manager);
            errorLabel.Text    = error ?? "";
            errorLabel.Visible = error != null;
            confirmBtn.Disabled = error != null;
        };

        GetNode<Button>("VBox/WarriorBtn").Pressed += () => _pendingType = CharacterType.Warrior;
        GetNode<Button>("VBox/RogueBtn").Pressed   += () => _pendingType = CharacterType.Rogue;
        GetNode<Button>("VBox/MageBtn").Pressed    += () => _pendingType = CharacterType.Mage;

        confirmBtn.Disabled = true;
        confirmBtn.Pressed += () =>
        {
            var name = nameInput.Text;
            if (Validate(name, manager) != null) return;
            manager.Create(name, _pendingType);
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");
        };

        GetNode<Button>("VBox/CancelBtn").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");
    }

    private static string? Validate(string name, CharacterManager manager)
    {
        if (name.Length == 0)                              return "Name is required.";
        if (!AlphaNumeric.IsMatch(name))                   return "Letters and numbers only, no spaces.";
        if (manager.GetAll().Any(c => c.Name == name))      return "Name already taken.";
        return null;
    }
}
