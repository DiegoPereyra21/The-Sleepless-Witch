using System;
using System.Collections;
using UnityEngine;

// Movimiento de la herramienta de una estación (cuchillo, mano del mortero)
public abstract class StationAnimation : MonoBehaviour
{
    [SerializeField] protected Transform tool;

    private Vector3 restPosition;
    private Quaternion restRotation;

    protected Vector3 RestPosition => transform.TransformPoint(restPosition);
    protected Quaternion RestRotation => transform.rotation * restRotation;

    protected virtual void Awake()
    {
        restPosition = transform.InverseTransformPoint(tool.position);
        restRotation = Quaternion.Inverse(transform.rotation) * tool.rotation;
    }

    // progress va de 0 a 1 mientras se trabaja
    public abstract IEnumerator Play(Action<float> progress);

    public void ResetPose() => tool.SetPositionAndRotation(RestPosition, RestRotation);

    // Lleva la herramienta desde donde está hasta una pose
    protected IEnumerator MoveTool(Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 startPosition = tool.position;
        Quaternion startRotation = tool.rotation;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            tool.SetPositionAndRotation(Vector3.Lerp(startPosition, position, t), Quaternion.Slerp(startRotation, rotation, t));
            yield return null;
        }

        tool.SetPositionAndRotation(position, rotation);
    }
}
