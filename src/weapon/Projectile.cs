using Godot;

namespace Godot1.Weapon;

public partial class Projectile : Area2D
{
    public float Damage;
    public float Speed = 500f;
    public float MaxRange = 600f;

    private Vector2 _direction;
    private float _traveled;

    public void Initialize(Vector2 direction, float damage)
    {
        _direction = direction.Normalized();
        Damage = damage;
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 5f, Colors.White);
    }

    public override void _PhysicsProcess(double delta)
    {
        var step = _direction * Speed * (float)delta;
        GlobalPosition += step;
        _traveled += step.Length();

        if (_traveled >= MaxRange)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Enemies.EnemyController enemy)
        {
            enemy.TakeDamage((int)Damage);
            QueueFree();
        }
    }
}
