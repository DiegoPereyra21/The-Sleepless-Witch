using UnityEngine;

// La única vista de la mesa: dónde se para la cámara y cuánto tiene que abarcar
public class CraftingView : MonoBehaviour
{
    [Tooltip("Ángulo horizontal que tiene que entrar en pantalla para ver toda la mesa")]
    [SerializeField, Range(40f, 120f)] private float horizontalFov = 86f;
    [Tooltip("En pantallas muy anchas no se baja de este ángulo vertical")]
    [SerializeField, Range(20f, 90f)] private float minVerticalFov = 50f;

    public Transform Anchor => transform;

    public float FieldOfViewFor(float aspect)
    {
        float half = Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad) / Mathf.Max(0.1f, aspect);
        return Mathf.Max(minVerticalFov, 2f * Mathf.Atan(half) * Mathf.Rad2Deg);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, FieldOfViewFor(16f / 9f), 12f, 0.5f, 16f / 9f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
