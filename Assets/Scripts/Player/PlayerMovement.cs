using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float outdoorSpeed = 11f;
    [SerializeField] private float indoorSpeed = 7f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform cameraTransform;

    [Header("Cámara en 3ra persona")]
    [Tooltip("Cuánto más rápido se corre respecto de caminar")]
    [SerializeField, Min(1f)] private float runMultiplier = 1.67f;
    [SerializeField] private float acceleration = 75f;
    [SerializeField] private float deceleration = 100f;

    [Header("Animación")]
    [Tooltip("Velocidad para la que está hecha la animación de correr; por encima se acelera")]
    [SerializeField, Min(0.1f)] private float runAnimationSpeed = 15f;
    [Tooltip("Tope de aceleración de la animación: más bajo se ve más natural, pero los pies patinan más")]
    [SerializeField, Min(1f)] private float maxAnimationSpeed = 1.2f;
    [SerializeField, Min(0f)] private float animationDamping = 0.1f;
    [Tooltip("Grados por segundo para los que se ve natural la animación de girar")]
    [SerializeField, Min(1f)] private float turnAnimationRate = 80f;
    [SerializeField, Min(0.01f)] private float turnSmoothTime = 0.12f;
    private Animator animator;
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;
    private CinemachineBrain cinemachineBrain;
    private CameraTransition cameraTransition;
    private PlayerStairClimber climber;
    private Vector3 horizontalVelocity;
    private float lastYaw;
    private float turnRate;
    private float turnRateVelocity;
    private Vector3 velocity;
    private int freezeCount;
    //para pausar al pj cuando este recolectando hongos, la ides es q sea reutilizable para distintas situaciones donde necesite una pausa
    // También queda quieto mientras la cámara vuelve desde una vista fija
    // Y mientras recorre una escalera, que lo mueve PlayerStairClimber
    public bool IsFrozen => freezeCount > 0 || IsClimbing || (cameraTransition != null && cameraTransition.IsReturningToPlayer);
    public bool IsClimbing => climber != null && climber.IsClimbing;
    
    public bool IsPickupInProgress { get; private set; }

    // true con la cámara que sigue al personaje; false en las vistas fijas (canasto, crafteo, escalera)
    public bool IsFollowView => cinemachineBrain != null && cinemachineBrain.isActiveAndEnabled;
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => controller.isGrounded;
    // 0 caminando, 1 corriendo a máxima velocidad
    public float RunBlend => Mathf.InverseLerp(WalkSpeed, RunSpeed, HorizontalSpeed);

    public float WalkSpeed => GameProgressManager.Instance.IsOutside ? outdoorSpeed : indoorSpeed;
    public float RunSpeed => WalkSpeed * runMultiplier;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        moveAction = GetComponent<PlayerInput>().actions["Move"];
        sprintAction = GetComponent<PlayerInput>().actions["Sprint"];
        animator = GetComponentInChildren<Animator>();
        cinemachineBrain = cameraTransform.GetComponent<CinemachineBrain>();
        cameraTransition = cameraTransform.GetComponent<CameraTransition>();
        climber = GetComponent<PlayerStairClimber>();
        lastYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (IsClimbing) return;

        Vector2 input = IsFrozen ? Vector2.zero : moveAction.ReadValue<Vector2>();

        bool followView = IsFollowView;

        // En 3ra persona se mueve respecto del cuerpo (que gira con la cámara);
        // en vistas fijas, respecto de la cámara
        Transform reference = followView ? transform : cameraTransform;

        Vector3 forward = reference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = reference.right;
        right.y = 0f;
        right.Normalize();

        Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

        bool isMoving = direction.sqrMagnitude > 0.0001f;
        animator.SetBool("IsWalking", isMoving);
        animator.SetBool("IsOutside", GameProgressManager.Instance.IsOutside);

        if (followView)
        {
            // Dentro de la casa no se corre
            bool running = GameProgressManager.Instance.IsOutside && sprintAction.IsPressed();
            float speed = running ? RunSpeed : WalkSpeed;
            float rate = isMoving ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, direction * speed, rate * Time.deltaTime);
        }
        else
        {
            if (isMoving)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            horizontalVelocity = direction * WalkSpeed;
        }

        // Congelado (recolección, canasto, crafteo): frena en seco
        if (IsFrozen) horizontalVelocity = Vector3.zero;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move((horizontalVelocity + velocity) * Time.deltaTime);

        UpdateLocomotionAnimation();
        UpdateTurnAnimation();
    }

    // Mueve al personaje de golpe y lo deja mirando hacia donde apunta el destino (flecha azul)
    public void Teleport(Transform destination)
    {
        controller.enabled = false;
        transform.position = destination.position;
        controller.enabled = true;

        if (TryGetComponent(out PlayerLook look))
            look.FaceDirection(destination.forward);

        ResetMotion();
    }

    // Olvida el movimiento anterior (después de teletransportarse o de una escalera)
    public void ResetMotion()
    {
        horizontalVelocity = Vector3.zero;
        velocity = Vector3.zero;
        turnRate = 0f;
        turnRateVelocity = 0f;
        lastYaw = transform.eulerAngles.y; // para que no dispare la animación de giro
    }

    // Quien mueve al personaje por su cuenta informa la velocidad, para la cámara en 1ra persona
    public void SetScriptedVelocity(Vector3 value)
    {
        value.y = 0f;
        horizontalVelocity = value;
    }

    // El Blend Tree elige la animación según la velocidad relativa al cuerpo (adelante, atrás, costados)
    private void UpdateLocomotionAnimation()
    {
        Vector3 local = transform.InverseTransformDirection(horizontalVelocity);
        animator.SetFloat("VelX", local.x, animationDamping, Time.deltaTime);
        animator.SetFloat("VelZ", local.z, animationDamping, Time.deltaTime);
        animator.SetFloat("AnimSpeed", Mathf.Clamp(HorizontalSpeed / runAnimationSpeed, 1f, maxAnimationSpeed));
    }

    // Giro en el lugar: el Animator recibe cuántos grados por segundo gira el cuerpo con la cámara
    private void UpdateTurnAnimation()
    {
        if (Time.deltaTime <= 0f) return;

        float yaw = transform.eulerAngles.y;
        bool canTurn = IsFollowView && !IsFrozen;
        float rate = canTurn ? Mathf.DeltaAngle(lastYaw, yaw) / Time.deltaTime : 0f;
        lastYaw = yaw;

        turnRate = Mathf.SmoothDamp(turnRate, rate, ref turnRateVelocity, turnSmoothTime);
        animator.SetFloat("Turn", turnRate);
        animator.SetFloat("TurnSpeed", Mathf.Clamp(Mathf.Abs(turnRate) / turnAnimationRate, 0.9f, 1.8f));
    }

    // Freeze temporal por duración fija (ej. animación de recolección)
    public void FreezeMovement(float duration)
    {
        StartCoroutine(TimedFreezeRoutine(duration));
    }

    private IEnumerator TimedFreezeRoutine(float duration)
    {
        freezeCount++;
        IsPickupInProgress = true;
        yield return new WaitForSeconds(duration);
        freezeCount--;
        IsPickupInProgress = false;
    }

    // Freeze indefinido, controlado manualmente (ej. mientras el canasto está abierto)
    public void SetFrozen(bool frozen)
    {
        freezeCount += frozen ? 1 : -1;
        freezeCount = Mathf.Max(0, freezeCount);
    }
}