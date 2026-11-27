using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Único lector del mouse en la mesa: señalar, click y arrastrar
public class CraftingPointer : MonoBehaviour
{
    [SerializeField] private CraftingRoom room;
    [SerializeField] private LayerMask craftingLayer;
    [SerializeField] private float maxDistance = 100f;
    [Tooltip("Píxeles que se puede mover el mouse y que siga contando como click.")]
    [SerializeField] private float clickThreshold = 15f;
    [Tooltip("A qué fracción de su distancia a la cámara viaja lo que se arrastra, para que no atraviese la mesa")]
    [SerializeField, Range(0.3f, 1f)] private float dragDepth = 0.72f;

    public event Action<string> HoverTextChanged;

    // La interfaz lo prende mientras el mouse está sobre un panel: ese click no es para la mesa
    public bool IsOverUI { get; set; }
    public bool IsDragging => source != null;

    private IPointerTarget hovered;
    private string hoverText;

    private IDragSource source;
    private Transform dragged;
    private Vector2 dragStart;
    private float dragDistance;

    private void Update()
    {
        Mouse mouse = Mouse.current;

        // Afuera de la mesa, en pausa, con el libro abierto o durante una animación no se toca nada
        if (!room.IsInside || room.IsBusy || Time.timeScale <= 0f || mouse == null)
        {
            Release();
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (IsDragging)
        {
            FollowMouse(screenPosition);
            if (mouse.leftButton.wasReleasedThisFrame)
                EndDrag(screenPosition);
            return;
        }

        IPointerTarget target = IsOverUI ? null : Find<IPointerTarget>(screenPosition);
        SetHovered(target);

        if (!mouse.leftButton.wasPressedThisFrame || target == null) return;

        if (target is IDragSource dragSource && dragSource.CanDrag)
            BeginDrag(dragSource, screenPosition);
        else if (target is IClickTarget clickTarget)
            clickTarget.Click();
    }

    private void OnDisable() => Release();

    // Suelta lo que se esté arrastrando y deja de señalar
    public void Release()
    {
        if (IsDragging)
        {
            source.EndDrag();
            source = null;
            dragged = null;
        }

        SetHovered(null);
    }

    private void BeginDrag(IDragSource dragSource, Vector2 screenPosition)
    {
        SetHovered(null);

        Transform visual = dragSource.BeginDrag();
        if (visual == null) return;

        source = dragSource;
        dragged = visual;
        dragStart = screenPosition;
        dragDistance = Vector3.Distance(Camera.main.transform.position, dragged.position) * dragDepth;
        dragged.localScale *= dragDepth; // se acerca a la cámara sin verse más grande
        FollowMouse(screenPosition);
    }

    private void FollowMouse(Vector2 screenPosition)
    {
        if (dragged != null)
            dragged.position = Camera.main.ScreenPointToRay(screenPosition).GetPoint(dragDistance);
    }

    private void EndDrag(Vector2 screenPosition)
    {
        IDragSource finished = source;
        ItemKey key = finished.Key;
        bool wasClick = Vector2.Distance(screenPosition, dragStart) < clickThreshold;
        IDropTarget dropTarget = wasClick ? null : Find<IDropTarget>(screenPosition);

        source = null;
        dragged = null;
        finished.EndDrag();

        if (wasClick) finished.Click();
        else dropTarget?.Drop(key);
    }

    private T Find<T>(Vector2 screenPosition) where T : class
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, craftingLayer, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<T>();
    }

    private void SetHovered(IPointerTarget target)
    {
        // Lo señalado puede haberse destruido (por ejemplo, un ítem sacado del caldero)
        if (hovered is UnityEngine.Object old && old == null) hovered = null;

        if (target != hovered)
        {
            hovered?.SetHovered(false);
            hovered = target;
            hovered?.SetHovered(true);
        }

        string text = hovered?.HoverText;
        if (text == hoverText) return;

        hoverText = text;
        HoverTextChanged?.Invoke(text);
    }
}
