using UnityEngine;

public class HoverOutline : MonoBehaviour
{
    [SerializeField] private InteractableOutline outline;
    [SerializeField] private LayerMask hoverLayer;
    [SerializeField] private float maxDistance = 100f;

    private bool isHovering;

    private void Update()
    {
        if (Camera.main == null) return;

        // Solo con el mouse libre (mesa, canasto): caminando por la casa no se resalta nada al pasar la vista
        var mouse = UnityEngine.InputSystem.Mouse.current;
        bool hitThisObject = false;

        if (mouse != null && Cursor.lockState != CursorLockMode.Locked)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            int mask = hoverLayer.value | (1 << gameObject.layer); //también su capa actual, el canasot los pasa  a basketvisual
            hitThisObject = Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask)
                && hit.collider.gameObject == gameObject;
        }

        if (hitThisObject != isHovering)
        {
            isHovering = hitThisObject;
            outline.SetHighlighted(isHovering);
        }
    }
}