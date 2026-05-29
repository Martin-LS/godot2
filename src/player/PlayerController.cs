using Godot;

namespace Godot1.Player;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void HealthChangedEventHandler(int newHealth);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void XpChangedEventHandler(int currentXp, int xpToNextLevel);
    [Signal] public delegate void LeveledUpEventHandler(int newLevel);

    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 100;

    private Stats.StatBlock _statBlock = new();

    public int CurrentHealth { get; private set; }
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

            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(_statBlock.Get(Stats.StatId.Damage));
        }
        else
        {
            _statBlock.SetBase(Stats.StatId.MaxHp,  MaxHealth);
            _statBlock.SetBase(Stats.StatId.Speed,  Speed);
            _statBlock.SetBase(Stats.StatId.Damage, 20f);
            XpToNextLevel = ComputeXpToNextLevel(Level);
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(20f);
        }

        CurrentHealth = MaxHealth;
        AddToGroup("player");
        AddChild(new MeshInstance3D
        {
            Mesh     = new BoxMesh { Size = new Vector3(32f, 48f, 32f) },
            Position = new Vector3(0f, 24f, 0f),
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = new Vector3(input.X, 0f, input.Y) * Speed;
        MoveAndSlide();
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth);

        if (CurrentHealth == 0)
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
            MaxHealth      = (int)_statBlock.Get(Stats.StatId.MaxHp);
            CurrentHealth  = Mathf.Min(CurrentHealth + 5, MaxHealth);
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(_statBlock.Get(Stats.StatId.Damage));
            EmitSignal(SignalName.LeveledUp, Level);
        }
        EmitSignal(SignalName.XpChanged, CurrentXp, XpToNextLevel);
    }

    private static int ComputeXpToNextLevel(int level)
    {
        int xtn = 20;
        for (int i = 1; i < level; i++)
            xtn = (int)(xtn * 1.4f);
        return xtn;
    }
}
