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
    [Tooltip("De las anteriores, las que se siguen viendo mientras la cámara viaja porque son lo que se mira: la canasta")]
    [SerializeField] private Renderer[] shownWhileTravelling;
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
    private const float ArrivalDistance = 0.6f; // tan cerca de los ojos ya se considera que la cámara volvió

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
    private bool travellingHidden;
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

    private void OnDisable() => SetHidden(false, false);

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
        UpdateVisibility();
    }

    // Según la distancia real de la cámara, así no se ve la cabeza por dentro cuando va o viene de otra vista.
    // En las vistas fijas ya quietas (canasto, crafteo, escalera) siempre se muestra.
    private void UpdateVisibility()
    {
        Camera view = Camera.main;
        if (view == null)
        {
            SetHidden(false, false);
            return;
        }

        if (transition == null) transition = view.GetComponent<CameraTransition>();
        bool travelling = transition != null && transition.IsBlending;
        float distance = Vector3.Distance(view.transform.position, firstPersonCamera.transform.position);
        bool hide = distance < hideDistance && (movement.IsFollowView || travelling);

        // Lo que se está mirando se oculta recién cuando la cámara ya volvió a los ojos
        bool arrived = travelling && transition.IsReturningToPlayer && IsFirstPerson && distance < ArrivalDistance;
        SetHidden(hide, hide && (!travelling || arrived));
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
    private void SetHidden(bool hidden, bool travellingToo)
    {
        if (originalShadowModes == null) return;
        if (hidden == renderersHidden && travellingToo == travellingHidden) return;
        renderersHidden = hidden;
        travellingHidden = travellingToo;

        for (int i = 0; i < hiddenRenderers.Length; i++)
        {
            Renderer part = hiddenRenderers[i];
            if (part == null) continue;

            bool hide = System.Array.IndexOf(shownWhileTravelling, part) >= 0 ? travellingToo : hidden;
            part.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : originalShadowModes[i];
        }
    }
}
