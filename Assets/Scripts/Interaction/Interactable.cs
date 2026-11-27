using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Algo del mundo que se activa con la acción Interact. PlayerInteractor elige cuál:
// en 1ra persona el que se está mirando y en 3ra el más cercano. Solo ese se resalta.
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected InteractableOutline outline;

    [Header("Mirada en 1ra persona")]
    [Tooltip("Radio de una esfera que hay que mirar. En 0 se usan las mallas que se resaltan")]
    [SerializeField, Min(0f)] private float lookRadius;
    [Tooltip("Centro de esa esfera respecto de este objeto (ejes del mundo)")]
    [SerializeField] private Vector3 lookOffset;
    [Tooltip("Cuánto se agranda por cada lado la zona calculada con las mallas")]
    [SerializeField, Min(0f)] private float lookMargin;

    // Metido dentro de la zona hay que mirar hacia su centro, no de espaldas
    private const float InsideViewAngle = 60f;

    private static readonly List<Interactable> listed = new List<Interactable>();
    public static IReadOnlyList<Interactable> Listed => listed;

    // Los recolectables son cientos: no se anotan, los entrega PlayerCollector según la cercanía
    protected virtual bool IsListed => true;

    protected virtual void OnEnable()
    {
        if (IsListed) listed.Add(this);
    }

    protected virtual void OnDisable() => listed.Remove(this);

    // Texto que acompaña a la tecla en pantalla
    public abstract string Prompt { get; }
    public virtual bool IsAvailable => isActiveAndEnabled;
    public abstract bool IsInRange(Vector3 playerPosition);
    public abstract void Interact(PlayerInteractor player);

    public void SetFocused(bool focused)
    {
        if (outline != null) outline.SetHighlighted(focused);
    }

    // Volumen que hay que mirar en 1ra persona
    public Bounds LookBounds
    {
        get
        {
            if (lookRadius > 0f)
                return new Bounds(transform.position + lookOffset, Vector3.one * (lookRadius * 2f));

            if (outline == null || !outline.TryGetBounds(out Bounds bounds))
                bounds = new Bounds(transform.position + lookOffset, Vector3.one);

            bounds.Expand(lookMargin * 2f);
            return bounds;
        }
    }

    // Grados que le faltan a la mirada para tocar la zona (0 = la está mirando)
    public float LookMissAngle(Vector3 eyes, Vector3 forward)
    {
        Bounds bounds = LookBounds;
        Vector3 toCenter = bounds.center - eyes;
        float centerAngle = Vector3.Angle(forward, toCenter);
        float distance = toCenter.magnitude;

        bool inside = lookRadius > 0f ? distance <= lookRadius : bounds.Contains(eyes);
        if (inside) return Mathf.Max(0f, centerAngle - InsideViewAngle);

        if (lookRadius > 0f)
            return Mathf.Max(0f, centerAngle - Mathf.Asin(lookRadius / distance) * Mathf.Rad2Deg);

        if (bounds.IntersectRay(new Ray(eyes, forward))) return 0f;

        // Punto de la caja más cercano a donde se está mirando
        float along = Mathf.Max(0f, Vector3.Dot(toCenter, forward));
        Vector3 closest = bounds.ClosestPoint(eyes + forward * along);
        return Vector3.Angle(forward, closest - eyes);
    }

    protected bool IsWithin(Vector3 point, float radius) => Vector3.Distance(transform.position, point) <= radius;

    protected static string Localized(string spanish, string english)
    {
        var locale = LocalizationSettings.SelectedLocale;
        return locale != null && locale.Identifier.Code != "es" ? english : spanish;
    }

    private void OnDrawGizmosSelected()
    {
        Bounds bounds = LookBounds;
        Gizmos.color = Color.magenta;

        if (lookRadius > 0f) Gizmos.DrawWireSphere(bounds.center, lookRadius);
        else Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
