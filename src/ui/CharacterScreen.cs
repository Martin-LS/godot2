using Godot;
using System.Linq;
using Godot1.Crafting;
using Godot1.Items;
using Godot1.Skills;
using Godot1.Stats;

namespace Godot1.Ui;

public partial class CharacterScreen : Control
{
    private Label         _inventoryInfo = null!;
    private GridContainer _inventoryGrid = null!;
    private GridContainer _skillsGrid    = null!;
    private GridContainer _augmentsGrid  = null!;

    private Label  _nameLabel  = null!;
    private Label  _typeLabel  = null!;
    private Label  _levelLabel = null!;
    private Label  _statsLabel = null!;
    private Button _weaponBtn  = null!;
    private Button _hatBtn     = null!;
    private Button _bodyBtn    = null!;
    private Button _ringBtn    = null!;
    private readonly Button[] _skillBtns = new Button[3];

    private Character.CharacterManager _manager = null!;

    private const string CharViewBase = "VBox/TabContainer/Loadout/LoadoutSplit/CharacterView";
    private const string InvBase      = "VBox/TabContainer/Loadout/LoadoutSplit/InventoryPanel/InventoryVBox";

    public override void _Ready()
    {
        _manager = GetNode<Character.CharacterManager>("/root/CharacterManager");

        if (_manager.SelectedCharacter == null)
        {
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");
            return;
        }

        _inventoryInfo = GetNode<Label>         ($"{InvBase}/InventoryInfo");
        _inventoryGrid = GetNode<GridContainer> ($"{InvBase}/InventoryTabs/Equipment/InventoryScroll/InventoryGrid");
        _skillsGrid    = GetNode<GridContainer> ($"{InvBase}/InventoryTabs/Skills/SkillsScroll/SkillsGrid");
        _augmentsGrid  = GetNode<GridContainer> ($"{InvBase}/InventoryTabs/Augments/AugmentsScroll/AugmentsGrid");

        _nameLabel  = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/NameLabel");
        _typeLabel  = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/TypeLabel");
        _levelLabel = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/LevelLabel");
        _statsLabel = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/StatsLabel");
        _weaponBtn  = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/WeaponSlot/WeaponSlotButton");
        _hatBtn     = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/HatSlot/HatSlotButton");
        _bodyBtn    = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/BodySlot/BodySlotButton");
        _ringBtn    = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/RingSlot/RingSlotButton");

        _weaponBtn.GuiInput += (e) => OnGearSlotInput(e, ItemSlot.Weapon, _weaponBtn);
        _hatBtn.GuiInput    += (e) => OnGearSlotInput(e, ItemSlot.Hat,    _hatBtn);
        _bodyBtn.GuiInput   += (e) => OnGearSlotInput(e, ItemSlot.Body,   _bodyBtn);
        _ringBtn.GuiInput   += (e) => OnGearSlotInput(e, ItemSlot.Ring,   _ringBtn);

        for (int i = 0; i < 3; i++)
        {
            int captured = i;
            _skillBtns[i] = GetNode<Button>($"{CharViewBase}/SkillBar/SkillSlot{i + 1}/SkillSlotButton{i + 1}");
            _skillBtns[i].GuiInput += (e) => OnSkillSlotInput(e, captured);
        }

        GetNode<Button>($"{CharViewBase}/CraftButtons/CraftEquipmentButton").Pressed += ShowCraftEquipmentPopup;
        GetNode<Button>($"{CharViewBase}/CraftButtons/CraftSkillButton").Pressed    += ShowCraftSkillPopup;

        GetNode<Button>($"{CharViewBase}/Buttons/StartRunButton").Pressed += () =>
        {
            World.RunConfig.Pending = World.MapData.GenerateRandom(level: 1);
            GetTree().ChangeSceneToFile("res://main.tscn");
        };

        GetNode<Button>("VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");

        Refresh();
    }

    private void Refresh()
    {
        RefreshInventory();
        RefreshSkillsInventory();
        RefreshAugmentsInventory();
        RefreshCharacter();
    }

    // ── Character panel ───────────────────────────────────────────────────────

    private void RefreshCharacter()
    {
        var c     = _manager.SelectedCharacter!;
        var stats = c.BuildStatBlock();

        c.EquippedGear.TryGetValue(ItemSlot.Weapon.ToString(), out var weaponInst);
        c.EquippedGear.TryGetValue(ItemSlot.Hat.ToString(),    out var hatInst);
        c.EquippedGear.TryGetValue(ItemSlot.Body.ToString(),   out var bodyInst);
        float effectiveRange = (weaponInst?.Definition?.WeaponRange ?? 1.5f)
                             + (hatInst?.Definition?.RangeModifier  ?? 0f)
                             + (bodyInst?.Definition?.RangeModifier  ?? 0f);

        _nameLabel.Text  = c.Name;
        _typeLabel.Text  = c.Type.ToString();
        _levelLabel.Text = $"Level {c.CurrentLevel}   XP: {c.CurrentXp}";
        _statsLabel.Text = $"HP {(int)stats.Get(StatId.MaxHp)}   Speed {stats.Get(StatId.Speed):F0}   P.Dmg {stats.Get(StatId.PhysicalDamage):F0}   M.Dmg {stats.Get(StatId.MagicDamage):F0}\nRange {effectiveRange:0.#} tiles   Runs: {c.RunsCompleted}";

        RefreshSlotButton(_weaponBtn, c, ItemSlot.Weapon, "Weapon");
        RefreshSlotButton(_hatBtn,    c, ItemSlot.Hat,    "Hat");
        RefreshSlotButton(_bodyBtn,   c, ItemSlot.Body,   "Body");
        RefreshSlotButton(_ringBtn,   c, ItemSlot.Ring,   "Ring");

        for (int i = 0; i < 3; i++)
            RefreshSkillSlotButton(_skillBtns[i], c, i);
    }

