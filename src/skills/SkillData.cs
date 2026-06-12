namespace Godot1.Skills;

public record SkillData(
    string               Id,
    string               Name,
    SkillType            Type,
    string[]             Tags,
    float                Cooldown,
    float                Range,
    float                FocusCost          = 0f,
    string               IconPath           = "",
    string               Description        = "",
    bool                 IsPrototype        = false,
    SkillTargetingShape  TargetingShape     = SkillTargetingShape.Self,
    float                WindUp             = 0f,
    SkillDamagePattern   DamagePattern      = SkillDamagePattern.Burst,
    int                  StackLimit         = -1,
    bool                 ZoneTracksEntity   = false,
    float                Duration          = 0f,
    float                ZoneRadius        = 0f,
    float                TriggerRadius     = 0f,
    float                ArmTime           = 0f,
    int                  TriggerCount      = 0,
    string[]             InherentEotIds    = null!
);
