using System.Collections.Generic;
using UnityEngine;

// Zona donde la cámara pasa a primera persona. No necesita collider:
// PlayerCameraRig consulta si el player está dentro de alguna zona activa.
public class FirstPersonZone : MonoBehaviour
{
    public enum Shape { Circle, Square }

    [SerializeField] private Shape shape = Shape.Circle;
    [Tooltip("Radio de la zona circular (unidades de mundo)")]
    [SerializeField, Min(0.1f)] private float radius = 10f;
    [Tooltip("Ancho (X) y largo (Z) de la zona cuadrada (unidades de mundo)")]
    [SerializeField] private Vector2 size = new Vector2(20f, 20f);
    [Tooltip("Alto total de la zona, centrado en este objeto")]
    [SerializeField, Min(0.1f)] private float height = 12f;

    private static readonly List<FirstPersonZone> zones = new List<FirstPersonZone>();

    private void OnEnable() => zones.Add(this);
    private void OnDisable() => zones.Remove(this);

    // margin agranda las zonas: se usa al salir para que no parpadee en el borde
    public static bool AnyContains(Vector3 point, float margin = 0f)
    {
        foreach (FirstPersonZone zone in zones)
            if (zone.Contains(point, margin)) return true;

        return false;
    }

    // La escala del Transform no se usa; la rotación sí
    public bool Contains(Vector3 point, float margin = 0f)
    {
        Vector3 local = Quaternion.Inverse(transform.rotation) * (point - transform.position);
        if (Mathf.Abs(local.y) > height * 0.5f + margin) return false;

        if (shape == Shape.Circle)
        {
            float limit = radius + margin;
            return local.x * local.x + local.z * local.z <= limit * limit;
        }

        return Mathf.Abs(local.x) <= size.x * 0.5f + margin
            && Mathf.Abs(local.z) <= size.y * 0.5f + margin;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);

        if (shape == Shape.Square)
        {
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, height, size.y));
            return;
        }

        const int segments = 48;
        Vector3 up = Vector3.up * (height * 0.5f);
        Vector3 previous = new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            Gizmos.DrawLine(previous + up, next + up);
            Gizmos.DrawLine(previous - up, next - up);
            Gizmos.DrawLine(previous, next);
            if (i % (segments / 8) == 0) Gizmos.DrawLine(next - up, next + up);

            previous = next;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("GameObject/Zona primera persona", false, 10)]
    private static void CreateZone(UnityEditor.MenuCommand command)
    {
        var zone = new GameObject("FirstPersonZone", typeof(FirstPersonZone));
        UnityEditor.GameObjectUtility.SetParentAndAlign(zone, command.context as GameObject);

        // Aparece donde está mirando la vista de escena
        if (command.context == null && UnityEditor.SceneView.lastActiveSceneView != null)
            zone.transform.position = UnityEditor.SceneView.lastActiveSceneView.pivot;

        UnityEditor.Undo.RegisterCreatedObjectUndo(zone, "Crear zona primera persona");
        UnityEditor.Selection.activeGameObject = zone;
    }
#endif
}