    private static void RefreshSlotButton(Button btn, Character.CharacterData c, ItemSlot slot, string slotName)
    {
        if (c.EquippedGear.TryGetValue(slot.ToString(), out var instance))
        {
            var item = instance.Definition;
            if (item != null)
            {
                btn.Text        = "";
                btn.Icon        = !string.IsNullOrEmpty(item.IconPath) ? GD.Load<Texture2D>(item.IconPath) : null;
                btn.ExpandIcon  = true;
                btn.TooltipText = BuildGearTooltip(item, instance.Tier);
                btn.Modulate    = Colors.White;
                ApplyTierStyle(btn, instance.Tier);
                return;
            }
        }
        btn.Icon        = null;
        btn.ExpandIcon  = false;
        btn.Text        = "—";
        btn.TooltipText = $"{slotName}: Empty";
        btn.Modulate    = new Color(1f, 1f, 1f, 0.4f);
        ClearTierStyle(btn);
    }

    private void RefreshSkillSlotButton(Button btn, Character.CharacterData c, int slotIndex)
    {
        string? instanceId = slotIndex < c.SlottedSkillInstanceIds.Count ? c.SlottedSkillInstanceIds[slotIndex] : null;
        if (!string.IsNullOrEmpty(instanceId))
        {
            var instance = _manager.FindSkillInstance(instanceId);
            var skill    = instance?.Definition;
            if (skill != null)
            {
                btn.Text        = "";
                btn.Icon        = !string.IsNullOrEmpty(skill.IconPath) ? GD.Load<Texture2D>(skill.IconPath) : null;
                btn.ExpandIcon  = btn.Icon != null;
                btn.TooltipText = BuildSkillTooltip(skill, instance!);
                btn.Modulate    = Colors.White;
                ApplyTierStyle(btn, instance!.Tier);
                return;
            }
        }
        btn.Icon        = null;
        btn.ExpandIcon  = false;
        btn.Text        = "—";
        btn.TooltipText = $"Skill {slotIndex + 1}: Empty";
        btn.Modulate    = new Color(1f, 1f, 1f, 0.4f);
        ClearTierStyle(btn);
    }

    // ── Inventory grids ───────────────────────────────────────────────────────

