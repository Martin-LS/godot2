using Godot;
using System;
using Godot1.Items;

namespace Godot1.Ui;

public partial class ItemPickerPanel : Control
{
    private Character.CharacterManager _manager   = null!;
    private Character.CharacterData    _character = null!;
    private ItemSlot                   _slot;
    private Action?                    _onClose;

    private Label         _titleLabel = null!;
    private VBoxContainer _itemList   = null!;
    private Button        _unequipBtn = null!;
    private Button        _closeBtn   = null!;

    // Must be called before AddChild so fields are set before _Ready fires.
    public void Init(
        Character.CharacterManager manager,
        Character.CharacterData    character,
        ItemSlot                   slot,
        Action?                    onClose = null)
    {
        _manager   = manager;
        _character = character;
        _slot      = slot;
        _onClose   = onClose;
    }

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>        ("Panel/VBox/TitleLabel");
        _itemList   = GetNode<VBoxContainer>("Panel/VBox/Scroll/ItemList");
        _unequipBtn = GetNode<Button>       ("Panel/VBox/UnequipButton");
        _closeBtn   = GetNode<Button>       ("Panel/VBox/CloseButton");

        _titleLabel.Text = $"Choose {_slot}";

        _closeBtn.Pressed += Close;

        // Unequip is only relevant when the slot is currently occupied.
        _unequipBtn.Visible = _character.EquippedItems.ContainsKey(_slot.ToString());
        _unequipBtn.Pressed += () =>
        {
            if (_manager.UnequipItem(_character.Id, _slot))
                Close();
            else
                _titleLabel.Text = "Inventory full — cannot unequip";
        };

        PopulateItems();
    }

    private void PopulateItems()
    {
        foreach (Node child in _itemList.GetChildren())
            child.QueueFree();

        string? equippedId = _character.EquippedItems.TryGetValue(_slot.ToString(), out var eid) ? eid : null;

        foreach (var itemId in _manager.Profile.OwnedItemIds)
        {
            var item = ItemRegistry.Get(itemId);
            if (item == null || item.Slot != _slot) continue;

            string label = BuildLabel(item, item.Id == equippedId);
            var btn = new Button { Text = label };
            string capturedId = itemId;
            btn.Pressed += () =>
            {
                _manager.EquipItem(_character.Id, _slot, capturedId);
                Close();
            };
            _itemList.AddChild(btn);
        }

        if (_itemList.GetChildCount() == 0)
            _itemList.AddChild(new Label { Text = "No items owned for this slot." });
    }

    private static string BuildLabel(ItemData item, bool equipped)
    {
        string parts = "";
        if (item.BonusHp            != 0)   parts += $" HP {item.BonusHp:+#;-#;0}";
        if (item.BonusSpeed         != 0f)  parts += $" Spd {item.BonusSpeed:+#;-#;0}";
        if (item.SkillBonus         != 0f)  parts += $" Skill {item.SkillBonus:+#;-#;0}";
        if (item.DamageReduction    != 0f)  parts += $" DR {item.DamageReduction:P0}";
        if (item.PhysicalResistance != 0f)  parts += $" PR {item.PhysicalResistance:P0}";
        string tag = equipped ? " [equipped]" : "";
        return $"{item.Name}{parts}{tag}";
    }

    private void Close()
    {
        _onClose?.Invoke();
        QueueFree();
    }
}
