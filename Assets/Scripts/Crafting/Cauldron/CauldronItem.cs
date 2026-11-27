using UnityEngine;

// Un ingrediente flotando sobre el caldero: se saca con un click
public class CauldronItem : MonoBehaviour, IClickTarget
{
    private CauldronView cauldron;
    private InteractableOutline outline;

    public ItemKey Key { get; private set; }
    public InteractableOutline Outline => outline;
    public string HoverText => GameText.Pick($"Sacar {Key.DisplayName}", $"Take out {Key.DisplayName}");

    public void Init(ItemKey key, CauldronView owner, float size)
    {
        Key = key;
        cauldron = owner;
        outline = GetComponentInChildren<InteractableOutline>();

        SphereCollider area = gameObject.AddComponent<SphereCollider>();
        area.center = Vector3.up * (size * 0.5f);
        area.radius = size * 0.5f;
    }

    public void Click() => cauldron.TakeOut(this);

    public void SetHovered(bool hovered)
    {
        if (outline != null) outline.SetHighlighted(hovered);
    }
}
