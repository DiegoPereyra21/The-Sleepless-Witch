using System;
using System.Collections;
using UnityEngine;

// La mano del mortero da vueltas alrededor del cuenco mientras lo golpea
public class GrindAnimation : StationAnimation
{
    [Tooltip("Centro del cuenco: la mano gira alrededor de su eje vertical")]
    [SerializeField] private Transform center;
    [SerializeField, Min(1)] private int turns = 3;
    [SerializeField, Min(0.1f)] private float duration = 1.3f;
    [SerializeField, Min(0f)] private float poundHeight = 0.18f;

    public override IEnumerator Play(Action<float> progress)
    {
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = time / duration;
            Quaternion turn = Quaternion.AngleAxis(360f * turns * Mathf.SmoothStep(0f, 1f, t), Vector3.up);
            float pound = Mathf.Abs(Mathf.Sin(t * turns * Mathf.PI * 2f)) * poundHeight;

            Vector3 position = center.position + turn * (RestPosition - center.position) + Vector3.up * pound;
            tool.SetPositionAndRotation(position, turn * RestRotation);

            progress?.Invoke(t);
            yield return null;
        }

        ResetPose(); // son vueltas enteras, así que termina donde empezó
        progress?.Invoke(1f);
    }
}
