using System;
using UnityEngine;

// Puntos para cruzar una puerta caminando. De cada lado: dónde se para junto a la puerta,
// hasta dónde camina al salir de ella y desde qué cámara se la ve.
public class DoorPassage : MonoBehaviour
{
    [Serializable]
    public struct Side
    {
        public Transform door;
        public Transform end;
        public Transform camera;
    }

    [SerializeField] private Side outside;
    [SerializeField] private Side inside;

    [Header("Hoja de la puerta")]
    [Tooltip("Todo lo que gira junto: la hoja y el picaporte")]
    [SerializeField] private Transform[] leaf;
    [Tooltip("Eje de giro, en el borde contrario al picaporte")]
    [SerializeField] private Transform hinge;
    [SerializeField, Range(0f, 150f)] private float openAngle = 100f;
    [Tooltip("Segundos que tarda en abrirse o cerrarse")]
    [SerializeField, Min(0.05f)] private float swingTime = 0.45f;

    // Pose cerrada de cada pieza y hacia dónde va la hoja desde la bisagra
    private Vector3[] closedPositions;
    private Quaternion[] closedRotations;
    private Vector3 closedDirection;

    public Side Outside => outside;
    public Side Inside => inside;
    public float SwingTime => swingTime;

    private void Awake()
    {
        closedPositions = new Vector3[leaf.Length];
        closedRotations = new Quaternion[leaf.Length];

        for (int i = 0; i < leaf.Length; i++)
        {
            closedPositions[i] = leaf[i].localPosition;
            closedRotations[i] = leaf[i].localRotation;
        }

        // La primera pieza es la hoja
        closedDirection = leaf[0].GetComponent<Renderer>().bounds.center - hinge.position;
        closedDirection.y = 0f;
    }

    // amount: 0 cerrada, 1 abierta. outward: gira hacia afuera de la casa (si no, hacia adentro)
    public void SetOpen(float amount, bool outward)
    {
        amount = Mathf.Clamp01(amount);

        Vector3 exit = outside.end.position - inside.end.position;
        exit.y = 0f;
        float side = Vector3.Dot(Quaternion.AngleAxis(90f, Vector3.up) * closedDirection, outward ? exit : -exit);
        Quaternion turn = Quaternion.AngleAxis(Mathf.Sign(side) * openAngle * Mathf.SmoothStep(0f, 1f, amount), Vector3.up);

        for (int i = 0; i < leaf.Length; i++)
        {
            Transform part = leaf[i];
            part.localPosition = closedPositions[i];
            part.localRotation = closedRotations[i];
            if (amount <= 0f) continue;

            part.SetPositionAndRotation(hinge.position + turn * (part.position - hinge.position), turn * part.rotation);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hinge == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(hinge.position, hinge.position + Vector3.up * 11f);
    }

    private void OnDrawGizmos()
    {
        Draw(outside, Color.green);
        Draw(inside, Color.cyan);
    }

    private static void Draw(Side side, Color color)
    {
        if (side.door == null || side.end == null) return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(side.door.position, 0.4f);
        Gizmos.DrawLine(side.door.position, side.end.position);
        Gizmos.DrawWireSphere(side.end.position, 0.25f);

        Gizmos.color = Color.yellow;
        if (side.camera != null) Gizmos.DrawWireCube(side.camera.position, Vector3.one);
    }
}
