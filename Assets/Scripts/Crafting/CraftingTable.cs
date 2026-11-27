using System;
using UnityEngine;

// Raíz de la mesa: crea las reglas del crafteo, conecta sus piezas y permite encontrarlas.
// Para sumar un ingrediente alcanza con agregar otro IngredientSlot como hijo.
public class CraftingTable : MonoBehaviour
{
    [SerializeField] private CauldronView cauldron;
    [SerializeField] private CauldronLadle ladle;
    [SerializeField] private CauldronBrewer brewer;

    // Se avisa cuando termina la animación de la poción creada
    public event Action<PotionData> PotionCrafted;

    public CraftingService Service { get; private set; }
    public bool IsBusy => Service != null && Service.IsBusy;
    public CauldronView Cauldron => cauldron;
    public CauldronLadle Ladle => ladle;

    private IngredientSlot[] slots;
    private ProcessingStation[] stations;

    private void Awake()
    {
        slots = GetComponentsInChildren<IngredientSlot>(true);
        stations = GetComponentsInChildren<ProcessingStation>(true);
    }

    private void OnEnable() => brewer.PotionCrafted += RaisePotionCrafted;
    private void OnDisable() => brewer.PotionCrafted -= RaisePotionCrafted;

    private void Start()
    {
        Service = new CraftingService(HomeStorage.Instance, new RecipeBook(GameDatabase.Instance.Potions));

        foreach (ICraftingPart part in GetComponentsInChildren<ICraftingPart>(true))
            part.Bind(Service);

        WarnMissingSlots();
    }

    public IngredientSlot FindSlot(ItemKey key)
    {
        foreach (IngredientSlot slot in slots)
        {
            if (slot.Key == key) return slot;
        }

        return null;
    }

    public ProcessingStation FindStation(TableType table)
    {
        foreach (ProcessingStation station in stations)
        {
            if (station.Table == table) return station;
        }

        return null;
    }

    private void RaisePotionCrafted(PotionData potion) => PotionCrafted?.Invoke(potion);

    private void WarnMissingSlots()
    {
        foreach (ItemData item in GameDatabase.Instance.Items)
        {
            if (item == null) continue;

            if (FindSlot(ItemKey.Raw(item)) == null)
                Debug.LogWarning($"[CraftingTable] '{item.name}' no tiene lugar en la mesa.", this);

            if (item.Processable && FindSlot(ItemKey.Processed(item)) == null)
                Debug.LogWarning($"[CraftingTable] '{item.name}' procesado no tiene lugar en la mesa.", this);
        }
    }
}
