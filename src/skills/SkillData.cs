namespace Godot1.Skills;

public record SkillData(
    string              Id,
    string              Name,
    SkillType           Type,
    Items.SkillCategory Category,
    float               Cooldown,
    float               Range
);
