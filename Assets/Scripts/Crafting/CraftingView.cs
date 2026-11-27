using UnityEngine;

// Qué se ve mientras se craftea y cómo moverse por la mesa. CraftingRoom solo la prende y la apaga.
// Tal cual está es una sola vista fija con todo a la vista; CraftingCameraRail le agrega las vistas laterales.
public class CraftingView : MonoBehaviour
{
    [SerializeField] protected CraftingRoom room;
    [SerializeField] private CameraTransition cameraTransition;
    [Tooltip("Vista con la que se entra a la mesa (la del caldero)")]
    [SerializeField] private Transform mainAnchor;

    public bool IsActive { get; private set; }
    public bool IsBusy => room.IsBusy;
    public Transform MainAnchor => mainAnchor;

    public virtual void Activate()
    {
        IsActive = true;
        Show(mainAnchor);
    }

    public virtual void Deactivate() => IsActive = false;

    protected void Show(Transform anchor) => cameraTransition.TransitionTo(anchor);

    // ---------- Controles propios de la vista ----------

    // true mientras el mouse está sobre un botón de la vista: ese click no es para la mesa
    public virtual bool IsPointerOverUI => false;

    // ---------- Ayudas para el tutorial ----------

    // Cómo moverse por la mesa (vacío si no hace falta)
    public virtual string ControlsHint => string.Empty;
    // Cómo llegar a una mesa que no está en pantalla (vacío si siempre se ve)
    public virtual string DirectionsTo(TableType table) => string.Empty;
    // Hace titilar el control que lleva a esa mesa (null = ninguno)
    public virtual void PointTo(TableType? table) { }
}
