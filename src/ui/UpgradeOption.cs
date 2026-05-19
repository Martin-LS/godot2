namespace Godot1.Ui;

public enum UpgradeEffect { MaxHealth, Speed, Damage, FireRate }

public class UpgradeOption
{
    public string DisplayName { get; }
    public string Description  { get; }
    public UpgradeEffect Effect { get; }
    public float Value { get; }

    public UpgradeOption(string name, string desc, UpgradeEffect effect, float value)
    {
        DisplayName = name;
        Description = desc;
        Effect      = effect;
        Value       = value;
    }
}
