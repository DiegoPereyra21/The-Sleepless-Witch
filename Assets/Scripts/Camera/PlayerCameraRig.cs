using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

// Alterna entre la cámara en 3ra y en 1ra persona según las FirstPersonZone
[DefaultExecutionOrder(-5)]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerCameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera thirdPersonCamera;
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private Transform cameraTarget;
    [Tooltip("Modelo del personaje (el que tiene el Animator)")]
    [SerializeField] private Animator body;

    [Header("Primera persona")]
    [Tooltip("Mallas que en 1ra persona solo proyectan sombra: cabeza, pelo, sombrero y canasta. Sacá la canasta de la lista para verla")]
    [SerializeField] private Renderer[] hiddenRenderers;
    [Tooltip("Posición de los ojos respecto del hueso de la cabeza (unidades de mundo). Y = altura, Z = adelante")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.55f, -0.7f);
    [Tooltip("Cuánto se adelanta la cámara al mirar hacia abajo, para no ver dentro del cuello")]
    [SerializeField, Min(0f)] private float lookDownForward = 0.6f;
    [Tooltip("Cuánto se adelanta la cámara al avanzar, porque el cuerpo se inclina y tapa la vista")]
    [SerializeField, Min(0f)] private float walkForward = 0.9f;
    [Tooltip("Suavizado con el que la cámara sigue a la cabeza animada. Más alto = más estable")]
    [SerializeField, Min(0f)] private float headFollowTime = 0.25f;
    [Tooltip("Esas mallas se ocultan cuando la cámara está más cerca que esto de los ojos")]
    [SerializeField, Min(0f)] private float hideDistance = 3.5f;

    [Header("Zona")]
    [Tooltip("Cuánto hay que alejarse del borde de la zona para volver a 3ra persona")]
    [SerializeField, Min(0f)] private float zoneExitMargin = 0.5f;

    private const int ActivePriority = 20;
    private const int InactivePriority = 0;

    private PlayerMovement movement;
    private CameraTransition transition;
    private Transform head;
    private Vector3 targetBasePosition;
    private Vector3 headPosition; // relativa al player, sin su rotación
    private Vector3 headVelocity;
    private float walkBlend;
    private float walkBlendVelocity;
    private ShadowCastingMode[] originalShadowModes;
    private bool renderersHidden;
    public bool IsFirstPerson { get; private set; }
    public CinemachineCamera ActiveCamera => IsFirstPerson ? firstPersonCamera : thirdPersonCamera;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        head = body.GetBoneTransform(HumanBodyBones.Head);
        targetBasePosition = cameraTarget.localPosition;
        headPosition = HeadPosition();

        originalShadowModes = new ShadowCastingMode[hiddenRenderers.Length];
        for (int i = 0; i < hiddenRenderers.Length; i++)
            originalShadowModes[i] = hiddenRenderers[i].shadowCastingMode;

        Apply(false);
    }

    private void OnDisable() => SetHidden(false);

    private void Update() => RefreshMode();

    public void RefreshMode()
    {
        bool inside = FirstPersonZone.AnyContains(transform.position, IsFirstPerson ? zoneExitMargin : 0f);
        if (inside != IsFirstPerson) Apply(inside);
    }

    // Corre después del Animator y antes de Cinemachine
    private void LateUpdate()
    {
        PlaceFirstPersonCamera();

        // Según la distancia real de la cámara, así no se ve la cabeza por dentro durante la transición.
        // En las vistas fijas (canasto, crafteo, escalera) siempre se muestra.
        Camera view = Camera.main;
        if (transition == null && view != null) transition = view.GetComponent<CameraTransition>();
        bool towardPlayer = movement.IsFollowView || (transition != null && transition.IsReturningToPlayer);

        bool hide = towardPlayer && view != null
            && Vector3.Distance(view.transform.position, firstPersonCamera.transform.position) < hideDistance;

        SetHidden(hide);
    }

    private Vector3 HeadPosition() =>
        Quaternion.Inverse(transform.rotation) * (head.position - transform.position);

    // La cámara acompaña la posición de la cabeza (así no se mete en el cuerpo al correr o agacharse),
    // pero la rotación la sigue manejando el mouse
    private void PlaceFirstPersonCamera()
    {
        if (Time.deltaTime > 0f)
            headPosition = Vector3.SmoothDamp(headPosition, HeadPosition(), ref headVelocity, headFollowTime);

        Vector3 headBob = cameraTarget.parent.TransformVector(cameraTarget.localPosition - targetBasePosition);

        if (Time.deltaTime > 0f)
        {
            float walking = Mathf.Clamp01(movement.HorizontalSpeed / movement.WalkSpeed);
            walkBlend = Mathf.SmoothDamp(walkBlend, walking, ref walkBlendVelocity, headFollowTime);
        }

        // El offset gira con el pitch: al mirar hacia abajo la cámara se asoma sobre el pecho
        float lookDown = Mathf.Max(0f, -cameraTarget.forward.y);

        float forward = lookDownForward * lookDown + walkForward * walkBlend;

        firstPersonCamera.transform.position = transform.position + transform.rotation * headPosition
            + cameraTarget.rotation * eyeOffset + transform.forward * forward + headBob;
    }

    private void Apply(bool firstPerson)
    {
        IsFirstPerson = firstPerson;
        SetPriority(firstPersonCamera, firstPerson);
        SetPriority(thirdPersonCamera, !firstPerson);
    }

    private static void SetPriority(CinemachineCamera cam, bool active)
    {
        cam.Priority.Enabled = true;
        cam.Priority.Value = active ? ActivePriority : InactivePriority;
    }

    // Usa el modo de sombras (no enabled) para no pisar a quienes ocultan los renderers
    private void SetHidden(bool hidden)
    {
        if (hidden == renderersHidden || originalShadowModes == null) return;
        renderersHidden = hidden;

        for (int i = 0; i < hiddenRenderers.Length; i++)
            if (hiddenRenderers[i] != null)
                hiddenRenderers[i].shadowCastingMode = hidden ? ShadowCastingMode.ShadowsOnly : originalShadowModes[i];
    }
}
