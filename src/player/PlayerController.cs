using Godot;
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

    private Stats.StatBlock _statBlock = new();
    private Node3D _model = null!;

    public float CurrentHealth { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNextLevel { get; private set; } = 20;

    public override void _Ready()
    {
        var manager = GetNodeOrNull<Character.CharacterManager>("/root/CharacterManager");

        if (manager?.SelectedCharacter != null)
        {
            var c = manager.SelectedCharacter;
            _statBlock = c.BuildStatBlock();

            MaxHealth = (int)_statBlock.Get(Stats.StatId.MaxHp);
            Speed     = _statBlock.Get(Stats.StatId.Speed);

            Level         = c.CurrentLevel;
            CurrentXp     = c.CurrentXp;
            XpToNextLevel = ComputeXpToNextLevel(Level);

            var weapon = GetEquippedItem(c, Items.ItemSlot.Weapon);
            var armor  = GetEquippedItem(c, Items.ItemSlot.Armor);
            var acc    = GetEquippedItem(c, Items.ItemSlot.Accessory);

            DamageReduction    = armor?.DamageReduction    ?? 0f;
            PhysicalResistance = acc?.PhysicalResistance   ?? 0f;

            var weaponController = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            weaponController?.SetDamage(_statBlock.Get(Stats.StatId.Damage));

            for (int i = 0; i < 3 && i < c.SlottedSkillInstanceIds.Count; i++)
            {
                var instanceId = c.SlottedSkillInstanceIds[i];
                if (string.IsNullOrEmpty(instanceId)) continue;
                var skill = manager.FindSkillInstance(instanceId)?.Definition;
                if (skill == null) continue;
                bool matches = weapon != null && weapon.WeaponAffinity switch
                {
                    Items.WeaponAffinity.Melee          => skill.Category == Items.SkillCategory.Melee,
                    Items.WeaponAffinity.RangedPhysical => skill.Category == Items.SkillCategory.RangedPhysical,
                    Items.WeaponAffinity.RangedMagic    => skill.Category == Items.SkillCategory.RangedMagic,
                    _                                   => false,
                };
                weaponController?.SetSlot(i, skill, matches ? weapon!.SkillBonus : 0f);
            }
        }
        else
        {
            _statBlock.SetBase(Stats.StatId.MaxHp,  MaxHealth);
            _statBlock.SetBase(Stats.StatId.Speed,  Speed);
            _statBlock.SetBase(Stats.StatId.Damage, 20f);
            XpToNextLevel = ComputeXpToNextLevel(Level);
            var wc = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            wc?.SetDamage(20f);
            var fallback = SkillRegistry.Get("attack_melee");
            if (fallback != null) wc?.SetSlot(0, fallback, 0f);
        }

        GlobalPosition = Vector3.Zero;
        CurrentHealth  = MaxHealth;
        AddToGroup("player");
        _model = GD.Load<PackedScene>("res://assets/models/characters/Knight.glb").Instantiate<Node3D>();
        _model.Scale = new Vector3(12f, 12f, 12f);
        AddChild(_model);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var direction = new Vector3(input.X, 0f, input.Y);
        Velocity = direction * Speed;
        MoveAndSlide();

        if (direction.LengthSquared() > 0.01f)
            _model.LookAt(GlobalPosition + direction, Vector3.Up);
    }

    public void TakeDamage(float rawAmount, Items.DamageType type)
    {
        float effective = rawAmount * (1f - DamageReduction);
        if (type == Items.DamageType.Physical)
            effective *= (1f - PhysicalResistance);
        CurrentHealth = Mathf.Max(0f, CurrentHealth - effective);
        EmitSignal(SignalName.HealthChanged, CurrentHealth);

        if (CurrentHealth == 0f)
            EmitSignal(SignalName.PlayerDied);
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
            _statBlock.AddModifier(new Stats.StatModifier(Stats.StatId.MaxHp,  Stats.ModifierType.FlatAdd, 5f, Stats.ModifierSource.Level));
            _statBlock.AddModifier(new Stats.StatModifier(Stats.StatId.Damage, Stats.ModifierType.FlatAdd, 1f, Stats.ModifierSource.Level));
            MaxHealth     = (int)_statBlock.Get(Stats.StatId.MaxHp);
            CurrentHealth = Mathf.Min(CurrentHealth + 5f, MaxHealth);
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(_statBlock.Get(Stats.StatId.Damage));
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