    private void RefreshInventory()
    {
        var profile = _manager.Profile;
        int sa = profile.OwnedSkillAugmentInstances.Count;
        int ea = profile.OwnedEquipmentAugmentInstances.Count;
        _inventoryInfo.Text = $"Gear: {profile.OwnedGearInstances.Count}/50  Skills: {profile.OwnedSkillInstances.Count}/50  S.Augs: {sa}/50  E.Augs: {ea}/50  Coins: {profile.CoinBank}  Common: {profile.GetMaterial("crafting_common")}";

        foreach (Node child in _inventoryGrid.GetChildren())
            child.QueueFree();

        for (int i = 0; i < Character.ProfileData.MaxInventory; i++)
        {
            var btn = new TooltipButton { CustomMinimumSize = new Vector2(72, 72) };

            if (i < profile.OwnedGearInstances.Count)
            {
                var instance = profile.OwnedGearInstances[i];
                var item     = instance.Definition;
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
                    btn.TooltipText = BuildGearTooltip(item, instance.Tier);
                    ApplyTierStyle(btn, instance.Tier);
                    var capturedInst = instance;
                    var capturedItem = item;
                    var capturedBtn  = btn;
                    btn.GuiInput += (e) => OnInventoryGearInput(e, capturedInst, capturedItem, capturedBtn);
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

    private void RefreshSkillsInventory()
    {
        foreach (Node child in _skillsGrid.GetChildren())
            child.QueueFree();

        var ownedSkills = _manager.Profile.OwnedSkillInstances;

        for (int i = 0; i < Character.ProfileData.MaxInventory; i++)
        {
            var btn = new TooltipButton { CustomMinimumSize = new Vector2(72, 72) };

            if (i < ownedSkills.Count)
            {
                var instance = ownedSkills[i];
                var skill    = instance.Definition;
                if (skill != null)
                {
                    if (!string.IsNullOrEmpty(skill.IconPath))
                    {
                        btn.Icon       = GD.Load<Texture2D>(skill.IconPath);
                        btn.ExpandIcon = true;
                    }
                    else
                    {
                        btn.Text = skill.Name;
                        btn.AddThemeFontSizeOverride("font_size", 10);
                    }
                    btn.TooltipText = BuildSkillTooltip(skill, instance);
                    ApplyTierStyle(btn, instance.Tier);
                    var capturedInst = instance;
                    var capturedBtn  = btn;
                    btn.GuiInput += (e) => OnInventorySkillInput(e, capturedInst, capturedBtn);
                }
            }
            else
            {
                btn.Modulate = new Color(1f, 1f, 1f, 0.3f);
                btn.Disabled = true;
            }

            _skillsGrid.AddChild(btn);
        }
    }

    private void RefreshAugmentsInventory()
    {
        foreach (Node child in _augmentsGrid.GetChildren())
            child.QueueFree();

        var skillAugs = _manager.Profile.OwnedSkillAugmentInstances;
        var equipAugs = _manager.Profile.OwnedEquipmentAugmentInstances;
        int total     = System.Math.Max(skillAugs.Count + equipAugs.Count, Character.ProfileData.MaxInventory);

        for (int i = 0; i < total; i++)
        {
            var btn = new TooltipButton { CustomMinimumSize = new Vector2(72, 72) };

            if (i < skillAugs.Count)
            {
                var inst = skillAugs[i];
                var def  = inst.Definition;
                if (def != null)
                {
                    btn.Text        = def.Name;
                    btn.TooltipText = $"{def.Name}\nRequires: {string.Join(", ", def.RequiredTags)}";
                    btn.AddThemeFontSizeOverride("font_size", 10);
                    var capturedInst = inst;
                    var capturedBtn  = btn;
                    btn.Pressed += () => ShowSupportInventoryPopup(capturedInst, capturedBtn);
                }
            }
            else if (i < skillAugs.Count + equipAugs.Count)
            {
                var inst = equipAugs[i - skillAugs.Count];
                var def  = inst.Definition;
                if (def != null)
                {
                    btn.Text        = def.Name;
                    btn.TooltipText = $"[Equip] {def.Name}\nRequires: {string.Join(", ", def.RequiredTags)}";
                    btn.AddThemeFontSizeOverride("font_size", 10);
                    var capturedInst = inst;
                    var capturedBtn  = btn;
                    btn.Pressed += () => ShowEquipmentAugmentInventoryPopup(capturedInst, capturedBtn);
                }
            }
            else
            {
                btn.Modulate = new Color(1f, 1f, 1f, 0.3f);
                btn.Disabled = true;
            }

            _augmentsGrid.AddChild(btn);
        }
    }

    // ── Slot interactions ─────────────────────────────────────────────────────

    private void OnGearSlotInput(InputEvent e, ItemSlot slot, Button btn)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;
        var c = _manager.SelectedCharacter;
        if (c == null) return;

        bool occupied = c.EquippedGear.ContainsKey(slot.ToString());
        if (mb.ButtonIndex == MouseButton.Right)
        {
            if (occupied)
            {
                bool inventoryFull = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;
                if (!inventoryFull) _manager.UnequipItem(c.Id, slot);
                Refresh();
            }
        }
        else if (mb.ButtonIndex == MouseButton.Left)
        {
            if (occupied)
                ShowEquippedItemPopup(slot, btn);
            else
                ShowEmptyGearSlotMenu(slot, btn);
        }
    }

    private void OnSkillSlotInput(InputEvent e, int slotIndex)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;
        var c = _manager.SelectedCharacter;
        if (c == null) return;

        string? instanceId = slotIndex < c.SlottedSkillInstanceIds.Count ? c.SlottedSkillInstanceIds[slotIndex] : null;
        bool occupied = !string.IsNullOrEmpty(instanceId);

        if (mb.ButtonIndex == MouseButton.Right)
        {
            if (occupied) { _manager.UnequipSkillSlot(c.Id, slotIndex); Refresh(); }
        }
        else if (mb.ButtonIndex == MouseButton.Left)
        {
            if (occupied)
                ShowEquippedSkillPopup(slotIndex, instanceId!, _skillBtns[slotIndex]);
            else
                ShowEmptySkillSlotMenu(slotIndex, _skillBtns[slotIndex]);
        }
    }

    private void OnInventoryGearInput(InputEvent e, GearItemInstance instance, ItemData item, Button btn)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;
        var c = _manager.SelectedCharacter;

        if (mb.ButtonIndex == MouseButton.Right && c != null)
        {
            _manager.EquipItem(c.Id, item.Slot, instance.Id);
            Refresh();
        }
        else if (mb.ButtonIndex == MouseButton.Left)
        {
            ShowInventoryItemPopup(instance, item, btn);
        }
    }

    private void OnInventorySkillInput(InputEvent e, SkillItemInstance instance, Button btn)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;
        var c = _manager.SelectedCharacter;

