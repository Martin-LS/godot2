using Godot;
using System.Collections.Generic;
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

    private VBoxContainer _craftVBox      = null!;
    private VBoxContainer _skillCraftVBox = null!;

    private Button        _gearModifySlotBtn    = null!;
    private Button        _gearUpgradeBtn       = null!;
    private HBoxContainer _gearEquipAugSlotsRow = null!;
    private Button        _skillModifySlotBtn   = null!;
    private Button        _skillUpgradeBtn      = null!;
    private HBoxContainer _supportSlotsRow      = null!;

    private string? _loadedGearInstanceId  = null;
    private string? _loadedSkillInstanceId = null;

    private Character.CharacterManager _manager = null!;

    private const string CharViewBase      = "VBox/HSplit/RightPanel/TabContainer/Loadout/CharacterView";
    private const string CraftingBase      = "VBox/HSplit/RightPanel/TabContainer/GearCrafting/CraftingTabs";
    private const string SkillCraftingBase = "VBox/HSplit/RightPanel/TabContainer/SkillCrafting/SkillCraftingTabs";

    public override void _Ready()
    {
        _manager = GetNode<Character.CharacterManager>("/root/CharacterManager");

        if (_manager.SelectedCharacter == null)
        {
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");
            return;
        }

        _inventoryInfo = GetNode<Label>("VBox/HSplit/LeftPanel/LeftVBox/InventoryInfo");
        _inventoryGrid = GetNode<GridContainer>("VBox/HSplit/LeftPanel/LeftVBox/InventoryTabs/Equipment/InventoryScroll/InventoryGrid");
        _skillsGrid    = GetNode<GridContainer>("VBox/HSplit/LeftPanel/LeftVBox/InventoryTabs/Skills/SkillsScroll/SkillsGrid");
        _augmentsGrid  = GetNode<GridContainer>("VBox/HSplit/LeftPanel/LeftVBox/InventoryTabs/Augments/AugmentsScroll/AugmentsGrid");

        _nameLabel  = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/NameLabel");
        _typeLabel  = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/TypeLabel");
        _levelLabel = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/LevelLabel");
        _statsLabel = GetNode<Label> ($"{CharViewBase}/HSplit/InfoVBox/StatsLabel");
        _weaponBtn  = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/WeaponSlot/WeaponSlotButton");
        _hatBtn     = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/HatSlot/HatSlotButton");
        _bodyBtn    = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/BodySlot/BodySlotButton");
        _ringBtn    = GetNode<Button>($"{CharViewBase}/HSplit/GearPanel/RingSlot/RingSlotButton");

        _weaponBtn.Pressed += () => OnGearSlotPressed(ItemSlot.Weapon);
        _hatBtn.Pressed    += () => OnGearSlotPressed(ItemSlot.Hat);
        _bodyBtn.Pressed   += () => OnGearSlotPressed(ItemSlot.Body);
        _ringBtn.Pressed   += () => OnGearSlotPressed(ItemSlot.Ring);

        for (int i = 0; i < 3; i++)
        {
            int captured = i;
            _skillBtns[i] = GetNode<Button>($"{CharViewBase}/SkillBar/SkillSlot{i + 1}/SkillSlotButton{i + 1}");
            _skillBtns[i].Pressed += () => OnSkillSlotPressed(captured);
        }

        GetNode<Button>($"{CharViewBase}/Buttons/StartRunButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://main.tscn");

        _craftVBox      = GetNode<VBoxContainer>($"{CraftingBase}/Create/CraftScroll/VBox");
        _skillCraftVBox = GetNode<VBoxContainer>($"{SkillCraftingBase}/Create/SkillCraftScroll/VBox");

        _gearModifySlotBtn  = GetNode<Button>       ($"{CraftingBase}/Modify/ModifyVBox/GearModifySlotBtn");
        _gearUpgradeBtn     = GetNode<Button>       ($"{CraftingBase}/Modify/ModifyVBox/GearUpgradeBtn");
        _skillModifySlotBtn = GetNode<Button>       ($"{SkillCraftingBase}/Modify/ModifyVBox/SkillModifySlotBtn");
        _skillUpgradeBtn    = GetNode<Button>       ($"{SkillCraftingBase}/Modify/ModifyVBox/SkillUpgradeBtn");
        _supportSlotsRow    = GetNode<HBoxContainer>($"{SkillCraftingBase}/Modify/ModifyVBox/SupportSlotsRow");

        // Equipment augment slots row — added dynamically below the upgrade button
        var gearModifyVBox = GetNode<VBoxContainer>($"{CraftingBase}/Modify/ModifyVBox");
        gearModifyVBox.AddChild(new Label { Text = "Equipment Augments:" });
        _gearEquipAugSlotsRow = new HBoxContainer();
        gearModifyVBox.AddChild(_gearEquipAugSlotsRow);

        _gearModifySlotBtn.Pressed  += OnGearModifySlotPressed;
        _gearUpgradeBtn.Pressed     += OnGearUpgradePressed;
        _skillModifySlotBtn.Pressed += OnSkillModifySlotPressed;
        _skillUpgradeBtn.Pressed    += OnSkillUpgradePressed;

        GetNode<Button>("VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://src/ui/account_screen.tscn");

        var tabs = GetNode<TabContainer>("VBox/HSplit/RightPanel/TabContainer");
        tabs.SetTabTitle(1, "Equipment Crafting");
        tabs.SetTabTitle(2, "Skill Crafting");

        Refresh();
    }

    private void Refresh()
    {
        RefreshInventory();
        RefreshSkillsInventory();
        RefreshAugmentsInventory();
        RefreshCharacter();
        RefreshCrafting();
        RefreshSkillCrafting();
        RefreshGearModify();
        RefreshSkillModify();
    }

    // ── Character panel ───────────────────────────────────────────────────────

    private void RefreshCharacter()
    {
        var c     = _manager.SelectedCharacter!;
        var stats = c.BuildStatBlock();

        _nameLabel.Text  = c.Name;
        _typeLabel.Text  = c.Type.ToString();
        _levelLabel.Text = $"Level {c.CurrentLevel}   XP: {c.CurrentXp}";
        _statsLabel.Text = $"HP {(int)stats.Get(StatId.MaxHp)}   Speed {stats.Get(StatId.Speed):F0}   P.Dmg {stats.Get(StatId.PhysicalDamage):F0}   M.Dmg {stats.Get(StatId.MagicDamage):F0}\nRuns: {c.RunsCompleted}";

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
            var btn = new Button { CustomMinimumSize = new Vector2(60, 60) };

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
                    btn.Pressed += () => ShowInventoryItemPopup(capturedInst, capturedItem, capturedBtn);
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
            var btn = new Button { CustomMinimumSize = new Vector2(60, 60) };

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
                    btn.Pressed += () => ShowSkillInventoryPopup(capturedInst, capturedBtn);
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
            var btn = new Button { CustomMinimumSize = new Vector2(60, 60) };

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

    // ── Crafting tabs ─────────────────────────────────────────────────────────

    private void RefreshCrafting()
    {
        foreach (Node child in _craftVBox.GetChildren())
            child.QueueFree();

        var matsLabel = new Label { Text = $"Common materials: {_manager.Profile.GetMaterial("crafting_common")}" };
        _craftVBox.AddChild(matsLabel);

        bool gearFull    = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;
        bool equipAugFull = _manager.Profile.OwnedEquipmentAugmentInstances.Count >= Character.ProfileData.MaxInventory;

        _craftVBox.AddChild(new Label { Text = "— Gear —" });
        foreach (var recipe in RecipeRegistry.ForType(RecipeType.Gear))
        {
            var item = ItemRegistry.Get(recipe.OutputItemId);
            if (item == null) continue;

            string costText = string.Join(", ", recipe.MaterialCosts.Select(kv =>
                $"{kv.Value}× {(kv.Key == "crafting_common" ? "Common" : kv.Key)}"));
            bool canAfford = recipe.MaterialCosts.All(kv => _manager.Profile.GetMaterial(kv.Key) >= kv.Value);

            var btn = new Button
            {
                Text                  = $"{item.Name}  —  {costText}",
                Disabled              = !canAfford || gearFull,
                SizeFlagsHorizontal   = SizeFlags.ShrinkBegin,
                CustomMinimumSize     = new Vector2(0, 32),
            };
            string capturedId = recipe.Id;
            btn.Pressed += () => { _manager.CraftGearItem(capturedId); Refresh(); };
            _craftVBox.AddChild(btn);
        }

        _craftVBox.AddChild(new Label { Text = "— Equipment Augments —" });
        foreach (var recipe in RecipeRegistry.ForType(RecipeType.EquipmentAugment))
        {
            var augment = EquipmentAugmentRegistry.Get(recipe.OutputItemId);
            if (augment == null) continue;

            string costText = string.Join(", ", recipe.MaterialCosts.Select(kv =>
                $"{kv.Value}× {(kv.Key == "crafting_common" ? "Common" : kv.Key)}"));
            bool canAfford = recipe.MaterialCosts.All(kv => _manager.Profile.GetMaterial(kv.Key) >= kv.Value);

            var btn = new Button
            {
                Text                = $"{augment.Name}  [{string.Join(", ", augment.RequiredTags)}]  —  {costText}",
                Disabled            = !canAfford || equipAugFull,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize   = new Vector2(0, 32),
            };
            string capturedId = recipe.Id;
            btn.Pressed += () => { _manager.CraftEquipmentAugmentItem(capturedId); Refresh(); };
            _craftVBox.AddChild(btn);
        }
    }

    private void RefreshSkillCrafting()
    {
        foreach (Node child in _skillCraftVBox.GetChildren())
            child.QueueFree();

        var matsLabel = new Label { Text = $"Common materials: {_manager.Profile.GetMaterial("crafting_common")}" };
        _skillCraftVBox.AddChild(matsLabel);

        bool skillFull   = _manager.Profile.OwnedSkillInstances.Count   >= Character.ProfileData.MaxInventory;
        bool supportFull = _manager.Profile.OwnedSkillAugmentInstances.Count >= Character.ProfileData.MaxInventory;

        _skillCraftVBox.AddChild(new Label { Text = "— Skills —" });
        foreach (var recipe in RecipeRegistry.ForType(RecipeType.Skill))
        {
            var skill = SkillRegistry.Get(recipe.OutputItemId);
            if (skill == null) continue;

            string costText = string.Join(", ", recipe.MaterialCosts.Select(kv =>
                $"{kv.Value}× {(kv.Key == "crafting_common" ? "Common" : kv.Key)}"));
            bool canAfford = recipe.MaterialCosts.All(kv => _manager.Profile.GetMaterial(kv.Key) >= kv.Value);

            var btn = new Button
            {
                Text                = $"{skill.Name}  —  {costText}",
                Disabled            = !canAfford || skillFull,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize   = new Vector2(0, 32),
            };
            string capturedId = recipe.Id;
            btn.Pressed += () => { _manager.CraftSkillItem(capturedId); Refresh(); };
            _skillCraftVBox.AddChild(btn);
        }

        _skillCraftVBox.AddChild(new Label { Text = "— Skill Augments —" });
        foreach (var recipe in RecipeRegistry.ForType(RecipeType.SkillAugment))
        {
            var augment = SkillAugmentRegistry.Get(recipe.OutputItemId);
            if (augment == null) continue;

            string costText = string.Join(", ", recipe.MaterialCosts.Select(kv =>
                $"{kv.Value}× {(kv.Key == "crafting_common" ? "Common" : kv.Key)}"));
            bool canAfford = recipe.MaterialCosts.All(kv => _manager.Profile.GetMaterial(kv.Key) >= kv.Value);

            var btn = new Button
            {
                Text                = $"{augment.Name}  [{string.Join(", ", augment.RequiredTags)}]  —  {costText}",
                Disabled            = !canAfford || supportFull,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize   = new Vector2(0, 32),
            };
            string capturedId = recipe.Id;
            btn.Pressed += () => { _manager.CraftSkillAugmentItem(capturedId); Refresh(); };
            _skillCraftVBox.AddChild(btn);
        }
    }

    // ── Modify tabs ───────────────────────────────────────────────────────────

    private void RefreshGearModify()
    {
        foreach (Node child in _gearEquipAugSlotsRow.GetChildren())
            child.QueueFree();

        var instance = _manager.FindGearInstance(_loadedGearInstanceId);
        if (instance == null)
        {
            _loadedGearInstanceId         = null;
            _gearModifySlotBtn.Text       = "Select";
            _gearModifySlotBtn.Icon       = null;
            _gearModifySlotBtn.ExpandIcon = false;
            ClearTierStyle(_gearModifySlotBtn);
            _gearUpgradeBtn.Text     = "Upgrade";
            _gearUpgradeBtn.Disabled = true;
            return;
        }

        var def = instance.Definition;
        _gearModifySlotBtn.Text        = "";
        _gearModifySlotBtn.Icon        = def != null && !string.IsNullOrEmpty(def.IconPath) ? GD.Load<Texture2D>(def.IconPath) : null;
        _gearModifySlotBtn.ExpandIcon  = _gearModifySlotBtn.Icon != null;
        _gearModifySlotBtn.TooltipText = def != null ? BuildGearTooltip(def, instance.Tier) : "";
        ApplyTierStyle(_gearModifySlotBtn, instance.Tier);

        bool atMax     = instance.Tier >= ItemTier.Max;
        bool canAfford = _manager.Profile.GetMaterial("crafting_common") >= 1;
        _gearUpgradeBtn.Text     = atMax ? "Max Tier" : $"Upgrade to {NextTierLabel(instance.Tier)}  [1 Common]";
        _gearUpgradeBtn.Disabled = atMax || !canAfford;

        // Build equipment augment slot buttons
        for (int i = 0; i < instance.MaxEquipmentAugSlots; i++)
        {
            int    captured   = i;
            string socketedId = i < instance.SocketedEquipmentAugmentIds.Count
                ? instance.SocketedEquipmentAugmentIds[i] : "";
            var    augInst    = _manager.FindEquipmentAugmentInstance(socketedId);
            var    btn        = new Button { CustomMinimumSize = new Vector2(60, 60) };

            if (augInst?.Definition != null)
            {
                btn.Text        = augInst.Definition.Name;
                btn.TooltipText = $"{augInst.Definition.Name}\nRequires: {string.Join(", ", augInst.Definition.RequiredTags)}";
                btn.AddThemeFontSizeOverride("font_size", 9);
                btn.Pressed += () =>
                {
                    _manager.RemoveEquipmentAugment(_loadedGearInstanceId!, captured);
                    RefreshGearModify();
                };
            }
            else
            {
                btn.Text        = $"E{i + 1}";
                btn.TooltipText = "Empty equipment augment slot";
                btn.Modulate    = new Color(1f, 1f, 1f, 0.5f);
                btn.Pressed     += () => OpenEquipmentAugmentPicker(_loadedGearInstanceId!, captured);
            }

            _gearEquipAugSlotsRow.AddChild(btn);
        }
    }

    private void RefreshSkillModify()
    {
        foreach (Node child in _supportSlotsRow.GetChildren())
            child.QueueFree();

        var instance = _manager.FindSkillInstance(_loadedSkillInstanceId);
        if (instance == null)
        {
            _loadedSkillInstanceId        = null;
            _skillModifySlotBtn.Text       = "Select";
            _skillModifySlotBtn.Icon       = null;
            _skillModifySlotBtn.ExpandIcon = false;
            ClearTierStyle(_skillModifySlotBtn);
            _skillUpgradeBtn.Text     = "Upgrade";
            _skillUpgradeBtn.Disabled = true;
            return;
        }

        var def = instance.Definition;
        _skillModifySlotBtn.Text        = "";
        _skillModifySlotBtn.Icon        = def != null && !string.IsNullOrEmpty(def.IconPath) ? GD.Load<Texture2D>(def.IconPath) : null;
        _skillModifySlotBtn.ExpandIcon  = _skillModifySlotBtn.Icon != null;
        _skillModifySlotBtn.TooltipText = def != null ? BuildSkillTooltip(def, instance) : "";
        ApplyTierStyle(_skillModifySlotBtn, instance.Tier);

        bool atMax     = instance.Tier >= ItemTier.Max;
        bool canAfford = _manager.Profile.GetMaterial("crafting_common") >= 1;
        _skillUpgradeBtn.Text     = atMax ? "Max Tier" : $"Upgrade to {NextTierLabel(instance.Tier)}  [1 Common]";
        _skillUpgradeBtn.Disabled = atMax || !canAfford;

        for (int i = 0; i < instance.MaxSkillAugmentSlots; i++)
        {
            int     captured   = i;
            string  socketedId = i < instance.SocketedSkillAugmentIds.Count
                ? instance.SocketedSkillAugmentIds[i] : "";
            var     augment    = _manager.FindSkillAugmentInstance(socketedId);
            var     btn        = new Button { CustomMinimumSize = new Vector2(60, 60) };

            if (augment?.Definition != null)
            {
                btn.Text        = augment.Definition.Name;
                btn.TooltipText = $"{augment.Definition.Name}\nRequires: {string.Join(", ", augment.Definition.RequiredTags)}";
                btn.AddThemeFontSizeOverride("font_size", 9);
                btn.Pressed += () =>
                {
                    _manager.RemoveSkillAugment(_loadedSkillInstanceId!, captured);
                    RefreshSkillModify();
                };
            }
            else
            {
                btn.Text        = $"S{i + 1}";
                btn.TooltipText = "Empty skill augment slot";
                btn.Modulate    = new Color(1f, 1f, 1f, 0.5f);
                btn.Pressed     += () => OpenSkillAugmentPicker(_loadedSkillInstanceId!, captured);
            }

            _supportSlotsRow.AddChild(btn);
        }
    }

    private void OnGearModifySlotPressed()
    {
        var allGear = GetAllGearInstances();
        if (allGear.Count == 0) return;

        var popup = new PopupMenu();
        for (int i = 0; i < allGear.Count; i++)
        {
            var inst = allGear[i];
            var def  = inst.Definition;
            if (def == null) continue;
            popup.AddItem($"{def.Name}  [{ItemTier.Label(inst.Tier)}]", i);
        }
        popup.IdPressed += (long id) => { _loadedGearInstanceId = allGear[(int)id].Id; RefreshGearModify(); };
        ShowPopupAt(popup, _gearModifySlotBtn);
    }

    private void OnGearUpgradePressed()
    {
        if (string.IsNullOrEmpty(_loadedGearInstanceId)) return;
        _manager.UpgradeGearItem(_loadedGearInstanceId);
        Refresh();
    }

    private void OnSkillModifySlotPressed()
    {
        var allSkills = _manager.Profile.OwnedSkillInstances;
        if (allSkills.Count == 0) return;

        var popup = new PopupMenu();
        for (int i = 0; i < allSkills.Count; i++)
        {
            var inst = allSkills[i];
            var def  = inst.Definition;
            if (def == null) continue;
            int socketed = inst.SocketedSkillAugmentIds.Count(id => !string.IsNullOrEmpty(id));
            popup.AddItem($"{def.Name}  [{ItemTier.Label(inst.Tier)}]  {socketed}/{inst.MaxSkillAugmentSlots} augments", i);
        }
        popup.IdPressed += (long id) => { _loadedSkillInstanceId = allSkills[(int)id].Id; RefreshSkillModify(); };
        ShowPopupAt(popup, _skillModifySlotBtn);
    }

    private void OnSkillUpgradePressed()
    {
        if (string.IsNullOrEmpty(_loadedSkillInstanceId)) return;
        _manager.UpgradeSkillItem(_loadedSkillInstanceId);
        Refresh();
    }

    private List<GearItemInstance> GetAllGearInstances()
    {
        var result = new List<GearItemInstance>(_manager.Profile.OwnedGearInstances);
        foreach (var c in _manager.GetAll())
            result.AddRange(c.EquippedGear.Values);
        return result;
    }

    // ── Slot interactions ─────────────────────────────────────────────────────

    private void OnGearSlotPressed(ItemSlot slot)
    {
        var c = _manager.SelectedCharacter;
        if (c == null) return;
        var anchor = slot switch
        {
            ItemSlot.Weapon => _weaponBtn,
            ItemSlot.Hat    => _hatBtn,
            ItemSlot.Body   => _bodyBtn,
            _               => _ringBtn,
        };
        if (c.EquippedGear.ContainsKey(slot.ToString()))
            ShowEquippedItemPopup(slot, anchor);
        else
            OpenPicker(slot);
    }

    private void OnSkillSlotPressed(int slotIndex)
    {
        var c = _manager.SelectedCharacter;
        if (c == null) return;
        string? instanceId = slotIndex < c.SlottedSkillInstanceIds.Count ? c.SlottedSkillInstanceIds[slotIndex] : null;
        if (!string.IsNullOrEmpty(instanceId))
            ShowEquippedSkillPopup(slotIndex, instanceId, _skillBtns[slotIndex]);
        else
            OpenSkillPicker(slotIndex);
    }

    // ── Popups ────────────────────────────────────────────────────────────────

    private void ShowInventoryItemPopup(GearItemInstance instance, ItemData item, Button anchor)
    {
        var popup = new PopupMenu();
        var c     = _manager.SelectedCharacter;

        if (c != null) popup.AddItem("Equip", 0);
        popup.AddItem("Delete", 1);

        popup.IdPressed += (long id) =>
        {
            if (id == 0 && c != null)
                _manager.EquipItem(c.Id, item.Slot, instance.Id);
            else if (id == 1)
                _manager.DeleteGearItem(instance.Id);
            Refresh();
        };

        ShowPopupAt(popup, anchor);
    }

    private void ShowSkillInventoryPopup(SkillItemInstance instance, Button anchor)
    {
        var popup = new PopupMenu();
        var c     = _manager.SelectedCharacter;

        if (c != null)
        {
            popup.AddItem("Equip to Slot 1", 0);
            popup.AddItem("Equip to Slot 2", 1);
            popup.AddItem("Equip to Slot 3", 2);
        }
        popup.AddItem("Delete", 3);

        popup.IdPressed += (long id) =>
        {
            if (id <= 2 && c != null)
                _manager.EquipSkill(c.Id, (int)id, instance.Id);
            else if (id == 3)
                _manager.DeleteSkillItem(instance.Id);
            Refresh();
        };

        ShowPopupAt(popup, anchor);
    }

    private void ShowSupportInventoryPopup(SkillAugmentInstance instance, Button anchor)
    {
        var popup = new PopupMenu();
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
        var popup = new PopupMenu();
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

        bool inventoryFull = _manager.Profile.OwnedGearInstances.Count >= Character.ProfileData.MaxInventory;
        var  popup         = new PopupMenu();

        popup.AddItem(inventoryFull ? "Unequip  (inventory full)" : "Unequip", 0);
        if (inventoryFull) popup.SetItemDisabled(0, true);
        popup.AddItem("Delete", 1);

        popup.IdPressed += (long id) =>
        {
            if (id == 0)
                _manager.UnequipItem(c.Id, slot);
            else if (id == 1)
                _manager.DeleteGearItem(instance.Id);
            Refresh();
        };

        ShowPopupAt(popup, anchor);
    }

    private void ShowEquippedSkillPopup(int slotIndex, string instanceId, Button anchor)
    {
        var c     = _manager.SelectedCharacter!;
        var popup = new PopupMenu();

        popup.AddItem("Unequip", 0);
        popup.AddItem("Delete",  1);

        popup.IdPressed += (long id) =>
        {
            if (id == 0)
                _manager.UnequipSkillSlot(c.Id, slotIndex);
            else if (id == 1)
                _manager.DeleteSkillPermanently(c.Id, slotIndex);
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

    private void OpenSkillAugmentPicker(string skillInstanceId, int slotIndex)
    {
        var skill = _manager.FindSkillInstance(skillInstanceId);
        if (skill?.Definition == null) return;

        var compatible = _manager.Profile.OwnedSkillAugmentInstances
            .Where(s => s.Definition != null &&
                        s.Definition.RequiredTags.Any(t => skill.Definition.Tags.Contains(t)))
            .ToList();

        if (compatible.Count == 0)
        {
            var nonePopup = new PopupMenu();
            nonePopup.AddItem("No compatible skill augments owned", 0);
            nonePopup.SetItemDisabled(0, true);
            AddChild(nonePopup);
            nonePopup.PopupHide += nonePopup.QueueFree;
            nonePopup.ResetSize();
            var rowRect = _supportSlotsRow.GetGlobalRect();
            nonePopup.Position = new Vector2I((int)rowRect.Position.X, (int)(rowRect.Position.Y + rowRect.Size.Y));
            nonePopup.Popup();
            return;
        }

        var popup = new PopupMenu();
        for (int i = 0; i < compatible.Count; i++)
            popup.AddItem($"{compatible[i].Definition!.Name}  [{string.Join(", ", compatible[i].Definition!.RequiredTags)}]", i);

        popup.IdPressed += (long id) =>
        {
            _manager.SocketSkillAugment(skillInstanceId, slotIndex, compatible[(int)id].Id);
            RefreshSkillModify();
        };

        var slotButtons = _supportSlotsRow.GetChildren();
        Control anchor  = slotIndex < slotButtons.Count && slotButtons[slotIndex] is Control c2 ? c2 : _supportSlotsRow;
        AddChild(popup);
        popup.PopupHide += popup.QueueFree;
        popup.ResetSize();
        var rect = anchor.GetGlobalRect();
        popup.Position = new Vector2I((int)rect.Position.X, (int)(rect.Position.Y + rect.Size.Y));
        popup.Popup();
    }

    private void OpenEquipmentAugmentPicker(string gearInstanceId, int slotIndex)
    {
        var gear = _manager.FindGearInstance(gearInstanceId);
        if (gear?.Definition == null) return;

        var compatible = _manager.Profile.OwnedEquipmentAugmentInstances
            .Where(a => a.Definition != null &&
                        (a.Definition.RequiredTags.Length == 0 ||
                         a.Definition.RequiredTags.Any(t => gear.Definition.Tags.Contains(t))))
            .ToList();

        if (compatible.Count == 0)
        {
            var nonePopup = new PopupMenu();
            nonePopup.AddItem("No compatible equipment augments owned", 0);
            nonePopup.SetItemDisabled(0, true);
            AddChild(nonePopup);
            nonePopup.PopupHide += nonePopup.QueueFree;
            nonePopup.ResetSize();
            var rowRect = _gearEquipAugSlotsRow.GetGlobalRect();
            nonePopup.Position = new Vector2I((int)rowRect.Position.X, (int)(rowRect.Position.Y + rowRect.Size.Y));
            nonePopup.Popup();
            return;
        }

        var popup = new PopupMenu();
        for (int i = 0; i < compatible.Count; i++)
            popup.AddItem($"{compatible[i].Definition!.Name}  [{string.Join(", ", compatible[i].Definition!.RequiredTags)}]", i);

        popup.IdPressed += (long id) =>
        {
            _manager.SocketEquipmentAugment(gearInstanceId, slotIndex, compatible[(int)id].Id);
            RefreshGearModify();
        };

        var slotButtons = _gearEquipAugSlotsRow.GetChildren();
        Control anchor  = slotIndex < slotButtons.Count && slotButtons[slotIndex] is Control c2 ? c2 : _gearEquipAugSlotsRow;
        AddChild(popup);
        popup.PopupHide += popup.QueueFree;
        popup.ResetSize();
        var rect = anchor.GetGlobalRect();
        popup.Position = new Vector2I((int)rect.Position.X, (int)(rect.Position.Y + rect.Size.Y));
        popup.Popup();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyTierStyle(Button btn, int tier)
    {
        var style = new StyleBoxFlat { BgColor = ItemTier.BackgroundColor(tier) with { A = 0.55f } };
        btn.AddThemeStyleboxOverride("normal", style);
    }

    private static void ClearTierStyle(Button btn)
    {
        btn.RemoveThemeStyleboxOverride("normal");
    }

    private static string NextTierLabel(int tier) => tier switch
    {
        1 => "Uncommon",
        2 => "Rare",
        _ => "Max",
    };

    private static string BuildGearTooltip(ItemData item, int tier)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{item.Name}  [{item.Slot}]  [{ItemTier.Label(tier)}]");
        if (item.BonusHp            != 0)  sb.Append($"\nHP {item.BonusHp:+#;-#;0}");
        if (item.BonusSpeed         != 0f) sb.Append($"\nSpeed {item.BonusSpeed:+#;-#;0}");
        if (item.WeaponRange        != 0f) sb.Append($"\nWeapon Range {item.WeaponRange:0}");
        if (item.RangeModifier      != 0f) sb.Append($"\nRange Modifier {item.RangeModifier:+#;-#;0}");
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
