using Godot;
using System.Collections.Generic;
using Godot1.Skills;

namespace Godot1.Player;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void HealthChangedEventHandler(float newHealth);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void XpChangedEventHandler(int currentXp, int xpToNextLevel);
    [Signal] public delegate void LeveledUpEventHandler(int newLevel);

    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 100;

    public float DamageReduction    { get; private set; }
    public float PhysicalResistance { get; private set; }
    public float MagicResistance    { get; private set; }

    private Stats.StatBlock _statBlock = new();
    private Character.CharacterData? _charData;
    private Node3D _model = null!;

    public float CurrentHealth { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNextLevel { get; private set; } = 20;

    // Equipment augment state
    private readonly HashSet<string> _activeAugments = new();
    private bool  _fortifyActive;
    private float _dashReflexTimer;
    private float _ghostStepTimer;
    private float _mendingTimer;

    public override void _Ready()
    {
        var manager = GetNodeOrNull<Character.CharacterManager>("/root/CharacterManager");

        if (manager?.SelectedCharacter != null)
        {
            var c = manager.SelectedCharacter;
            _charData  = c;
            _statBlock = c.BuildStatBlock();

            MaxHealth          = (int)_statBlock.Get(Stats.StatId.MaxHp);
            Speed              = _statBlock.Get(Stats.StatId.Speed);
            PhysicalResistance = _statBlock.Get(Stats.StatId.PhysicalResistance);
            MagicResistance    = _statBlock.Get(Stats.StatId.MagicResistance);

            Level         = c.CurrentLevel;
            CurrentXp     = c.CurrentXp;
            XpToNextLevel = ComputeXpToNextLevel(Level);

            var weapon = GetEquippedItem(c, Items.ItemSlot.Weapon);
            var armor  = GetEquippedItem(c, Items.ItemSlot.Armor);

            DamageReduction = armor?.DamageReduction ?? 0f;

            var weaponController = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            weaponController?.SetDamage(
                _statBlock.Get(Stats.StatId.PhysicalDamage),
                _statBlock.Get(Stats.StatId.MagicDamage));

            for (int i = 0; i < 3 && i < c.SlottedSkillInstanceIds.Count; i++)
            {
                var instanceId = c.SlottedSkillInstanceIds[i];
                if (string.IsNullOrEmpty(instanceId)) continue;
                var instance = manager.FindSkillInstance(instanceId);
                var skill    = instance?.Definition;
                if (skill == null) continue;
                bool matches = weapon != null
                    && weapon.WeaponAffinity != Items.WeaponAffinity.None
                    && System.Array.Exists(skill.Tags, t => t == weapon.WeaponAffinity.ToString());

                var eotIds   = new List<string>();
                bool hasSplash = false, hasPierce = false;
                if (instance != null)
                {
                    foreach (var supportInstId in instance.SocketedSupportInstanceIds)
                    {
                        if (string.IsNullOrEmpty(supportInstId)) continue;
                        var supportInst = manager.FindSupportInstance(supportInstId);
                        if (supportInst?.DefinitionId == "splash") hasSplash = true;
                        if (supportInst?.DefinitionId == "pierce") hasPierce = true;
                        var eotId = supportInst?.Definition?.EotId;
                        if (eotId != null) eotIds.Add(eotId);
                    }
                }

                weaponController?.SetSlot(i, skill, matches ? weapon!.SkillBonus : 0f, eotIds, hasSplash, hasPierce);
            }

            // Seed equipment augments from all equipped gear
            _activeAugments.Clear();
            foreach (var kvp in c.EquippedGear)
            {
                foreach (var augInstId in kvp.Value.SocketedEquipmentAugmentIds)
                {
                    if (string.IsNullOrEmpty(augInstId)) continue;
                    var augInst = manager.FindEquipmentAugmentInstance(augInstId);
                    if (augInst?.DefinitionId != null)
                        _activeAugments.Add(augInst.DefinitionId);
                }
            }
        }
        else
        {
            _statBlock.SetBase(Stats.StatId.MaxHp,         MaxHealth);
            _statBlock.SetBase(Stats.StatId.Speed,          Speed);
            _statBlock.SetBase(Stats.StatId.PhysicalDamage, 20f);
            _statBlock.SetBase(Stats.StatId.MagicDamage,    0f);
            XpToNextLevel = ComputeXpToNextLevel(Level);
            var wc = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            wc?.SetDamage(20f, 0f);
            var fallback = SkillRegistry.Get("strike");
            if (fallback != null) wc?.SetSlot(0, fallback, 0f);
        }

        _mendingTimer = 3.0f;

        GlobalPosition = Vector3.Zero;
        CurrentHealth  = MaxHealth;
        AddToGroup("player");

        var visuals = new Node3D();
        visuals.Scale = new Vector3(9f, 9f, 9f);
        AddChild(visuals);
        _model = visuals;

        visuals.AddChild(GD.Load<PackedScene>("res://assets/models/characters/player.glb").Instantiate<Node3D>());

        if (_charData != null)
        {
            var armorItem  = GetEquippedItem(_charData, Items.ItemSlot.Armor);
            var weaponItem = GetEquippedItem(_charData, Items.ItemSlot.Weapon);

            var armourPath = armorItem?.ArmorCategory switch
            {
                Items.ArmorCategory.Heavy  => "res://assets/models/equipment/armour_heavy.glb",
                Items.ArmorCategory.Medium => "res://assets/models/equipment/armour_medium.glb",
                Items.ArmorCategory.Light  => "res://assets/models/equipment/armour_light.glb",
                _                          => null,
            };
            if (armourPath != null)
                visuals.AddChild(GD.Load<PackedScene>(armourPath).Instantiate<Node3D>());

            var weaponPath = weaponItem?.WeaponAffinity switch
            {
                Items.WeaponAffinity.Melee  => "res://assets/models/equipment/weapon_sword.glb",
                Items.WeaponAffinity.Ranged => "res://assets/models/equipment/weapon_bow.glb",
                Items.WeaponAffinity.Magic  => "res://assets/models/equipment/weapon_wand.glb",
                _                           => null,
            };
            if (weaponPath != null)
                visuals.AddChild(GD.Load<PackedScene>(weaponPath).Instantiate<Node3D>());
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var input     = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var direction = new Vector3(input.X, 0f, input.Y);

        float moveSpeed = Speed + (_dashReflexTimer > 0f ? 100f : 0f);
        Velocity = direction * moveSpeed;
        MoveAndSlide();

        if (direction.LengthSquared() > 0.01f)
            _model.LookAt(GlobalPosition + direction, Vector3.Up);

        float dt = (float)delta;
        if (_dashReflexTimer > 0f) _dashReflexTimer -= dt;
        if (_ghostStepTimer  > 0f) _ghostStepTimer  -= dt;

        if (_activeAugments.Contains("mending"))
        {
            _mendingTimer -= dt;
            if (_mendingTimer <= 0f)
            {
                Heal(5);
                _mendingTimer = 3.0f;
            }
        }
    }

    public void TakeDamage(float rawAmount, Items.DamageType type, Node3D? attacker = null)
    {
        float effective = rawAmount * (1f - DamageReduction);
        if (type == Items.DamageType.Physical)
            effective *= (1f - PhysicalResistance);
        else if (type == Items.DamageType.Magic)
            effective *= (1f - MagicResistance);

        // Fortify: if active, reduce this hit; always refresh for next hit
        if (_activeAugments.Contains("fortify"))
        {
            if (_fortifyActive) effective *= 0.5f;
            _fortifyActive = true;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - effective);
        EmitSignal(SignalName.HealthChanged, CurrentHealth);

        // Retaliation: deal damage back to attacker
        if (_activeAugments.Contains("retaliation") && attacker is Enemies.EnemyController ec)
            ec.TakeDamage(5f, Items.DamageType.Physical);

        // Dash Reflex: brief speed boost on hit
        if (_activeAugments.Contains("dash_reflex"))
            _dashReflexTimer = 1.0f;

        // Ghost Step: arm kill-heal window
        if (_activeAugments.Contains("ghost_step"))
            _ghostStepTimer = 2.0f;

        if (CurrentHealth == 0f)
            EmitSignal(SignalName.PlayerDied);
    }

    public void OnEnemyKilled()
    {
        // Ghost Step: heal if killed within 2s of being hit
        if (_activeAugments.Contains("ghost_step") && _ghostStepTimer > 0f)
            Heal(10);

        // Adaptation: reduce active skill cooldowns on kill
        if (_activeAugments.Contains("adaptation"))
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.ReduceCooldowns(0.3f);
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth);
    }

    public void CollectXp(int amount)
    {
        CurrentXp += amount;
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp     -= XpToNextLevel;
            Level++;
            XpToNextLevel  = ComputeXpToNextLevel(Level);

            if (_charData != null)
            {
                _charData.CurrentLevel = Level;
                _statBlock = _charData.BuildStatBlock();
                Speed              = _statBlock.Get(Stats.StatId.Speed);
                PhysicalResistance = _statBlock.Get(Stats.StatId.PhysicalResistance);
                MagicResistance    = _statBlock.Get(Stats.StatId.MagicResistance);
                GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(
                    _statBlock.Get(Stats.StatId.PhysicalDamage),
                    _statBlock.Get(Stats.StatId.MagicDamage));
            }

            MaxHealth     = (int)_statBlock.Get(Stats.StatId.MaxHp);
            CurrentHealth = Mathf.Min(CurrentHealth + 5f, MaxHealth);
            EmitSignal(SignalName.LeveledUp, Level);
        }
        EmitSignal(SignalName.XpChanged, CurrentXp, XpToNextLevel);
    }

    private static Items.ItemData? GetEquippedItem(Character.CharacterData c, Items.ItemSlot slot)
    {
        c.EquippedGear.TryGetValue(slot.ToString(), out var instance);
        return instance?.Definition;
    }

    private static int ComputeXpToNextLevel(int level)
    {
        int xtn = 20;
        for (int i = 1; i < level; i++)
            xtn = (int)(xtn * 1.4f);
        return xtn;
    }
}
