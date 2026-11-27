using System;
using System.Collections;
using UnityEngine;

// El cuchillo se levanta solo, da unos golpes sobre el ingrediente y vuelve a su lugar
public class ChopAnimation : StationAnimation
{
    [Tooltip("Cómo queda el cuchillo al final de cada golpe. Se puede mover en la escena")]
    [SerializeField] private Transform chopPose;
    [SerializeField, Min(0f)] private float liftHeight = 0.6f;
    [SerializeField, Min(1)] private int strikes = 3;
    [Tooltip("Cuánto se corre entre golpe y golpe, en ejes de Chop Pose")]
    [SerializeField] private Vector3 strikeStep = new Vector3(0f, 0f, 0.14f);

    [Header("Tiempos")]
    [SerializeField, Min(0.01f)] private float travelTime = 0.25f;
    [SerializeField, Min(0.01f)] private float downTime = 0.08f;
    [SerializeField, Min(0.01f)] private float upTime = 0.16f;

    public override IEnumerator Play(Action<float> progress)
    {
        Vector3 step = chopPose.rotation * strikeStep;
        Vector3 lift = Vector3.up * liftHeight;

        for (int i = 0; i < strikes; i++)
        {
            Vector3 hit = chopPose.position + step * (i - (strikes - 1) * 0.5f);

            yield return MoveTool(hit + lift, chopPose.rotation, i == 0 ? travelTime : upTime);
            yield return MoveTool(hit, chopPose.rotation, downTime);
            progress?.Invoke((i + 1f) / strikes);
        }

        yield return MoveTool(chopPose.position + lift, chopPose.rotation, upTime);
        yield return MoveTool(RestPosition, RestRotation, travelTime);
    }
}
