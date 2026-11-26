using Unity.Cinemachine;
using UnityEngine;

// Abre un poco el FOV al correr, proporcional a la velocidad
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFovKick : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [Tooltip("Grados extra sobre el FOV base al correr")]
    [SerializeField, Min(0f)] private float runKick = 8f;
    [Tooltip("Tiempo aproximado para alcanzar el objetivo, en segundos")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    private PlayerMovement movement;
    private float baseFov;
    private float firstPersonBaseFov;
    private float kick;
    private float kickVelocity;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        baseFov = followCamera.Lens.FieldOfView;
        if (firstPersonCamera != null) firstPersonBaseFov = firstPersonCamera.Lens.FieldOfView;
    }

    private void OnDisable()
    {
        kick = 0f;
        kickVelocity = 0f;
        Apply();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        float target = movement.IsFollowView ? runKick * movement.RunBlend : 0f;
        kick = Mathf.SmoothDamp(kick, target, ref kickVelocity, smoothTime);
        Apply();
    }

    private void Apply()
    {
        SetFov(followCamera, baseFov + kick);
        SetFov(firstPersonCamera, firstPersonBaseFov + kick);
    }

    private static void SetFov(CinemachineCamera cam, float fov)
    {
        if (cam == null) return;

        LensSettings lens = cam.Lens;
        lens.FieldOfView = fov;
        cam.Lens = lens;
    }
}
