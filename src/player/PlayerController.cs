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
    public float EffectiveRange     { get; private set; }

    private Stats.StatBlock _statBlock = new();
    private Character.CharacterData? _charData;
    private Node3D _model = null!;
    private AnimationNodeStateMachinePlayback? _smPlayback;

    public float CurrentHealth { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNextLevel { get; private set; } = 20;

    private float _yaw;
    private const float RotationSpeed = 20f;

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
            var hat    = GetEquippedItem(c, Items.ItemSlot.Hat);
            var body   = GetEquippedItem(c, Items.ItemSlot.Body);

            DamageReduction = (hat?.DamageReduction ?? 0f) + (body?.DamageReduction ?? 0f);
            EffectiveRange  = (weapon?.WeaponRange ?? 200f) + (hat?.RangeModifier ?? 0f) + (body?.RangeModifier ?? 0f);

            var weaponController = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            weaponController?.SetDamage(
                _statBlock.Get(Stats.StatId.PhysicalDamage),
                _statBlock.Get(Stats.StatId.MagicDamage));
            weaponController?.SetRange(EffectiveRange);

            for (int i = 0; i < 3 && i < c.SlottedSkillInstanceIds.Count; i++)
            {
                var instanceId = c.SlottedSkillInstanceIds[i];
                if (string.IsNullOrEmpty(instanceId)) continue;
                var instance = manager.FindSkillInstance(instanceId);
                var skill    = instance?.Definition;
                if (skill == null) continue;

                var eotIds   = new List<string>();
                bool hasSplash = false, hasPierce = false;
                if (instance != null)
                {
                    foreach (var augInstId in instance.SocketedSkillAugmentIds)
                    {
                        if (string.IsNullOrEmpty(augInstId)) continue;
                        var augInst = manager.FindSkillAugmentInstance(augInstId);
                        if (augInst?.DefinitionId == "splash") hasSplash = true;
                        if (augInst?.DefinitionId == "pierce") hasPierce = true;
                        var eotId = augInst?.Definition?.EotId;
                        if (eotId != null) eotIds.Add(eotId);
                    }
                }

                weaponController?.SetSlot(i, skill, eotIds, hasSplash, hasPierce);
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
            if (fallback != null) wc?.SetSlot(0, fallback);
        }

        _mendingTimer = 3.0f;

        GlobalPosition = Vector3.Zero;
        CurrentHealth  = MaxHealth;
        AddToGroup("player");

        var visuals = new Node3D();
        visuals.Scale = new Vector3(9f, 9f, 9f);
        AddChild(visuals);
        _model = visuals;

        string modelPath = _charData?.Type switch
        {
            Character.CharacterType.Warrior => "res://assets/models/characters/kaykit_warrior.glb",
            Character.CharacterType.Rogue   => "res://assets/models/characters/kaykit_rogue.glb",
            Character.CharacterType.Mage    => "res://assets/models/characters/kaykit_mage.glb",
            _                               => "res://assets/models/characters/kaykit_warrior.glb",
        };
        var playerModel = GD.Load<PackedScene>(modelPath).Instantiate<Node3D>();
        visuals.AddChild(playerModel);

        var animPlayer = playerModel.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (animPlayer == null)
        {
            animPlayer = new AnimationPlayer();
            playerModel.AddChild(animPlayer);
        }

        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_general.glb",  "Idle_A",              "idle",   Animation.LoopModeEnum.Linear);
        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_movement.glb", "Running_A",           "run",    Animation.LoopModeEnum.Linear);
        LoadAnimClip(animPlayer, "res://assets/models/characters/kaykit_anim_melee.glb",    "Melee_1H_Attack_Chop", "attack", Animation.LoopModeEnum.None);

        var animTree = GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree != null)
        {
            animTree.AnimPlayer = animTree.GetPathTo(animPlayer);
            animTree.Active = true;
        }

        GetNodeOrNull<Weapon.WeaponController>("Weapon")?.Connect(
            Weapon.WeaponController.SignalName.SkillFired,
            Callable.From<int, float, bool>(OnSkillFired));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_smPlayback == null)
        {
            var at = GetNodeOrNull<AnimationTree>("AnimationTree");
            if (at != null)
                _smPlayback = at.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        }

        var input     = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var direction = new Vector3(input.X, 0f, input.Y);

        float moveSpeed = Speed + (_dashReflexTimer > 0f ? 100f : 0f);
        Velocity = direction * moveSpeed;
        MoveAndSlide();

        bool moving = direction.LengthSquared() > 0.01f;
        if (moving)
        {
            float targetYaw = Mathf.Atan2(direction.X, direction.Z);
            _yaw = Mathf.LerpAngle(_yaw, targetYaw, Mathf.Min(1f, RotationSpeed * (float)delta));
            _model.Rotation = new Vector3(0f, _yaw, 0f);
        }
        else
        {
            var nearest = FindNearestEnemy();
            if (nearest != null)
            {
                var toEnemy = (nearest.GlobalPosition - GlobalPosition).Normalized();
                float targetYaw = Mathf.Atan2(toEnemy.X, toEnemy.Z);
                _yaw = Mathf.LerpAngle(_yaw, targetYaw, Mathf.Min(1f, RotationSpeed * (float)delta));
                _model.Rotation = new Vector3(0f, _yaw, 0f);
            }
        }

        if (_smPlayback != null)
        {
            var current = _smPlayback.GetCurrentNode();
            if (current != "attack")
            {
                var want = moving ? "run" : "idle";
                if (current != want)
                    _smPlayback.Travel(want);
            }
        }

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

    private static readonly PackedScene SwingVfxScene =
        GD.Load<PackedScene>("res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn");

    private void OnSkillFired(int slotIndex, float cooldown, bool isMelee)
    {
        _smPlayback?.Travel("attack");
        if (!isMelee) return;

        try
        {
            var fx  = SwingVfxScene.Instantiate<GpuParticles3D>();
            var mat = (ParticleProcessMaterial)fx.ProcessMaterial.Duplicate();
            mat.ScaleMin = 35f;
            mat.ScaleMax = 55f;
            fx.Amount    = 12;
            fx.Lifetime  = 0.6f;
            fx.ProcessMaterial = mat;
            GetTree().Root.AddChild(fx);
            fx.GlobalPosition = GlobalPosition + new Vector3(0f, 20f, 0f);
            fx.Call("activate_effects");
            GetTree().CreateTimer(2.0).Timeout += fx.QueueFree;
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"SwingEffect failed: {e.Message}");
        }
    }

    private static int FindBone(Skeleton3D skeleton, string name)
    {
        int idx = skeleton.FindBone(name);
        return idx >= 0 ? idx : skeleton.FindBone(name + "_2");
    }

    private static void AttachArmourToSkeleton(Node3D armourRoot, Skeleton3D skeleton)
    {
        int chestIdx = FindBone(skeleton, "Chest");
        int headIdx  = FindBone(skeleton, "Head");
        Vector3 chestOrigin = chestIdx >= 0 ? skeleton.GetBoneGlobalRest(chestIdx).Origin : Vector3.Zero;
        Vector3 headOrigin  = headIdx  >= 0 ? skeleton.GetBoneGlobalRest(headIdx).Origin  : Vector3.Zero;

        string chestBoneName = chestIdx >= 0 ? skeleton.GetBoneName(chestIdx) : "Chest";
        string headBoneName  = headIdx  >= 0 ? skeleton.GetBoneName(headIdx)  : "Head";
        var chestAttach = new BoneAttachment3D { BoneName = chestBoneName };
        var headAttach  = new BoneAttachment3D { BoneName = headBoneName };
        skeleton.AddChild(chestAttach);
        skeleton.AddChild(headAttach);

        var pieces = new List<(Node3D n, Vector3 pos)>();
        foreach (var child in armourRoot.GetChildren())
            if (child is Node3D n) pieces.Add((n, n.Position));

        foreach (var (piece, origPos) in pieces)
        {
            string name = piece.Name.ToString();
            bool isHead = name.Contains("Cap") || name.Contains("Hood") || name.Contains("Helm");
            var attach = isHead ? headAttach : chestAttach;
            Vector3 boneOrigin = isHead ? headOrigin : chestOrigin;

            armourRoot.RemoveChild(piece);
            attach.AddChild(piece);
            piece.Position = origPos - boneOrigin;
        }

        armourRoot.GetParent()?.RemoveChild(armourRoot);
        armourRoot.QueueFree();
    }

    private static void AttachWeaponToSkeleton(Node3D weaponRoot, Skeleton3D skeleton)
    {
        int handIdx = FindBone(skeleton, "Hand_R");
        string handBoneName = handIdx >= 0 ? skeleton.GetBoneName(handIdx) : "Hand_R";
        var attach = new BoneAttachment3D { BoneName = handBoneName };
        skeleton.AddChild(attach);

        var pieces = new List<(Node3D n, Vector3 pos)>();
        foreach (var child in weaponRoot.GetChildren())
            if (child is Node3D n) pieces.Add((n, n.Position));

        // Anchor: use the Handle piece, or fall back to the first piece
        Vector3 anchorPos = pieces.Count > 0 ? pieces[0].pos : Vector3.Zero;
        foreach (var (n, p) in pieces)
            if (n.Name.ToString().Contains("Handle")) { anchorPos = p; break; }

        foreach (var (piece, origPos) in pieces)
        {
            weaponRoot.RemoveChild(piece);
            attach.AddChild(piece);
            piece.Position = origPos - anchorPos;
        }

        weaponRoot.GetParent()?.RemoveChild(weaponRoot);
        weaponRoot.QueueFree();
    }

    private void LoadAnimClip(AnimationPlayer target, string sourcePath, string sourceName, string targetName, Animation.LoopModeEnum loop)
    {
        var sourceScene = GD.Load<PackedScene>(sourcePath);
        if (sourceScene == null) return;
        var sourceRoot = sourceScene.Instantiate<Node3D>();
        AddChild(sourceRoot);
        var sourcePlayer = sourceRoot.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (sourcePlayer != null && sourcePlayer.HasAnimation(sourceName))
        {
            var copy = (Animation)sourcePlayer.GetAnimation(sourceName).Duplicate();
            copy.LoopMode = loop;

            if (!target.HasAnimationLibrary(""))
                target.AddAnimationLibrary("", new AnimationLibrary());
            target.GetAnimationLibrary("").AddAnimation(targetName, copy);
        }
        sourceRoot.QueueFree();
    }

    private Node3D? FindNearestEnemy()
    {
        Node3D? nearest = null;
        float nearestDistSq = float.MaxValue;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Node3D enemy || enemy.IsQueuedForDeletion()) continue;
            float distSq = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (distSq < nearestDistSq) { nearestDistSq = distSq; nearest = enemy; }
        }
        return nearest;
    }

    private static int ComputeXpToNextLevel(int level)
    {
        int xtn = 20;
        for (int i = 1; i < level; i++)
            xtn = (int)(xtn * 1.4f);
        return xtn;
    }
}
