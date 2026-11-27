using UnityEngine;

// Contratos entre el mouse y las cosas de la mesa: el puntero no sabe qué objeto está tocando

// Algo que el mouse puede señalar
public interface IPointerTarget
{
    // Texto del cartel que acompaña al mouse (null = sin cartel)
    string HoverText { get; }
    void SetHovered(bool hovered);
}

public interface IClickTarget : IPointerTarget
{
    void Click();
}

// Algo de lo que se puede sacar un ingrediente arrastrando. Un click sin arrastrar llama a Click.
public interface IDragSource : IClickTarget
{
    ItemKey Key { get; }
    bool CanDrag { get; }
    // Entrega el modelo que va a seguir al mouse
    Transform BeginDrag();
    void EndDrag();
}

// Donde se puede soltar un ingrediente arrastrado
public interface IDropTarget
{
    void Drop(ItemKey key);
}
