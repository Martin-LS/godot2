using Godot;
using System.Collections.Generic;
using Godot1.Skills;
using Godot1;
using Godot1.Enemies;

namespace Godot1.Player;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void HealthChangedEventHandler(float newHealth);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void PlayerHitEventHandler();
    [Signal] public delegate void XpChangedEventHandler(int currentXp, int xpToNextLevel);
    [Signal] public delegate void LeveledUpEventHandler(int newLevel);
    [Signal] public delegate void FocusChangedEventHandler(float current, float max);
    [Signal] public delegate void ShieldChangedEventHandler(float current, float max);
    [Signal] public delegate void DamageTakenEventHandler(float effectiveDamage, bool isMagic);

    [Export] public float Speed = 200f;
    [Export] public int MaxHealth = 100;

    public float DamageReduction    { get; private set; }
    public float PhysicalResistance { get; private set; }
    public float MagicResistance    { get; private set; }
    public float EffectiveRange     { get; private set; }

    private float _rangeBuffBonus; // flat tile bonus from active range buffs (e.g. Shout)

    private static readonly PackedScene TargetIndicatorScene =
        GD.Load<PackedScene>("res://src/vfx/target_indicator.tscn");

    private Node3D? _targetIndicator;
    private Node3D? _aimReticle;

    private Stats.StatBlock _statBlock = new();
    private Character.CharacterData? _charData;
    private Node3D _model = null!;
    private AnimationNodeStateMachinePlayback? _moveSm;
    private AnimationTree? _animTree;
    private AnimationPlayer? _animPlayer;

    public bool  GodMode       { get; set; }
    public float CurrentHealth { get; private set; }
    public float CurrentFocus  { get; private set; }
    public float MaxFocus      { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNextLevel { get; private set; } = 20;

    public EnemyController? LockedTarget  { get; private set; }
    public Vector3          TargetPosition { get; private set; }

    private float _focusRegen;
    private float _totalReserved;
    private float _currentFocusShield;
    private float _maxFocusShield;

    private float _yaw;
    private const float RotationSpeed = 20f;

    private MeshInstance3D? _rangeIndicator;

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
            MaxFocus           = _statBlock.Get(Stats.StatId.MaxFocus);
            _focusRegen        = _statBlock.Get(Stats.StatId.FocusRegen);
            CurrentFocus       = MaxFocus;
            _totalReserved     = 0f;

            _maxFocusShield     = MaxFocus * BalanceConfig.Focus.ShieldFraction;
            _currentFocusShield = _maxFocusShield;

            Level         = c.CurrentLevel;
            CurrentXp     = c.CurrentXp;
            XpToNextLevel = ComputeXpToNextLevel(Level);

            var weapon = GetEquippedItem(c, Items.ItemSlot.Weapon);
            var hat    = GetEquippedItem(c, Items.ItemSlot.Hat);
            var body   = GetEquippedItem(c, Items.ItemSlot.Body);

            DamageReduction = (hat?.DamageReduction ?? 0f) + (body?.DamageReduction ?? 0f);

            var weaponController = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            ApplyWeaponDamage(weaponController, weapon);
            RecalculateEffectiveRange();
            weaponController?.SetPreferredDelivery(weapon?.PreferredDelivery ?? "Melee");

            for (int i = 0; i < 3 && i < c.SlottedSkillInstanceIds.Count; i++)
            {
                var instanceId = c.SlottedSkillInstanceIds[i];
                if (string.IsNullOrEmpty(instanceId)) continue;
                var instance = manager.FindSkillInstance(instanceId);
                var skill    = instance?.Definition;
                if (skill == null) continue;

                var eotIds   = new List<string>();
                bool  hasSplash = false, hasPierce = false, hasMagicDamage = false;
                float critChanceBonus = 0f;
                if (instance != null)
                {
                    var activeAugments = Skills.AugmentResolver.Resolve(instance.SocketedSkillAugmentIds, manager.FindSkillAugmentInstance);
                    foreach (var augInst in activeAugments)
                    {
                        if (augInst.DefinitionId == "splash")          hasSplash      = true;
                        if (augInst.DefinitionId == "pierce")          hasPierce      = true;
                        if (augInst.DefinitionId == "magic_damage")    hasMagicDamage = true;
                        if (augInst.DefinitionId == "critical_strike") critChanceBonus += BalanceConfig.SkillAugments.CritChance;
                        var eotId = augInst.Definition?.EotId;
                        if (eotId != null) eotIds.Add(eotId);
                    }
                }

                weaponController?.SetSlot(i, skill, eotIds, hasSplash, hasPierce, hasMagicDamage, critChanceBonus);
                bool autoActivate = i < c.SlotAutoActivate.Count ? c.SlotAutoActivate[i] : true;
                weaponController?.SetSlotAutoActivate(i, autoActivate);
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
            wc?.SetBaseDamageType(Items.DamageType.Physical);
            wc?.SetGlobalCritChance(0f);
            wc?.SetCritMultiplier(BalanceConfig.SkillAugments.CritMultiplier);
            wc?.SetRange(1.5f * GameScale.TileSize);
            wc?.SetPreferredDelivery("Melee");
            var fallback = SkillRegistry.Get("entity_burst");
            if (fallback != null) wc?.SetSlot(0, fallback);

            MaxFocus            = BalanceConfig.Focus.WarriorMaxFocus;
            _focusRegen         = BalanceConfig.Focus.WarriorRegenPerSec;
            CurrentFocus        = MaxFocus;
            _maxFocusShield     = MaxFocus * BalanceConfig.Focus.ShieldFraction;
            _currentFocusShield = _maxFocusShield;
        }

        _mendingTimer = 3.0f;

        GlobalPosition = Vector3.Zero;
        CurrentHealth  = MaxHealth;
        AddToGroup("player");

        var visuals = new Node3D();
        visuals.Scale = new Vector3(9f, 9f, 9f);
        AddChild(visuals);
        _model = visuals;

        string modelPath = "res://assets/models/characters/player.glb";
        var playerModel = GD.Load<PackedScene>(modelPath).Instantiate<Node3D>();
        visuals.AddChild(playerModel);

        var animPlayer = playerModel.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (animPlayer == null)
        {
            animPlayer = new AnimationPlayer();
            playerModel.AddChild(animPlayer);
        }

        if (animPlayer.HasAnimation("breathing_idle"))
            animPlayer.GetAnimation("breathing_idle").LoopMode = Animation.LoopModeEnum.Linear;
        if (animPlayer.HasAnimation("run"))
        {
            var runAnim = animPlayer.GetAnimation("run");
            runAnim.LoopMode = Animation.LoopModeEnum.Linear;
            // Strip root motion: zero X/Z on Hips position track so the animation plays in place
            for (int i = 0; i < runAnim.GetTrackCount(); i++)
            {
                if (runAnim.TrackGetPath(i).ToString().Contains("Hips") &&
                    runAnim.TrackGetType(i) == Animation.TrackType.Position3D)
                {
                    for (int k = 0; k < runAnim.TrackGetKeyCount(i); k++)
                    {
                        var pos = (Vector3)runAnim.TrackGetKeyValue(i, k);
                        runAnim.TrackSetKeyValue(i, k, new Vector3(0f, pos.Y, 0f));
                    }
                }
            }
        }

        _animPlayer = animPlayer;

        var animTree = GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree != null)
        {
            animTree.AnimPlayer = animTree.GetPathTo(animPlayer);
            animTree.Active = true;
            _animTree = animTree;
        }

        var skeleton = playerModel.FindChild("Skeleton3D", true, false) as Skeleton3D;
        if (skeleton != null)
        {
            string? weaponDefId = _charData != null
                ? GetEquippedItem(_charData, Items.ItemSlot.Weapon)?.Id
                : "sword_t1";
            string? weaponPath = GetWeaponModelPath(weaponDefId);
            if (weaponPath != null)
            {
                var weaponScene = GD.Load<PackedScene>(weaponPath);
                if (weaponScene != null)
                {
                    var weaponRoot = weaponScene.Instantiate<Node3D>();
                    visuals.AddChild(weaponRoot);
                    string attachBone = weaponDefId == "bow_t1" ? "Hand_L" : "Hand_R";
                    AttachWeaponToSkeleton(weaponRoot, skeleton, attachBone);
                }
            }
        }

        GetNodeOrNull<Weapon.WeaponController>("Weapon")?.Connect(
            Weapon.WeaponController.SignalName.SkillFired,
            Callable.From<int, float, string>(OnSkillFired));


        float indicatorRadius = EffectiveRange > 0f ? EffectiveRange : 1.5f * GameScale.TileSize;
        _rangeIndicator = CreateRangeIndicator(indicatorRadius);
        AddChild(_rangeIndicator);
        _rangeIndicator.Visible = false;

        if (TargetIndicatorScene != null)
        {
            _targetIndicator = TargetIndicatorScene.Instantiate<Node3D>();
            _targetIndicator.Visible = false;
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, _targetIndicator);
        }

        _aimReticle = CreateAimReticle();
        _aimReticle.Visible = false;
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, _aimReticle);
    }

    public void SetRangeIndicatorVisible(bool visible)
    {
        if (_rangeIndicator != null)
            _rangeIndicator.Visible = visible;
    }

    public void RecalculateEffectiveRange()
    {
        var weapon = _charData != null ? GetEquippedItem(_charData, Items.ItemSlot.Weapon) : null;
        var hat    = _charData != null ? GetEquippedItem(_charData, Items.ItemSlot.Hat)    : null;
        var body   = _charData != null ? GetEquippedItem(_charData, Items.ItemSlot.Body)   : null;

        float weaponRange = weapon?.WeaponRange ?? 1.5f;
        EffectiveRange = weapon?.PreferredDelivery != "Melee"
            ? (weaponRange + (hat?.RangeModifier ?? 0f) + (body?.RangeModifier ?? 0f) + _rangeBuffBonus) * GameScale.TileSize
            : weaponRange * GameScale.TileSize;

        GetNodeOrNull<Weapon.WeaponController>("Weapon")?.SetRange(EffectiveRange);
    }

    public void AddRangeBuffBonus(float tiles)
    {
        _rangeBuffBonus += tiles;
        RecalculateEffectiveRange();
    }

    public void RemoveRangeBuffBonus(float tiles)
    {
        _rangeBuffBonus -= tiles;
        RecalculateEffectiveRange();
    }

    private static Node3D CreateAimReticle()
    {
        var root  = new Node3D();
        var torus = new TorusMesh { OuterRadius = 12f, InnerRadius = 8f, Rings = 32, RingSegments = 8 };
        var mat   = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1f, 1f, 1f, 0.9f),
        };
        var mesh = new MeshInstance3D { Mesh = torus, MaterialOverride = mat };
        mesh.RotateX(Mathf.Pi / 2f);
        root.AddChild(mesh);
        return root;
    }

    private static MeshInstance3D CreateRangeIndicator(float radius)
    {
        var torus = new TorusMesh { OuterRadius = radius, InnerRadius = 1.5f, Rings = 64, RingSegments = 8 };
        var mat = new StandardMaterial3D
        {
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor  = new Color(0f, 0.8f, 1f, 0.5f),
            NoDepthTest  = true,
        };
        return new MeshInstance3D { Mesh = torus, MaterialOverride = mat, Position = new Vector3(0f, 0.5f, 0f) };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_moveSm == null)
        {
            var at = GetNodeOrNull<AnimationTree>("AnimationTree");
            if (at != null)
                _moveSm = at.Get("parameters/movement/playback").As<AnimationNodeStateMachinePlayback>();
        }

        var input     = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var direction = new Vector3(input.X, 0f, input.Y);

        float moveSpeed = Speed + (_dashReflexTimer > 0f ? 100f : 0f);
        Velocity = direction * moveSpeed;
        MoveAndSlide();

        UpdateLockedTarget();

        var camera = GetViewport().GetCamera3D();
        if (camera != null)
        {
            var mousePos = GetViewport().GetMousePosition();
            var rayFrom  = camera.ProjectRayOrigin(mousePos);
            var rayDir   = camera.ProjectRayNormal(mousePos);
            if (Mathf.Abs(rayDir.Y) > 0.001f)
            {
                float t = -rayFrom.Y / rayDir.Y;
                TargetPosition = rayFrom + rayDir * t;
            }
        }

        if (_targetIndicator != null)
        {
            bool hasTarget = LockedTarget != null && GodotObject.IsInstanceValid(LockedTarget);
            _targetIndicator.Visible = hasTarget;
            if (hasTarget)
                _targetIndicator.GlobalPosition = new Vector3(LockedTarget!.GlobalPosition.X, 1f, LockedTarget!.GlobalPosition.Z);
        }

        if (_aimReticle != null)
        {
            var wc = GetNodeOrNull<Weapon.WeaponController>("Weapon");
            bool show = wc?.HasAnyPositionSkill() ?? false;
            _aimReticle.Visible = show;
            if (show)
                _aimReticle.GlobalPosition = new Vector3(TargetPosition.X, 0.5f, TargetPosition.Z);
        }

        bool moving = direction.LengthSquared() > 0.01f;
        bool inAttack = _animTree != null &&
            (((bool)_animTree.Get("parameters/shot_right/active")) ||
             ((bool)_animTree.Get("parameters/shot_left/active")));

        if (moving && !inAttack)
        {
            float targetYaw = Mathf.Atan2(direction.X, direction.Z);
            _yaw = Mathf.LerpAngle(_yaw, targetYaw, Mathf.Min(1f, RotationSpeed * (float)delta));
            _model.Rotation = new Vector3(0f, _yaw, 0f);
        }
        else
        {
            var toAim = new Vector3(TargetPosition.X - GlobalPosition.X, 0f, TargetPosition.Z - GlobalPosition.Z);
            if (toAim.LengthSquared() > 1f)
            {
                float targetYaw = Mathf.Atan2(toAim.X, toAim.Z);
                _yaw = Mathf.LerpAngle(_yaw, targetYaw, Mathf.Min(1f, RotationSpeed * (float)delta));
                _model.Rotation = new Vector3(0f, _yaw, 0f);
            }
        }

        if (_moveSm != null)
        {
            var want = moving ? "run" : "idle";
            if (_moveSm.GetCurrentNode() != want)
                _moveSm.Travel(want);
        }

        float dt = (float)delta;

        CurrentFocus = Mathf.Min(CurrentFocus + _focusRegen * dt, MaxFocus);
        EmitSignal(SignalName.FocusChanged, GetAvailableFocus(), MaxFocus);

        if (_currentFocusShield < _maxFocusShield)
            _currentFocusShield = Mathf.Min(_currentFocusShield + BalanceConfig.Focus.ShieldRegenPerSec * dt, _maxFocusShield);
        EmitSignal(SignalName.ShieldChanged, _currentFocusShield, _maxFocusShield);

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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || key.Echo) return;
        var wc = GetNodeOrNull<Weapon.WeaponController>("Weapon");
        if (key.Pressed)
        {
            if      (key.Keycode == Key.Key1) wc?.TryFireSlot(0);
            else if (key.Keycode == Key.Key2) wc?.TryFireSlot(1);
            else if (key.Keycode == Key.Key3) wc?.TryFireSlot(2);
        }
        else
        {
            if      (key.Keycode == Key.Key1) wc?.ReleaseSlot(0);
            else if (key.Keycode == Key.Key2) wc?.ReleaseSlot(1);
            else if (key.Keycode == Key.Key3) wc?.ReleaseSlot(2);
        }
    }

    public void TakeDamage(float rawAmount, Items.DamageType type, Node3D? attacker = null)
    {
        if (GodMode) return;
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

        float damageToShow = effective;

        // Focus Shield absorbs damage before HP
        if (_currentFocusShield > 0f)
        {
            float absorbed = Mathf.Min(_currentFocusShield, effective);
            _currentFocusShield -= absorbed;
            effective           -= absorbed;
            EmitSignal(SignalName.ShieldChanged, _currentFocusShield, _maxFocusShield);
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - effective);
        EmitSignal(SignalName.HealthChanged, CurrentHealth);

        if (damageToShow > 0f)
            EmitSignal(SignalName.DamageTaken, damageToShow, type == Items.DamageType.Magic);

        if (effective > 0f)
        {
            EmitSignal(SignalName.PlayerHit);
            Engine.TimeScale = 0f;
            GetTree().CreateTimer(0.05f, true, false, true).Timeout += () => Engine.TimeScale = 1f;
        }

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
                var wc     = GetNodeOrNull<Weapon.WeaponController>("Weapon");
                var weapon = GetEquippedItem(_charData, Items.ItemSlot.Weapon);
                ApplyWeaponDamage(wc, weapon);
            }

            MaxHealth     = (int)_statBlock.Get(Stats.StatId.MaxHp);
            CurrentHealth = Mathf.Min(CurrentHealth + 5f, MaxHealth);
            EmitSignal(SignalName.LeveledUp, Level);
        }
        EmitSignal(SignalName.XpChanged, CurrentXp, XpToNextLevel);
    }

    private static string? GetWeaponModelPath(string? weaponId) => weaponId switch
    {
        "sword_t1" => "res://assets/models/equipment/weapon_sword.glb",
        "bow_t1"   => "res://assets/models/equipment/weapon_bow.glb",
        "wand_t1"  => "res://assets/models/equipment/weapon_wand.glb",
        _          => null
    };

    private static Items.ItemData? GetEquippedItem(Character.CharacterData c, Items.ItemSlot slot)
    {
        c.EquippedGear.TryGetValue(slot.ToString(), out var instance);
        return instance?.Definition;
    }

    private void OnSkillFired(int slotIndex, float cooldown, string delivery)
    {
        if (_animTree == null) return;
        var param = delivery switch
        {
            "Ranged" => "parameters/shot_left/request",
            _        => "parameters/shot_right/request",
        };
        if ((bool)_animTree.Get(param.Replace("/request", "/active"))) return;
        if (delivery != "Ranged")
        {
            const float MeleeAnimLength = 2.3f; // melee_right_atack duration in seconds
            _animTree.Set("parameters/right_ts/scale", MeleeAnimLength / cooldown);
        }
        _animTree.Set(param, (int)AnimationNodeOneShot.OneShotRequest.Fire);
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

    private static void AttachWeaponToSkeleton(Node3D weaponRoot, Skeleton3D skeleton, string boneName = "Hand_R")
    {
        int handIdx = FindBone(skeleton, boneName);
        string handBoneName = handIdx >= 0 ? skeleton.GetBoneName(handIdx) : boneName;
        var attach = new BoneAttachment3D();
        skeleton.AddChild(attach);
        attach.BoneName = handBoneName;

        var pieces = new List<(Node3D n, Vector3 pos)>();
        foreach (var child in weaponRoot.GetChildren())
            if (child is Node3D n) pieces.Add((n, n.Position));

        // Anchor: use the Handle mesh AABB centre so models with offset geometry align correctly
        Vector3 anchorPos = pieces.Count > 0 ? pieces[0].pos : Vector3.Zero;
        foreach (var (n, p) in pieces)
        {
            if (n.Name.ToString().Contains("Handle"))
            {
                anchorPos = n is MeshInstance3D mi ? p + mi.GetAabb().GetCenter() : p;
                break;
            }
        }

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

    private void UpdateLockedTarget()
    {
        // Always recompute — cursor may have moved to a closer enemy
        EnemyController? best = null;
        float bestDist = float.MaxValue;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyController enemy || enemy.IsQueuedForDeletion()) continue;
            float dist = TargetPosition.DistanceTo(enemy.GlobalPosition);
            if (dist < bestDist) { bestDist = dist; best = enemy; }
        }
        LockedTarget = best;
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

    public float GetAvailableFocus() => Mathf.Max(0f, CurrentFocus - _totalReserved);

    public bool TrySpendFocus(float amount)
    {
        if (GetAvailableFocus() < amount) return false;
        CurrentFocus -= amount;
        EmitSignal(SignalName.FocusChanged, GetAvailableFocus(), MaxFocus);
        return true;
    }

    public void ReserveFocus(float absoluteAmount)
    {
        _totalReserved += absoluteAmount;
        EmitSignal(SignalName.FocusChanged, GetAvailableFocus(), MaxFocus);
    }

    public void UnreserveFocus(float absoluteAmount)
    {
        _totalReserved = Mathf.Max(0f, _totalReserved - absoluteAmount);
        EmitSignal(SignalName.FocusChanged, GetAvailableFocus(), MaxFocus);
    }

    private void ApplyWeaponDamage(Weapon.WeaponController? wc, Items.ItemData? weapon)
    {
        if (wc == null || weapon == null) return;

        var charType = _charData?.Type ?? Character.CharacterType.Warrior;
        bool isMagicWeapon = weapon.BaseDamageType == Items.DamageType.Magic;

        float archetypeMult = isMagicWeapon
            ? charType switch
            {
                Character.CharacterType.Warrior => BalanceConfig.Archetypes.Warrior.MagicDamageMultiplier,
                Character.CharacterType.Rogue   => BalanceConfig.Archetypes.Rogue.MagicDamageMultiplier,
                Character.CharacterType.Mage    => BalanceConfig.Archetypes.Mage.MagicDamageMultiplier,
                _                               => 1.0f,
            }
            : charType switch
            {
                Character.CharacterType.Warrior => BalanceConfig.Archetypes.Warrior.PhysicalDamageMultiplier,
                Character.CharacterType.Rogue   => BalanceConfig.Archetypes.Rogue.PhysicalDamageMultiplier,
                Character.CharacterType.Mage    => BalanceConfig.Archetypes.Mage.PhysicalDamageMultiplier,
                _                               => 1.0f,
            };

        float levelBonus = 1f + (Level - 1) * BalanceConfig.LevelUp.DamageBonusPerLevel;
        float weaponDmg  = weapon.BaseDamage * archetypeMult * levelBonus * (1f + weapon.DamageBonus);

        float physDmg  = isMagicWeapon ? 0f : weaponDmg;
        float magicDmg = isMagicWeapon ? weaponDmg : 0f;

        _statBlock.SetBase(Stats.StatId.PhysicalDamage, physDmg);
        _statBlock.SetBase(Stats.StatId.MagicDamage,    magicDmg);

        wc.SetDamage(physDmg, magicDmg);
        wc.SetBaseDamageType(weapon.BaseDamageType);
        wc.SetGlobalCritChance(weapon.CritChanceBonus);
        wc.SetCritMultiplier(BalanceConfig.SkillAugments.CritMultiplier);
    }
}
