using Godot;
using System.Linq;
using Godot1.Crafting;
using Godot1.Items;
using Godot1.Stats;

namespace Godot1.Ui;

public partial class CharacterScreen : Control
{
    private Label         _inventoryInfo = null!;
    private GridContainer _inventoryGrid = null!;

    private Label  _nameLabel  = null!;
    private Label  _typeLabel  = null!;
    private Label  _levelLabel = null!;
    private Label  _statsLabel = null!;
    private Button _weaponBtn  = null!;
    private Button _armorBtn   = null!;
    private Button _accBtn     = null!;

    private VBoxContainer _craftVBox = null!;

    private Character.CharacterManager _manager = null!;

    private const string CharViewBase = "VBox/HSplit/RightPanel/TabContainer/Equipment/CharacterView";
    private const string CraftingBase = "VBox/HSplit/RightPanel/TabContainer/Crafting";

    public override void _Ready()
    {
        _manager = GetNode<Character.CharacterManager>("/root/CharacterManager");

        if (_manager.SelectedCharacter == null)
        {
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");
            return;
        }

        _inventoryInfo = GetNode<Label>        ("VBox/HSplit/LeftPanel/LeftVBox/InventoryInfo");
        _inventoryGrid = GetNode<GridContainer>("VBox/HSplit/LeftPanel/LeftVBox/InventoryScroll/InventoryGrid");

        _nameLabel  = GetNode<Label>  ($"{CharViewBase}/HSplit/InfoVBox/NameLabel");
        _typeLabel  = GetNode<Label>  ($"{CharViewBase}/HSplit/InfoVBox/TypeLabel");
        _levelLabel = GetNode<Label>  ($"{CharViewBase}/HSplit/InfoVBox/LevelLabel");
        _statsLabel = GetNode<Label>  ($"{CharViewBase}/HSplit/InfoVBox/StatsLabel");
        _weaponBtn  = GetNode<Button> ($"{CharViewBase}/HSplit/GearPanel/WeaponSlot/WeaponSlotButton");
        _armorBtn   = GetNode<Button> ($"{CharViewBase}/HSplit/GearPanel/ArmorSlot/ArmorSlotButton");
        _accBtn     = GetNode<Button> ($"{CharViewBase}/HSplit/GearPanel/AccessorySlot/AccessorySlotButton");

        _weaponBtn.Pressed += () => OnGearSlotPressed(ItemSlot.Weapon);
        _armorBtn.Pressed  += () => OnGearSlotPressed(ItemSlot.Armor);
        _accBtn.Pressed    += () => OnGearSlotPressed(ItemSlot.Accessory);

        GetNode<Button>($"{CharViewBase}/HSplit/InfoVBox/Buttons/StartRunButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://main.tscn");

        _craftVBox = GetNode<VBoxContainer>($"{CraftingBase}/VBox");

        GetNode<Button>("VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/character_select.tscn");

        Refresh();
    }

    private void Refresh()
    {
        RefreshInventory();
        RefreshCharacter();
        RefreshCrafting();
    }

    private void RefreshCharacter()
    {
        var c     = _manager.SelectedCharacter!;
        var stats = c.BuildStatBlock();

        _nameLabel.Text  = c.Name;
        _typeLabel.Text  = c.Type.ToString();
        _levelLabel.Text = $"Level {c.CurrentLevel}   XP: {c.CurrentXp}";
        _statsLabel.Text = $"HP {(int)stats.Get(StatId.MaxHp)}   Speed {stats.Get(StatId.Speed):F0}   Damage {stats.Get(StatId.Damage):F0}\nRuns: {c.RunsCompleted}";

        RefreshSlotButton(_weaponBtn, c, ItemSlot.Weapon,    "Weapon");
        RefreshSlotButton(_armorBtn,  c, ItemSlot.Armor,     "Armor");
        RefreshSlotButton(_accBtn,    c, ItemSlot.Accessory, "Accessory");
    }

    private static void RefreshSlotButton(Button btn, Character.CharacterData c, ItemSlot slot, string slotName)
    {
        if (c.EquippedItems.TryGetValue(slot.ToString(), out var id))
        {
            var item = ItemRegistry.Get(id);
            if (item != null)
            {
                btn.Text        = "";
                btn.Icon        = !string.IsNullOrEmpty(item.IconPath) ? GD.Load<Texture2D>(item.IconPath) : null;
                btn.ExpandIcon  = true;
                btn.TooltipText = BuildTooltip(item);
                btn.Modulate    = Colors.White;
                return;
            }
        }

        btn.Icon        = null;
        btn.ExpandIcon  = false;
        btn.Text        = "—";
        btn.TooltipText = $"{slotName}: Empty";
        btn.Modulate    = new Color(1f, 1f, 1f, 0.4f);
    }

