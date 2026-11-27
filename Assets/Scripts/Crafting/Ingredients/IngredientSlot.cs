using System.Collections.Generic;
using TMPro;
using UnityEngine;

// El lugar de un ingrediente en la mesa: muestra cuánto hay en la despensa y deja sacarlo
public class IngredientSlot : MonoBehaviour, IDragSource, ICraftingPart
{
    [SerializeField] private ItemData item;
    [SerializeField] private ItemState state;

    [Header("Aspecto")]
    [Tooltip("Medida del lado más largo de cada modelo")]
    [SerializeField, Min(0.05f)] private float itemSize = 0.6f;
    [Tooltip("Dónde se apoya cada unidad visible. Su cantidad es el máximo de modelos")]
    [SerializeField] private Vector3[] places = { Vector3.zero };
    [SerializeField] private TextMeshPro countLabel;
    [Tooltip("Material del modelo atenuado que queda cuando no hay stock")]
    [SerializeField] private Material emptyMaterial;

    private static readonly Color Available = Color.white;
    private static readonly Color Missing = new Color(1f, 1f, 1f, 0.4f);

    private readonly List<Transform> visuals = new List<Transform>();
    private readonly List<InteractableOutline> outlines = new List<InteractableOutline>();
    private Transform emptyVisual;
    private Transform dragged;
    private CraftingService service;

    public ItemKey Key => new ItemKey(item, state);
    public bool CanDrag => Count > 0;
    public string HoverText => $"{Key.DisplayName}  x{Count}";

    // Contornos de los modelos que se están viendo (para que el tutorial los haga titilar)
    public IReadOnlyList<InteractableOutline> Outlines => outlines;

    private int Count => service != null ? service.Pantry.Count(Key) : 0;

    public void Bind(CraftingService craftingService)
    {
        service = craftingService;
        service.Pantry.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (service != null) service.Pantry.OnChanged -= Refresh;
    }

    public void Click() => service.AddToCauldron(Key);

    public void SetHovered(bool hovered)
    {
        foreach (InteractableOutline outline in outlines)
        {
            if (outline != null) outline.SetHighlighted(hovered);
        }
    }

    public Transform BeginDrag()
    {
        int index = Mathf.Min(Count, visuals.Count) - 1;
        if (index < 0) return null;

        dragged = visuals[index];
        visuals.RemoveAt(index);
        dragged.SetParent(null, true);
        Refresh();
        return dragged;
    }

    public void EndDrag()
    {
        if (dragged == null) return;

        dragged.SetParent(transform, false);
        dragged.localRotation = Quaternion.identity;
        dragged.localScale = Vector3.one;
        visuals.Add(dragged);
        dragged = null;
        Refresh();
    }

    private void Refresh()
    {
        if (!Key.IsValid) return;

        int count = Count;
        int onShelf = dragged != null ? count - 1 : count;
        int shown = Mathf.Clamp(onShelf, 0, places.Length);

        while (visuals.Count < shown)
        {
            Transform visual = CraftingModels.Create(Key, transform, itemSize);
            if (visual == null) break;
            visuals.Add(visual);
        }

        outlines.Clear();

        for (int i = 0; i < visuals.Count; i++)
        {
            bool visible = i < shown;
            visuals[i].gameObject.SetActive(visible);
            if (!visible) continue;

            visuals[i].localPosition = places[i];
            InteractableOutline outline = visuals[i].GetComponentInChildren<InteractableOutline>();
            if (outline != null) outlines.Add(outline);
        }

        ShowEmpty(count == 0);

        if (countLabel != null)
        {
            countLabel.text = count.ToString();
            countLabel.color = count > 0 ? Available : Missing;
        }
    }

    private void ShowEmpty(bool show)
    {
        if (show && emptyVisual == null && emptyMaterial != null)
        {
            emptyVisual = CraftingModels.Create(Key, transform, itemSize);
            if (emptyVisual == null) return;

            emptyVisual.localPosition = places.Length > 0 ? places[0] : Vector3.zero;
            Dim(emptyVisual);
        }

        if (emptyVisual != null) emptyVisual.gameObject.SetActive(show);
    }

    private void Dim(Transform visual)
    {
        foreach (InteractableOutline outline in visual.GetComponentsInChildren<InteractableOutline>(true))
            outline.enabled = false;

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer)
            {
                renderer.enabled = false;
                continue;
            }

            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = emptyMaterial;

            renderer.sharedMaterials = materials;
        }

        foreach (Light itemLight in visual.GetComponentsInChildren<Light>(true))
            itemLight.enabled = false;

        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        foreach (Vector3 place in places)
            Gizmos.DrawWireCube(place + Vector3.up * itemSize * 0.5f, Vector3.one * itemSize);

        Gizmos.matrix = Matrix4x4.identity;
    }
}
