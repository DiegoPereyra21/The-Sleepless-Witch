using System.Collections.Generic;

// Las recetas con sus requisitos ya armados, para no rehacerlos en cada intento
public class RecipeBook
{
    private readonly struct Recipe
    {
        public readonly PotionData Potion;
        public readonly Dictionary<ItemKey, int> Requirements;

        public Recipe(PotionData potion, Dictionary<ItemKey, int> requirements)
        {
            Potion = potion;
            Requirements = requirements;
        }
    }

    private readonly List<Recipe> recipes = new List<Recipe>();

    public RecipeBook(IEnumerable<PotionData> potions)
    {
        foreach (PotionData potion in potions)
        {
            if (potion == null) continue;

            Dictionary<ItemKey, int> requirements = potion.GetRequirements();
            if (requirements.Count > 0)
                recipes.Add(new Recipe(potion, requirements));
        }
    }

    // Coincidencia exacta: mismos ítems, mismos estados, mismas cantidades, nada de más
    public PotionData Match(IReadOnlyDictionary<ItemKey, int> contents)
    {
        foreach (Recipe recipe in recipes)
        {
            if (Matches(recipe.Requirements, contents))
                return recipe.Potion;
        }

        return null;
    }

    private static bool Matches(Dictionary<ItemKey, int> required, IReadOnlyDictionary<ItemKey, int> contents)
    {
        if (required.Count != contents.Count) return false;

        foreach (KeyValuePair<ItemKey, int> pair in contents)
        {
            if (!required.TryGetValue(pair.Key, out int needed) || needed != pair.Value)
                return false;
        }

        return true;
    }
}