    private void RefreshInventory()
    {
        var profile = _manager.Profile;
        _inventoryInfo.Text = $"{profile.OwnedItemIds.Count} / {Character.ProfileData.MaxInventory}   Coins: {profile.CoinBank}   Common: {profile.GetMaterial("crafting_common")}";

        foreach (Node child in _inventoryGrid.GetChildren())
            child.QueueFree();

        for (int i = 0; i < Character.ProfileData.MaxInventory; i++)
        {
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(60, 60),
            };

            if (i < profile.OwnedItemIds.Count)
            {
                var id   = profile.OwnedItemIds[i];
                var item = ItemRegistry.Get(id);
                if (item != null)
                {
                    if (!string.IsNullOrEmpty(item.IconPath))
                    {
                        btn.Icon       = GD.Load<Texture2D>(item.IconPath);
                        btn.ExpandIcon = true;
                    }
                    else
                    {
                        btn.Text = item.Name;
                        btn.AddThemeFontSizeOverride("font_size", 10);
                    }
                    btn.TooltipText = BuildTooltip(item);
                    var capturedId   = id;
                    var capturedItem = item;
                    var capturedBtn  = btn;
                    btn.Pressed += () => ShowInventoryItemPopup(capturedId, capturedItem, capturedBtn);
                }
            }
            else
            {
                btn.Modulate = new Color(1f, 1f, 1f, 0.3f);
                btn.Disabled = true;
            }

            _inventoryGrid.AddChild(btn);
        }

    }

    private void RefreshCrafting()
    {
        foreach (Node child in _craftVBox.GetChildren())
            child.QueueFree();

        var matsLabel = new Label
        {
            Text = $"Common materials: {_manager.Profile.GetMaterial("crafting_common")}",
        };
        _craftVBox.AddChild(matsLabel);

        bool inventoryFull = _manager.Profile.OwnedItemIds.Count >= Character.ProfileData.MaxInventory;

        foreach (var recipe in RecipeRegistry.All.Values)
        {
            var item = ItemRegistry.Get(recipe.OutputItemId);
            if (item == null) continue;

            string costText = string.Join(", ", recipe.MaterialCosts.Select(kv =>
                $"{kv.Value}× {(kv.Key == "crafting_common" ? "Common" : kv.Key)}"));
            bool canAfford = recipe.MaterialCosts.All(kv => _manager.Profile.GetMaterial(kv.Key) >= kv.Value);

            var btn = new Button
            {
                Text     = $"{item.Name}  —  {costText}",
                Disabled = !canAfford || inventoryFull,
            };
            string capturedId = recipe.Id;
            btn.Pressed += () => { _manager.CraftItem(capturedId); Refresh(); };
            _craftVBox.AddChild(btn);
        }
    }

    private static string BuildTooltip(ItemData item)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{item.Name}  [{item.Slot}]");
        if (item.BonusHp            != 0)   sb.Append($"\nHP {item.BonusHp:+#;-#;0}");
        if (item.BonusSpeed         != 0f)  sb.Append($"\nSpeed {item.BonusSpeed:+#;-#;0}");
        if (item.SkillBonus         != 0f)  sb.Append($"\nSkill Bonus {item.SkillBonus:+#;-#;0}");
        if (item.DamageReduction    != 0f)  sb.Append($"\nDamage Reduction {item.DamageReduction:P0}");
        if (item.PhysicalResistance != 0f)  sb.Append($"\nPhys. Resist {item.PhysicalResistance:P0}");
        return sb.ToString();
    }

    private void OnGearSlotPressed(ItemSlot slot)
    {
        var c = _manager.SelectedCharacter;
        if (c == null) return;
        var anchor = slot switch
        {
            ItemSlot.Weapon => _weaponBtn,
            ItemSlot.Armor  => _armorBtn,
            _               => _accBtn,
        };
        if (c.EquippedItems.ContainsKey(slot.ToString()))
            ShowEquippedItemPopup(slot, anchor);
        else
            OpenPicker(slot);
    }

    private void ShowInventoryItemPopup(string itemId, ItemData item, Button anchor)
    {
        var popup = new PopupMenu();
        var c     = _manager.SelectedCharacter;

        if (c != null) popup.AddItem("Equip",  0);
        popup.AddItem("Delete", 1);

        popup.IdPressed += (long id) =>
        {
            if (id == 0 && c != null)
                _manager.EquipItem(c.Id, item.Slot, itemId);
            else if (id == 1)
                _manager.DeleteItem(itemId);
            Refresh();
        };

        ShowPopupAt(popup, anchor);
    }

    private void ShowEquippedItemPopup(ItemSlot slot, Button anchor)
    {
        var c = _manager.SelectedCharacter!;
        if (!c.EquippedItems.TryGetValue(slot.ToString(), out var itemId)) return;

        bool inventoryFull = _manager.Profile.OwnedItemIds.Count >= Character.ProfileData.MaxInventory;
        var  popup         = new PopupMenu();

        popup.AddItem(inventoryFull ? "Unequip  (inventory full)" : "Unequip", 0);
        if (inventoryFull) popup.SetItemDisabled(0, true);
        popup.AddItem("Delete", 1);

        popup.IdPressed += (long id) =>
        {
            if (id == 0)
                _manager.UnequipItem(c.Id, slot);
            else if (id == 1)
                _manager.DeleteItem(itemId);
            Refresh();
        };

        ShowPopupAt(popup, anchor);
    }

    private void ShowPopupAt(PopupMenu popup, Button anchor)
    {
        AddChild(popup);
        popup.PopupHide += popup.QueueFree;
        popup.ResetSize();
        var rect = anchor.GetGlobalRect();
        popup.PopupOnParent(new Rect2I((int)rect.Position.X, (int)rect.Position.Y, (int)rect.Size.X, (int)rect.Size.Y));
    }

    private void OpenPicker(ItemSlot slot)
    {
        var pickerScene = GD.Load<PackedScene>("res://src/ui/item_picker_panel.tscn");
        var picker      = pickerScene.Instantiate<ItemPickerPanel>();
        picker.Init(_manager, _manager.SelectedCharacter!, slot, () => Refresh());
        AddChild(picker);
    }
}
