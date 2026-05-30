using System.Collections.Generic;

namespace Godot1.Crafting;

public record RecipeData(
    string                  Id,
    string                  OutputItemId,
    Dictionary<string, int> MaterialCosts
);
