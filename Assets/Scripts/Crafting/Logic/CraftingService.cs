using System;

public enum ProcessCheck
{
    Ok,
    WrongStation,
    NotProcessable,
    Unavailable
}

public enum BrewFailure
{
    EmptyCauldron,
    NoRecipe
}

// Reglas del crafteo sobre lo guardado en la casa. No conoce nada de la escena ni de la interfaz.
// Es el único que modifica la despensa y el caldero desde la mesa, y guarda después de cada cambio.
public class CraftingService
{
    private readonly HomeStorage storage;
    private readonly RecipeBook recipes;
    private int busyCount;

    public CraftingService(HomeStorage storage, RecipeBook recipes)
    {
        this.storage = storage;
        this.recipes = recipes;
    }

    public event Action<ProcessCheck, TableType> ProcessRejected;
    public event Action<BrewFailure> BrewFailed;
    public event Action<PotionData> PotionBrewed;

    public ItemContainer Pantry => storage.Pantry;
    public ItemContainer Cauldron => storage.Cauldron;

    // Mientras hay una animación en curso la mesa no acepta otra acción
    public bool IsBusy => busyCount > 0;
    public void BeginBusy() => busyCount++;
    public void EndBusy() => busyCount = Math.Max(0, busyCount - 1);

    // ---------- Caldero ----------

    public bool AddToCauldron(ItemKey key) => Move(key, Pantry, Cauldron);

    public bool RemoveFromCauldron(ItemKey key) => Move(key, Cauldron, Pantry);

    // ---------- Mortero y tabla ----------

    public ProcessCheck CheckProcess(ItemKey key, TableType station)
    {
        if (!key.IsValid || key.State != ItemState.Raw) return ProcessCheck.Unavailable;
        if (key.Item.Table != station) return ProcessCheck.WrongStation;
        if (!key.Item.Processable) return ProcessCheck.NotProcessable;

        return Pantry.Has(key) ? ProcessCheck.Ok : ProcessCheck.Unavailable;
    }

    // Saca el crudo de la despensa. Hay que cerrar con FinishProcessing o CancelProcessing.
    public bool TryStartProcessing(ItemKey key, TableType station)
    {
        ProcessCheck check = CheckProcess(key, station);

        if (check != ProcessCheck.Ok)
        {
            ProcessRejected?.Invoke(check, station);
            return false;
        }

        return Pantry.TryRemove(key);
    }

    public void FinishProcessing(ItemData item)
    {
        Pantry.TryAdd(ItemKey.Processed(item));
        storage.Save();
    }

    public void CancelProcessing(ItemData item) => Pantry.TryAdd(ItemKey.Raw(item));

    // ---------- Preparar ----------

    public bool CanBrew()
    {
        if (!Cauldron.IsEmpty) return true;

        BrewFailed?.Invoke(BrewFailure.EmptyCauldron);
        return false;
    }

    // Devuelve la poción creada. Si no coincide ninguna receta, los ingredientes vuelven a la despensa.
    public PotionData Brew()
    {
        if (!CanBrew()) return null;

        PotionData potion = recipes.Match(Cauldron.Contents);

        if (potion == null)
        {
            storage.ReturnCauldronToPantry();
            storage.Save();
            BrewFailed?.Invoke(BrewFailure.NoRecipe);
            return null;
        }

        Cauldron.Clear();
        storage.AddCraftedPotion(potion);
        storage.Save();
        PotionBrewed?.Invoke(potion);
        return potion;
    }

    private bool Move(ItemKey key, ItemContainer from, ItemContainer to)
    {
        if (!from.TryRemove(key)) return false;

        to.TryAdd(key);
        storage.Save();
        return true;
    }
}
