using System.Collections.Generic;
using UnityEngine;

// Muestra lo que hay en el caldero y recibe los ingredientes que se le sueltan
public class CauldronView : MonoBehaviour, IPointerTarget, IDropTarget, ICraftingPart
{
    [SerializeField] private InteractableOutline outline;

    [Header("Ingredientes flotando")]
    [Tooltip("Centro de la boca del caldero")]
    [SerializeField] private Transform contentRoot;
    [SerializeField, Min(0f)] private float radius = 0.45f;
    [SerializeField, Min(0.05f)] private float itemSize = 0.34f;
    [SerializeField, Min(0f)] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2f;

    // Ángulo áureo: reparte los ítems sin que se encimen, sean cuantos sean
    private const float SpiralAngle = 137.5f;

    private readonly List<CauldronItem> items = new List<CauldronItem>();
    private readonly List<Vector3> places = new List<Vector3>();
    private CraftingService service;

    public InteractableOutline Outline => outline;
    public string HoverText => null;

    public void Bind(CraftingService craftingService)
    {
        service = craftingService;
        service.Cauldron.OnChanged += Sync;
        Sync();
    }

    private void OnDestroy()
    {
        if (service != null) service.Cauldron.OnChanged -= Sync;
    }

    public void SetHovered(bool hovered) { }

    public void Drop(ItemKey key) => service.AddToCauldron(key);

    public void TakeOut(CauldronItem item)
    {
        ItemKey key = item.Key;
        Remove(item);

        if (!service.RemoveFromCauldron(key))
            Sync();
    }

    // Contornos de los ítems de un tipo (para que el tutorial los haga titilar)
    public IEnumerable<InteractableOutline> OutlinesOf(ItemKey key)
    {
        foreach (CauldronItem item in items)
        {
            if (item.Key == key && item.Outline != null)
                yield return item.Outline;
        }
    }

    // Ajusta los modelos a lo que dice el contenedor
    private void Sync()
    {
        Dictionary<ItemKey, int> pending = new Dictionary<ItemKey, int>();
        foreach (KeyValuePair<ItemKey, int> pair in service.Cauldron.Contents)
            pending[pair.Key] = pair.Value;

        for (int i = 0; i < items.Count; i++)
        {
            ItemKey key = items[i].Key;

            if (pending.TryGetValue(key, out int left) && left > 0)
                pending[key] = left - 1;
            else
                Remove(items[i--]);
        }

        foreach (KeyValuePair<ItemKey, int> pair in pending)
        {
            for (int i = 0; i < pair.Value; i++)
                Add(pair.Key);
        }

        Arrange();
    }

    private void Add(ItemKey key)
    {
        Transform visual = CraftingModels.Create(key, contentRoot, itemSize);
        if (visual == null) return;

        CauldronItem item = visual.gameObject.AddComponent<CauldronItem>();
        item.Init(key, this, itemSize);
        items.Add(item);
    }

    private void Remove(CauldronItem item)
    {
        items.Remove(item);
        item.gameObject.SetActive(false);
        Destroy(item.gameObject);
    }

    private void Arrange()
    {
        places.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            float distance = items.Count > 1 ? radius * Mathf.Sqrt((i + 0.5f) / items.Count) : 0f;
            places.Add(Quaternion.Euler(0f, i * SpiralAngle, 0f) * Vector3.forward * distance);
        }
    }

    private void Update()
    {
        for (int i = 0; i < items.Count && i < places.Count; i++)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed + i) * bobHeight;
            items[i].transform.localPosition = places[i] + Vector3.up * bob;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (contentRoot == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(contentRoot.position, radius);
    }
}
