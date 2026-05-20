using Godot;

namespace Godot1.Player;

public partial class PlayerController : CharacterBody2D
{
    [Signal] public delegate void HealthChangedEventHandler(int newHealth);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void XpChangedEventHandler(int currentXp, int xpToNextLevel);
    [Signal] public delegate void LeveledUpEventHandler(int newLevel);

    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNextLevel { get; private set; } = 20;

    private static readonly Texture2D CharTex =
        GD.Load<Texture2D>("res://assets/kenney_topdown_rpg/Roguelike Characters Pack/Spritesheet/roguelikeChar_transparent.png");

    public override void _Ready()
    {
        var manager = GetNodeOrNull<Character.CharacterManager>("/root/CharacterManager");
        Character.CharacterType type = Character.CharacterType.Warrior;

        if (manager?.SelectedCharacter != null)
        {
            var c = manager.SelectedCharacter;
            var (hp, spd, dmg) = c.BaseStats();

            MaxHealth = hp;
            Speed     = spd;
            type      = c.Type;

            Level        = c.CurrentLevel;
            CurrentXp    = c.CurrentXp;
            XpToNextLevel = ComputeXpToNextLevel(Level);

            int levelsAboveOne = Level - 1;
            MaxHealth += levelsAboveOne * 5;
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(dmg + levelsAboveOne);
        }
        else
        {
            XpToNextLevel = ComputeXpToNextLevel(Level);
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(20f);
        }

        CurrentHealth = MaxHealth;
        AddToGroup("player");
        SetupSprite(type);
    }

    private void SetupSprite(Character.CharacterType type)
    {
        int row = type switch
        {
            Character.CharacterType.Warrior => 3,
            Character.CharacterType.Rogue   => 0,
            Character.CharacterType.Mage    => 5,
            _                               => 0
        };
        AddChild(new Sprite2D
        {
            Texture       = CharTex,
            RegionEnabled = true,
            RegionRect    = new Rect2(0, row * 17, 16, 16),
            Scale         = new Vector2(2f, 2f)
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * Speed;
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
            MaxHealth     += 5;
            CurrentHealth  = Mathf.Min(CurrentHealth + 5, MaxHealth);
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.AddDamage(1f);
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
