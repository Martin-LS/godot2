using Godot;
using Godot1.Character;
using Godot1.Meta;
using System.Linq;

namespace Godot1.Ui;

public partial class MetaUpgradesPanel : Panel
{
    private Label _coinLabel = null!;
    private VBoxContainer _upgradesVBox = null!;
    private CharacterManager _manager = null!;
    private string? _characterId;

    public override void _Ready()
    {
        _coinLabel    = GetNode<Label>("VBox/CoinLabel");
        _upgradesVBox = GetNode<VBoxContainer>("VBox/UpgradesVBox");
        _manager      = GetNode<CharacterManager>("/root/CharacterManager");

        GetNode<Button>("VBox/CloseButton").Pressed += () => Visible = false;

        Visible = false;
    }

    public void Refresh(CharacterData c)
    {
        _characterId = c.Id;
        _coinLabel.Text = $"Coins: {c.CoinBank}";

        foreach (Node child in _upgradesVBox.GetChildren())
            child.QueueFree();

        AddRow(c, MetaUpgradeType.MaxHealth, "Max Health", $"+{c.BonusMaxHealth} HP",   c.BonusMaxHealth / 10);
        AddRow(c, MetaUpgradeType.Speed,     "Speed",      $"+{c.BonusSpeed} spd",      (int)(c.BonusSpeed / 10f));
        AddRow(c, MetaUpgradeType.Damage,    "Damage",     $"+{c.BonusDamage} dmg",     (int)(c.BonusDamage / 2f));
    }

    private void AddRow(CharacterData c, MetaUpgradeType type, string name, string bonus, int level)
    {
        var hbox = new HBoxContainer();
        hbox.CustomMinimumSize = new Vector2(0, 40);

        var lbl = new Label
        {
            Text = $"{name}  Lv {level}/5  ({bonus})",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        hbox.AddChild(lbl);

        bool maxed = level >= 5;
        int cost = (level + 1) * 50;

        var btn = new Button
        {
            Text     = maxed ? "MAX" : $"{cost} coins",
            Disabled = maxed || c.CoinBank < cost,
            CustomMinimumSize = new Vector2(100, 0)
        };
        btn.Pressed += () =>
        {
            if (_characterId == null) return;
            _manager.PurchaseUpgrade(_characterId, type);
            var updated = _manager.GetAll().FirstOrDefault(x => x.Id == _characterId);
            if (updated != null) Refresh(updated);
        };
        hbox.AddChild(btn);

        _upgradesVBox.AddChild(hbox);
    }
}
