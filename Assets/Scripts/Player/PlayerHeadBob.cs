using UnityEngine;

// Cabeceo y balanceo al caminar/correr. Mueve solo la posición del CameraTarget
// (PlayerLook solo toca su rotación) y la cámara lo sigue con damping.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHeadBob : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;

    [Header("Ritmo")]
    [Tooltip("Pasos por unidad recorrida (a más velocidad, más pasos por segundo)")]
    [SerializeField, Min(0.001f)] private float stepsPerUnit = 0.14f;
    [SerializeField, Min(0f)] private float minSpeed = 2f;

    [Header("Amplitud caminando (unidades de mundo)")]
    [SerializeField, Min(0f)] private float walkVertical = 0.057f;
    [SerializeField, Min(0f)] private float walkSide = 0.033f;

    [Header("Amplitud corriendo (unidades de mundo)")]
    [SerializeField, Min(0f)] private float runVertical = 0.098f;
    [SerializeField, Min(0f)] private float runSide = 0.057f;

    [Header("Primera persona")]
    [Tooltip("Intensidad del cabeceo en 1ra persona respecto de 3ra: 0 = sin cabeceo")]
    [SerializeField, Min(0f)] private float firstPersonScale = 0.3f;
    [Tooltip("Qué tan rápido cambia al cambiar de cámara")]
    [SerializeField, Min(0.1f)] private float scaleChangeRate = 6f;

    [Header("Transiciones")]
    [SerializeField, Min(0.01f)] private float startTime = 0.1f;
    [SerializeField, Min(0.01f)] private float returnTime = 0.15f;

    private PlayerMovement movement;
    private PlayerCameraRig cameraRig;
    private float modeScale = 1f;
    private Vector3 basePosition;
    private float phase;     // 1 ciclo = 1 paso (el apoyo cae en cada entero)
    private float weight;
    private float weightVelocity;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        cameraRig = GetComponent<PlayerCameraRig>();
        basePosition = cameraTarget.localPosition;
    }

    private void OnDisable()
    {
        if (cameraTarget != null) cameraTarget.localPosition = basePosition;
        weight = 0f;
        weightVelocity = 0f;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        float speed = movement.HorizontalSpeed;
        bool moving = movement.IsFollowView && movement.IsGrounded && speed > minSpeed;
        float runT = movement.RunBlend;

        // El balanceo lateral tiene período de 2 pasos
        if (moving)
            phase = Mathf.Repeat(phase + speed * stepsPerUnit * Time.deltaTime, 2f);

        weight = Mathf.SmoothDamp(weight, moving ? 1f : 0f, ref weightVelocity,
            moving ? startTime : returnTime);

        float targetScale = cameraRig != null && cameraRig.IsFirstPerson ? firstPersonScale : 1f;
        modeScale = Mathf.MoveTowards(modeScale, targetScale, scaleChangeRate * Time.deltaTime);

        float vertical = Mathf.Lerp(walkVertical, runVertical, runT);
        float side = Mathf.Lerp(walkSide, runSide, runT);

        float y = -Mathf.Cos(phase * Mathf.PI * 2f) * vertical;
        float x = Mathf.Sin(phase * Mathf.PI) * side;

        // El player está escalado: se divide para que la amplitud quede en unidades de mundo
        Vector3 offset = new Vector3(x, y, 0f) * (weight * modeScale / transform.lossyScale.y);
        cameraTarget.localPosition = basePosition + offset;
    }
}
