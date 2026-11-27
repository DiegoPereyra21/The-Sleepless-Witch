using System.Collections.Generic;
using UnityEngine;

// Recorrido de una escalera. Los hijos de "Points Root" marcan por dónde pisa el personaje,
// de abajo hacia arriba: el primero en el piso de abajo y el último en el de arriba.
public class StairPath : MonoBehaviour
{
    [SerializeField] private Transform pointsRoot;

    [Header("Corte en escaleras largas")]
    [Tooltip("Se muestra un tramo en cada piso y se salta el del medio")]
    [SerializeField] private bool skipMiddle = true;
    [Tooltip("Escalones que se ven del lado de abajo")]
    [SerializeField, Min(1)] private int bottomSteps = 4;
    [Tooltip("Escalones que se ven del lado de arriba")]
    [SerializeField, Min(1)] private int topSteps = 3;

    [Header("Cámaras que muestran al personaje")]
    [SerializeField] private Transform lowerCamera;
    [SerializeField] private Transform upperCamera;
    [Tooltip("A cuánto del piso de arriba se pasa a la cámara de arriba (unidades de mundo)")]
    [SerializeField, Min(0f)] private float upperCameraMargin = 3.5f;

    public List<Vector3> GetPoints(bool goingUp)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (Transform point in pointsRoot)
            points.Add(point.position);

        if (!goingUp) points.Reverse();
        return points;
    }

    public Transform LowerCamera => lowerCamera;
    public Transform UpperCamera => upperCamera;

    // Puntos (en el orden de GetPoints) donde se corta el recorrido y donde se retoma.
    // false si la escalera se recorre entera
    public bool TryGetCut(bool goingUp, out int leaveAt, out int resumeAt)
    {
        leaveAt = goingUp ? bottomSteps : topSteps;
        resumeAt = pointsRoot.childCount - 1 - (goingUp ? topSteps : bottomSteps);
        return skipMiddle && resumeAt > leaveAt + 1;
    }

    // La cámara que mejor ve al personaje según la altura de sus pies (recorrido sin corte)
    public Transform CameraFor(Vector3 feet)
    {
        float top = pointsRoot.GetChild(pointsRoot.childCount - 1).position.y;
        return feet.y > top - upperCameraMargin ? upperCamera : lowerCamera;
    }

    private void OnDrawGizmos()
    {
        if (pointsRoot == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < pointsRoot.childCount; i++)
        {
            Vector3 point = pointsRoot.GetChild(i).position;
            Gizmos.DrawWireSphere(point, 0.25f);
            if (i > 0) Gizmos.DrawLine(pointsRoot.GetChild(i - 1).position, point);
        }

        Gizmos.color = Color.yellow;
        if (lowerCamera != null) Gizmos.DrawWireCube(lowerCamera.position, Vector3.one);
        if (upperCamera != null) Gizmos.DrawWireCube(upperCamera.position, Vector3.one);
    }
}
