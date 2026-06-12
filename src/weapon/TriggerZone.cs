using Godot;
using System.Collections.Generic;
using Godot1.Eot;
using Godot1.Enemies;

namespace Godot1.Weapon;

public partial class TriggerZone : Node3D
{
    public float            Damage;
    public Items.DamageType DmgType;
    public float            BlastRadius;
    public float            TriggerRadius;
    public float            Duration;
    public float            ArmTime;
    public List<string>     EotIds         = new();
    public float            CritMultiplier = 1f;

    private float                _elapsed;
    private bool                 _armed;
    private StandardMaterial3D?  _mat;

    public override void _Ready()
    {
        var disc = new CylinderMesh
        {
            TopRadius      = TriggerRadius,
            BottomRadius   = TriggerRadius,
            Height         = 20f,
            RadialSegments = 64,
            Rings          = 1,
        };
        _mat = new StandardMaterial3D
        {
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor  = new Color(0.5f, 0.5f, 0.1f, 0.35f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled,
        };
        AddChild(new MeshInstance3D { Mesh = disc, MaterialOverride = _mat });
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;

        if (!_armed)
        {
            if (_elapsed < ArmTime) { CheckExpiry(); return; }
            _armed = true;
            if (_mat != null)
                _mat.AlbedoColor = new Color(1.0f, 0.75f, 0.05f, 0.7f);
        }

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
            if (GlobalPosition.DistanceTo(enemy.GlobalPosition) > TriggerRadius) continue;
            Burst();
            return;
        }

        CheckExpiry();
    }

    private void CheckExpiry()
    {
        if (_elapsed >= Duration) QueueFree();
    }

    private void Burst()
    {
        bool isCrit = CritMultiplier > 1f;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
            if (GlobalPosition.DistanceTo(enemy.GlobalPosition) > BlastRadius) continue;
            enemy.TakeDamage(Damage, DmgType, isCrit);
            foreach (var eotId in EotIds)
            {
                var eot = EotRegistry.Get(eotId);
                if (eot != null && GD.Randf() < eot.ApplyChance)
                    enemy.ApplyEot(eot, CritMultiplier);
            }
        }
        QueueFree();
    }
}
