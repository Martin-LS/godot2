namespace Godot1.Skills;

public record SkillData(
    string    Id,
    string    Name,
    SkillType Type,
    string[]  Tags,
    float     Cooldown,
    float     Range,
    string    IconPath = ""
);
