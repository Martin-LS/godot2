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

    public override void _Ready()
    {
        var manager = GetNodeOrNull<Character.CharacterManager>("/root/CharacterManager");
        if (manager?.SelectedCharacter != null)
        {
            var (hp, spd, dmg) = manager.SelectedCharacter.BaseStats();
            MaxHealth = hp;
            Speed = spd;
            GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetDamage(dmg);
        }

        CurrentHealth = MaxHealth;
        AddToGroup("player");
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 16f, Colors.CornflowerBlue);
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
            CurrentXp -= XpToNextLevel;
            Level++;
            XpToNextLevel = (int)(XpToNextLevel * 1.4f);
            EmitSignal(SignalName.LeveledUp, Level);
        }
        EmitSignal(SignalName.XpChanged, CurrentXp, XpToNextLevel);
    }
}