        if (mb.ButtonIndex == MouseButton.Right && c != null)
        {
            int emptySlot = c.SlottedSkillInstanceIds.IndexOf("");
            if (emptySlot < 0) emptySlot = 0;
            _manager.EquipSkill(c.Id, emptySlot, instance.Id);
            Refresh();
        }
        else if (mb.ButtonIndex == MouseButton.Left)
        {
            ShowSkillInventoryPopup(instance, btn);
        }
    }

    // ── Popups ────────────────────────────────────────────────────────────────

    private void ShowInventoryItemPopup(GearItemInstance instance, ItemData item, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Equip",  0);
        popup.AddItem("Modify", 1);
        popup.AddItem("Delete", 2);
        popup.IdPressed += (long id) =>
        {
            var c = _manager.SelectedCharacter;
            if (id == 0 && c != null) { _manager.EquipItem(c.Id, item.Slot, instance.Id); Refresh(); }
            else if (id == 1) ShowGearModifyPanel(instance);
            else if (id == 2) { _manager.DeleteGearItem(instance.Id); Refresh(); }
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowSkillInventoryPopup(SkillItemInstance instance, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Equip",  0);
        popup.AddItem("Modify", 1);
        popup.AddItem("Delete", 2);
        popup.IdPressed += (long id) =>
        {
            var c = _manager.SelectedCharacter;
            if (id == 0 && c != null)
            {
                int emptySlot = c.SlottedSkillInstanceIds.IndexOf("");
                if (emptySlot < 0) emptySlot = 0;
                _manager.EquipSkill(c.Id, emptySlot, instance.Id);
                Refresh();
            }
            else if (id == 1) ShowSkillModifyPanel(instance);
            else if (id == 2) { _manager.DeleteSkillItem(instance.Id); Refresh(); }
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowSupportInventoryPopup(SkillAugmentInstance instance, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Delete", 0);
        popup.IdPressed += (long id) =>
        {
            if (id == 0)
                _manager.Profile.OwnedSkillAugmentInstances.Remove(instance);
            Refresh();
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowEquipmentAugmentInventoryPopup(EquipmentAugmentInstance instance, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Delete", 0);
        popup.IdPressed += (long id) =>
        {
            if (id == 0)
                _manager.Profile.OwnedEquipmentAugmentInstances.Remove(instance);
            Refresh();
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowEquippedItemPopup(ItemSlot slot, Button anchor)
    {
        var c = _manager.SelectedCharacter!;
        if (!c.EquippedGear.TryGetValue(slot.ToString(), out var instance)) return;

        var popup = NewStyledPopup();
        popup.AddItem("Unequip", 0);
        popup.AddItem("Modify",  1);
        popup.AddItem("Delete",  2);
        bool inventoryFull = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;
        popup.SetItemDisabled(0, inventoryFull);
        popup.IdPressed += (long id) =>
        {
            if (id == 0) { _manager.UnequipItem(c.Id, slot); Refresh(); }
            else if (id == 1) ShowGearModifyPanel(instance);
            else if (id == 2) { _manager.DeleteGearItem(instance.Id); Refresh(); }
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowEquippedSkillPopup(int slotIndex, string instanceId, Button anchor)
    {
        var c        = _manager.SelectedCharacter!;
        var instance = _manager.FindSkillInstance(instanceId);
        var popup    = NewStyledPopup();
        popup.AddItem("Unequip", 0);
        popup.AddItem("Modify",  1);
        popup.AddItem("Delete",  2);
        popup.AddCheckItem("Auto-activate", 3);
        bool autoOn = slotIndex < c.SlotAutoActivate.Count ? c.SlotAutoActivate[slotIndex] : true;
        popup.SetItemChecked(3, autoOn);
        popup.IdPressed += (long id) =>
        {
            if (id == 0) { _manager.UnequipSkillSlot(c.Id, slotIndex); Refresh(); }
            else if (id == 1 && instance != null) ShowSkillModifyPanel(instance);
            else if (id == 2) { _manager.DeleteSkillPermanently(c.Id, slotIndex); Refresh(); }
            else if (id == 3) { _manager.SetSlotAutoActivate(c.Id, slotIndex, !popup.IsItemChecked(3)); }
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowEmptyGearSlotMenu(ItemSlot slot, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Craft New",            0);
        popup.AddItem("Equip from Inventory", 1);
        popup.IdPressed += (long id) =>
        {
            if      (id == 0) ShowCraftGearForSlotPanel(slot);
            else if (id == 1) OpenPicker(slot);
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowEmptySkillSlotMenu(int slotIndex, Button anchor)
    {
        var popup = NewStyledPopup();
        popup.AddItem("Craft New",            0);
        popup.AddItem("Equip from Inventory", 1);
        popup.IdPressed += (long id) =>
        {
            if      (id == 0) ShowCraftSkillPopup();
            else if (id == 1) OpenSkillPicker(slotIndex);
        };
        ShowPopupAt(popup, anchor);
    }

    private void ShowCraftGearForSlotPanel(ItemSlot slot)
    {
        var overlay = MakeModalOverlay();
        var panel   = MakeModifyPanel();
        var vbox    = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);
        overlay.AddChild(panel);
        AddChild(overlay);

        vbox.AddChild(MakeModifyHeader($"Craft {slot}", () => overlay.QueueFree()));
        vbox.AddChild(new HSeparator());

        int  common  = _manager.Profile.GetMaterial("crafting_common");
        bool invFull = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;

        var statusLbl = new Label { Text = invFull ? "Inventory full" : $"Common material: {common}" };
        statusLbl.AddThemeColorOverride("font_color", new Color("#8AA0AE"));
        vbox.AddChild(statusLbl);

        bool any = false;
        foreach (var recipe in RecipeRegistry.ForType(RecipeType.Gear))
        {
            var itemDef = ItemRegistry.Get(recipe.OutputItemId);
            if (itemDef == null || itemDef.Slot != slot) continue;
            any = true;
            int  cost     = recipe.MaterialCosts.TryGetValue("crafting_common", out var mc) ? mc : 1;
            bool canCraft = !invFull && common >= cost;
            var  btn      = MakeModifyButton($"{itemDef.Name}  —  {cost} Common", !canCraft);
            string rid    = recipe.Id;
            btn.Pressed  += () => { _manager.CraftGearItem(rid); overlay.QueueFree(); Refresh(); };
            vbox.AddChild(btn);
        }

        if (!any)
        {
            var lbl = new Label { Text = "No recipes available for this slot" };
            lbl.AddThemeColorOverride("font_color", new Color("#4A5560"));
            vbox.AddChild(lbl);
        }
    }

    private void ShowCraftEquipmentPopup()
    {
        var overlay = MakeModalOverlay();
        var panel   = MakeModifyPanel();
        var vbox    = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);
        overlay.AddChild(panel);
        AddChild(overlay);

        vbox.AddChild(MakeModifyHeader("Craft Equipment", () => overlay.QueueFree()));
        vbox.AddChild(new HSeparator());

        int common    = _manager.Profile.GetMaterial("crafting_common");
        bool invFull  = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;

        var statusLbl = new Label { Text = invFull ? "Inventory full" : $"Common material: {common}" };
        statusLbl.AddThemeColorOverride("font_color", new Color("#8AA0AE"));
        vbox.AddChild(statusLbl);

        foreach (var recipe in RecipeRegistry.ForType(RecipeType.Gear))
        {
            var itemDef  = ItemRegistry.Get(recipe.OutputItemId);
            if (itemDef == null) continue;
            int cost     = (recipe.MaterialCosts.TryGetValue("crafting_common", out var matCost) ? matCost : 1);
            bool canCraft = !invFull && common >= cost;
            string label  = $"{itemDef.Name}  [{itemDef.Slot}]  —  {cost} Common";
            var btn       = MakeModifyButton(label, !canCraft);
            string recipeId = recipe.Id;
            btn.Pressed += () =>
            {
                _manager.CraftGearItem(recipeId);
                overlay.QueueFree();
                Refresh();
            };
            vbox.AddChild(btn);
        }
    }

    private void ShowCraftSkillPopup()
    {
        var overlay = MakeModalOverlay();
        var panel   = MakeModifyPanel();
        var vbox    = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);
        overlay.AddChild(panel);
        AddChild(overlay);

        vbox.AddChild(MakeModifyHeader("Craft Skill", () => overlay.QueueFree()));
        vbox.AddChild(new HSeparator());

        int common   = _manager.Profile.GetMaterial("crafting_common");
        bool invFull = _manager.Profile.OwnedSkillInstances.Count >= Character.ProfileData.MaxInventory;

        var statusLbl = new Label { Text = invFull ? "Inventory full" : $"Common material: {common}" };
        statusLbl.AddThemeColorOverride("font_color", new Color("#8AA0AE"));
        vbox.AddChild(statusLbl);

        foreach (var recipe in RecipeRegistry.ForType(RecipeType.Skill))
        {
            var skillDef = Skills.SkillRegistry.Get(recipe.OutputItemId);
            if (skillDef == null) continue;
            int cost     = (recipe.MaterialCosts.TryGetValue("crafting_common", out var matCost) ? matCost : 1);
            bool canCraft = !invFull && common >= cost;
            string label  = $"{skillDef.Name}  —  {cost} Common";
            var btn       = MakeModifyButton(label, !canCraft);
            string recipeId = recipe.Id;
            btn.Pressed += () =>
            {
                _manager.CraftSkillItem(recipeId);
                overlay.QueueFree();
                Refresh();
            };
            vbox.AddChild(btn);
        }
    }

    private void ShowPopupAt(PopupMenu popup, Control anchor)
    {
        AddChild(popup);
        popup.PopupHide += popup.QueueFree;
        popup.ResetSize();
        var rect = anchor.GetGlobalRect();
        popup.Position = new Vector2I((int)rect.Position.X, (int)(rect.Position.Y + rect.Size.Y));
        popup.Popup();
    }

    private void OpenPicker(ItemSlot slot)
    {
        var pickerScene = GD.Load<PackedScene>("res://src/ui/item_picker_panel.tscn");
        var picker      = pickerScene.Instantiate<ItemPickerPanel>();
        picker.Init(_manager, _manager.SelectedCharacter!, slot, () => Refresh());
        AddChild(picker);
    }

    private void OpenSkillPicker(int slotIndex)
    {
        var pickerScene = GD.Load<PackedScene>("res://src/ui/skill_picker_panel.tscn");
        var picker      = pickerScene.Instantiate<SkillPickerPanel>();
        picker.Init(_manager, _manager.SelectedCharacter!, slotIndex, () => Refresh());
        AddChild(picker);
    }

    // ── Modify panel ─────────────────────────────────────────────────────────

    private void ShowGearModifyPanel(GearItemInstance instance)
    {
        var overlay = MakeModalOverlay();
        var panel   = MakeModifyPanel();
        var vbox    = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);

        var def = instance.Definition;
        panel.AddChild(vbox);
        overlay.AddChild(panel);
        AddChild(overlay);

        // Title
        var header = MakeModifyHeader($"{def?.Name ?? "Item"}  [{ItemTier.Label(instance.Tier)}]", () => overlay.QueueFree());
        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        // Upgrade
        bool atMax     = instance.Tier >= ItemTier.Max;
        bool canAfford = _manager.Profile.GetMaterial("crafting_common") >= 1;
        string next    = instance.Tier switch { 1 => "Uncommon", 2 => "Rare", _ => "" };
        var upgradeBtn = MakeModifyButton(atMax ? "Max Tier" : $"Upgrade to {next}  [1 Common]", atMax || !canAfford);
        upgradeBtn.Pressed += () => { _manager.UpgradeGearItem(instance.Id); overlay.QueueFree(); Refresh(); };
        vbox.AddChild(upgradeBtn);

        // Augment slots
        if (instance.MaxEquipmentAugSlots > 0)
        {
            vbox.AddChild(new HSeparator());
            vbox.AddChild(MakeModifySubLabel("Equipment Augments"));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            vbox.AddChild(row);
            BuildGearAugSlots(row, instance);
        }
    }

    private void ShowSkillModifyPanel(SkillItemInstance instance)
    {
        var overlay = MakeModalOverlay();
        var panel   = MakeModifyPanel();
        var vbox    = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);

        var def = instance.Definition;
        panel.AddChild(vbox);
        overlay.AddChild(panel);
        AddChild(overlay);

        // Title
        var header = MakeModifyHeader($"{def?.Name ?? "Skill"}  [{ItemTier.Label(instance.Tier)}]", () => overlay.QueueFree());
        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        // Upgrade
        bool atMax     = instance.Tier >= ItemTier.Max;
        bool canAfford = _manager.Profile.GetMaterial("crafting_common") >= 1;
        string next    = instance.Tier switch { 1 => "Uncommon", 2 => "Rare", _ => "" };
        var upgradeBtn = MakeModifyButton(atMax ? "Max Tier" : $"Upgrade to {next}  [1 Common]", atMax || !canAfford);
        upgradeBtn.Pressed += () => { _manager.UpgradeSkillItem(instance.Id); overlay.QueueFree(); Refresh(); };
        vbox.AddChild(upgradeBtn);

        // Augment slots
        if (instance.MaxSkillAugmentSlots > 0)
        {
            vbox.AddChild(new HSeparator());
            vbox.AddChild(MakeModifySubLabel("Skill Augments"));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            vbox.AddChild(row);
            BuildSkillAugSlots(row, instance);
        }
    }

    private void BuildGearAugSlots(HBoxContainer row, GearItemInstance instance)
    {
        foreach (Node child in row.GetChildren()) child.QueueFree();

        for (int i = 0; i < instance.MaxEquipmentAugSlots; i++)
        {
            int    captured   = i;
            string socketedId = i < instance.SocketedEquipmentAugmentIds.Count
                ? instance.SocketedEquipmentAugmentIds[i] : "";
            var    augInst    = _manager.FindEquipmentAugmentInstance(socketedId);
            var    btn        = new TooltipButton { CustomMinimumSize = new Vector2(60, 60) };

            if (augInst?.Definition != null)
            {
                btn.Text        = augInst.Definition.Name;
                btn.TooltipText = augInst.Definition.Name;
                btn.AddThemeFontSizeOverride("font_size", 9);
                btn.Pressed += () => { _manager.RemoveEquipmentAugment(instance.Id, captured); BuildGearAugSlots(row, instance); };
            }
            else
            {
                btn.Text        = $"E{i + 1}";
                btn.TooltipText = "Empty — click to socket";
                btn.Modulate    = new Color(1f, 1f, 1f, 0.5f);
                btn.Pressed     += () => OpenGearAugmentPicker(row, instance, captured);
            }

            row.AddChild(btn);
        }
    }

    private void BuildSkillAugSlots(HBoxContainer row, SkillItemInstance instance)
    {
        foreach (Node child in row.GetChildren()) child.QueueFree();

        for (int i = 0; i < instance.MaxSkillAugmentSlots; i++)
        {
            int    captured   = i;
            string socketedId = i < instance.SocketedSkillAugmentIds.Count
                ? instance.SocketedSkillAugmentIds[i] : "";
            var    augment    = _manager.FindSkillAugmentInstance(socketedId);
            var    btn        = new TooltipButton { CustomMinimumSize = new Vector2(60, 60) };

            if (augment?.Definition != null)
            {
                btn.Text        = augment.Definition.Name;
                btn.TooltipText = augment.Definition.Name;
                btn.AddThemeFontSizeOverride("font_size", 9);
                btn.Pressed += () => { _manager.RemoveSkillAugment(instance.Id, captured); BuildSkillAugSlots(row, instance); };
            }
            else
            {
                btn.Text        = $"S{i + 1}";
                btn.TooltipText = "Empty — click to socket";
                btn.Modulate    = new Color(1f, 1f, 1f, 0.5f);
                btn.Pressed     += () => OpenSkillAugmentPicker(row, instance, captured);
            }

            row.AddChild(btn);
        }
    }

    private void OpenGearAugmentPicker(HBoxContainer row, GearItemInstance gear, int slotIndex)
    {
        var def = gear.Definition;
        if (def == null) return;

        var compatible = _manager.Profile.OwnedEquipmentAugmentInstances
            .Where(a => a.Definition != null &&
                        (a.Definition.RequiredTags.Length == 0 ||
                         a.Definition.RequiredTags.Any(t => def.Tags.Contains(t))))
            .ToList();

        var popup = NewStyledPopup();
        if (compatible.Count == 0)
        {
            popup.AddItem("No compatible augments owned", 0);
            popup.SetItemDisabled(0, true);
        }
        else
        {
            for (int i = 0; i < compatible.Count; i++)
                popup.AddItem(compatible[i].Definition!.Name, i);
            popup.IdPressed += (long id) => { _manager.SocketEquipmentAugment(gear.Id, slotIndex, compatible[(int)id].Id); BuildGearAugSlots(row, gear); };
        }

        Control anchor = slotIndex < row.GetChildCount() && row.GetChild(slotIndex) is Control c ? c : row;
        ShowPopupAt(popup, anchor);
    }

    private void OpenSkillAugmentPicker(HBoxContainer row, SkillItemInstance skill, int slotIndex)
    {
        var compatible = _manager.Profile.OwnedSkillAugmentInstances
            .Where(s => s.Definition != null)
            .ToList();

        var popup = NewStyledPopup();
        if (compatible.Count == 0)
        {
            popup.AddItem("No skill augments owned", 0);
            popup.SetItemDisabled(0, true);
        }
        else
        {
            for (int i = 0; i < compatible.Count; i++)
                popup.AddItem(compatible[i].Definition!.Name, i);
            popup.IdPressed += (long id) => { _manager.SocketSkillAugment(skill.Id, slotIndex, compatible[(int)id].Id); BuildSkillAugSlots(row, skill); };
        }

        Control anchor = slotIndex < row.GetChildCount() && row.GetChild(slotIndex) is Control c ? c : row;
        ShowPopupAt(popup, anchor);
    }

    private static ColorRect MakeModalOverlay()
    {
        return new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.6f),
            AnchorLeft   = 0f, AnchorRight  = 1f,
            AnchorTop    = 0f, AnchorBottom = 1f,
            MouseFilter  = Control.MouseFilterEnum.Stop,
        };
    }

    private static PanelContainer MakeModifyPanel()
    {
        var panel = new PanelContainer
        {
            AnchorLeft        = 0.5f, AnchorRight  = 0.5f,
            AnchorTop         = 0.5f, AnchorBottom = 0.5f,
            GrowHorizontal    = Control.GrowDirection.Both,
            GrowVertical      = Control.GrowDirection.Both,
            CustomMinimumSize = new Vector2(380f, 0f),
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor                 = new Color("#1E252B"),
            BorderColor             = new Color("#3A4650"),
            BorderWidthTop          = 1, BorderWidthBottom      = 1,
            BorderWidthLeft         = 1, BorderWidthRight       = 1,
            CornerRadiusTopLeft     = 4, CornerRadiusTopRight   = 4,
            CornerRadiusBottomLeft  = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft       = 16f, ContentMarginRight   = 16f,
            ContentMarginTop        = 16f, ContentMarginBottom  = 16f,
        });
        return panel;
    }

    private static HBoxContainer MakeModifyHeader(string title, System.Action onClose)
    {
        var font   = GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_2.ttf");
        var header = new HBoxContainer();

        var lbl = new Label
        {
            Text                = title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        lbl.AddThemeFontOverride("font", font);
        lbl.AddThemeFontSizeOverride("font_size", 18);
        lbl.AddThemeColorOverride("font_color", new Color("#C8D4DC"));

        var closeBtn = new Button { Text = "✕" };
        closeBtn.AddThemeFontOverride("font", font);
        closeBtn.AddThemeFontSizeOverride("font_size", 16);
        closeBtn.Pressed += () => onClose();

        header.AddChild(lbl);
        header.AddChild(closeBtn);
        return header;
    }

    private static Label MakeModifySubLabel(string text)
    {
        var font = GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_2.ttf");
        var lbl  = new Label { Text = text };
        lbl.AddThemeFontOverride("font", font);
        lbl.AddThemeFontSizeOverride("font_size", 14);
        lbl.AddThemeColorOverride("font_color", new Color("#8AA0AE"));
        return lbl;
    }

    private static Button MakeModifyButton(string text, bool disabled)
    {
        var font = GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_2.ttf");
        var btn  = new Button { Text = text, Disabled = disabled, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        btn.AddThemeFontOverride("font", font);
        btn.AddThemeFontSizeOverride("font_size", 16);
        return btn;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyTierStyle(Button btn, int tier)
    {
        var border = ItemTier.BorderColor(tier);

        // Button: transparent shell; ContentMargins keep icon inside the border+gap+inner area
        // (5 border + 3 gap + 4 inner = 12px each side)
        var shell = new StyleBoxFlat
        {
            DrawCenter          = false,
            ContentMarginLeft   = 12f,
            ContentMarginRight  = 12f,
            ContentMarginTop    = 12f,
            ContentMarginBottom = 12f,
        };
        btn.AddThemeStyleboxOverride("normal",  shell);
        btn.AddThemeStyleboxOverride("hover",   shell);
        btn.AddThemeStyleboxOverride("pressed", shell);

        // Border ring at outer edge (DrawCenter=false so fill is transparent — shows dark gap)
        btn.GetNodeOrNull("_tier_border")?.QueueFree();
        var borderPanel = new Panel
        {
            Name             = "_tier_border",
            ShowBehindParent = true,
            AnchorLeft       = 0f,
            AnchorRight      = 1f,
            AnchorTop        = 0f,
            AnchorBottom     = 1f,
            MouseFilter      = Control.MouseFilterEnum.Ignore,
        };
        borderPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            DrawCenter              = false,
            BorderColor             = border,
            BorderWidthTop          = 5,
            BorderWidthBottom       = 5,
            BorderWidthLeft         = 5,
            BorderWidthRight        = 5,
            CornerRadiusTopLeft     = 3,
            CornerRadiusTopRight    = 3,
            CornerRadiusBottomLeft  = 3,
            CornerRadiusBottomRight = 3,
        });
        btn.AddChild(borderPanel);

        // Pale slate fill: inset 8px (5 border + 3 gap) so the dark bg shows between border and fill
        btn.GetNodeOrNull("_tier_bg")?.QueueFree();
        var bgPanel = new Panel
        {
            Name             = "_tier_bg",
            ShowBehindParent = true,
            AnchorLeft       = 0f,
            AnchorRight      = 1f,
            AnchorTop        = 0f,
            AnchorBottom     = 1f,
            OffsetLeft       = 8f,
            OffsetRight      = -8f,
            OffsetTop        = 8f,
            OffsetBottom     = -8f,
            MouseFilter      = Control.MouseFilterEnum.Ignore,
        };
        bgPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor                 = new Color("#8AA0AE"),
            CornerRadiusTopLeft     = 2,
            CornerRadiusTopRight    = 2,
            CornerRadiusBottomLeft  = 2,
            CornerRadiusBottomRight = 2,
        });
        btn.AddChild(bgPanel);

        btn.GetNodeOrNull("_tier_lbl")?.QueueFree();
        var lbl = new Label
        {
            Name                = "_tier_lbl",
            Text                = TierRoman(tier),
            AnchorLeft          = 1f,
            AnchorRight         = 1f,
            AnchorTop           = 0f,
            AnchorBottom        = 0f,
            OffsetLeft          = -34f,
            OffsetRight         = -9f,
            OffsetTop           = 9f,
            OffsetBottom        = 27f,
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        lbl.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_2.ttf"));
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", Colors.Black);
        btn.AddChild(lbl);
    }

    private static void ClearTierStyle(Button btn)
    {
        btn.RemoveThemeStyleboxOverride("normal");
        btn.RemoveThemeStyleboxOverride("hover");
        btn.RemoveThemeStyleboxOverride("pressed");
        btn.GetNodeOrNull("_tier_border")?.QueueFree();
        btn.GetNodeOrNull("_tier_bg")?.QueueFree();
        btn.GetNodeOrNull("_tier_lbl")?.QueueFree();
    }

    private static string TierRoman(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => tier.ToString(),
    };

    private static PopupMenu NewStyledPopup()
    {
        var popup = new PopupMenu();
        var font  = GD.Load<Font>("res://assets/fonts/Exo_2/Exo_2_2.ttf");

        popup.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor                 = new Color("#1E252B"),
            BorderColor             = new Color("#3A4650"),
            BorderWidthTop          = 1,
            BorderWidthBottom       = 1,
            BorderWidthLeft         = 1,
            BorderWidthRight        = 1,
            CornerRadiusTopLeft     = 4,
            CornerRadiusTopRight    = 4,
            CornerRadiusBottomLeft  = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft       = 4f,
            ContentMarginRight      = 4f,
            ContentMarginTop        = 4f,
            ContentMarginBottom     = 4f,
        });
        popup.AddThemeStyleboxOverride("hover", new StyleBoxFlat
        {
            BgColor                 = new Color("#2E3840"),
            CornerRadiusTopLeft     = 3,
            CornerRadiusTopRight    = 3,
            CornerRadiusBottomLeft  = 3,
            CornerRadiusBottomRight = 3,
        });
        popup.AddThemeFontOverride("font", font);
        popup.AddThemeFontSizeOverride("font_size", 16);
        popup.AddThemeColorOverride("font_color",          new Color("#C8D4DC"));
        popup.AddThemeColorOverride("font_hover_color",    new Color("#E8F0F4"));
        popup.AddThemeColorOverride("font_disabled_color", new Color("#4A5560"));
        popup.AddThemeConstantOverride("v_separation",     6);
        popup.AddThemeConstantOverride("item_start_padding", 10);
        popup.AddThemeConstantOverride("item_end_padding",   10);
        return popup;
    }

    private static string BuildGearTooltip(ItemData item, int tier)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{item.Name}  [{item.Slot}]  [{ItemTier.Label(tier)}]");
        if (item.BonusHp            != 0)  sb.Append($"\nHP {item.BonusHp:+#;-#;0}");
        if (item.BonusSpeed         != 0f) sb.Append($"\nSpeed {item.BonusSpeed:+#;-#;0}");
        if (item.WeaponRange        != 0f) sb.Append($"\nWeapon Range {item.WeaponRange:0.#} tiles");
        if (item.RangeModifier      != 0f) sb.Append($"\nRange Modifier {item.RangeModifier:+0.#;-0.#} tiles");
        if (item.DamageReduction    != 0f) sb.Append($"\nDamage Reduction {item.DamageReduction:P0}");
        if (item.PhysicalResistance != 0f) sb.Append($"\nPhys. Resist {item.PhysicalResistance:P0}");
        if (item.Tags.Length        > 0)   sb.Append($"\nTags: {string.Join(", ", item.Tags)}");
        return sb.ToString();
    }

    private static string BuildSkillTooltip(SkillData skill, SkillItemInstance instance)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{skill.Name}  [{skill.Type}]  [{ItemTier.Label(instance.Tier)}]");
        sb.Append($"\nTags: {string.Join(", ", skill.Tags)}");
        sb.Append($"\nCD: {skill.Cooldown:F1}s");
        sb.Append($"\nAugments: {instance.SocketedSkillAugmentIds.Count(id => !string.IsNullOrEmpty(id))}/{instance.MaxSkillAugmentSlots}");
        return sb.ToString();
    }
}
