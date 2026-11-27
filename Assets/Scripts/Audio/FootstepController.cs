using UnityEngine;

public class FootstepController : MonoBehaviour
{
    [Header("Evento de pasos")]
    [SerializeField] private AK.Wwise.Event footstepEvent;

    [Header("Configuración de pasos")]
    [SerializeField] private float distancePerStep = 5f;

    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private Vector3 lastPosition;
    private float distanceWalked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 movement = transform.position - lastPosition;

        // Ignora el movimiento vertical
        movement.y = 0f;

        float distance = movement.magnitude;
        lastPosition = transform.position;

        if (distance <= 0f)
            return;

        // No reproduce pasos mientras está en el aire (caminando solo sí, aunque el controlador esté apagado)
        bool autoWalking = playerMovement != null && playerMovement.IsAutoWalking;
        if (!characterController.isGrounded && !autoWalking)
            return;

        distanceWalked += distance;

        if (distanceWalked >= distancePerStep)
        {
            PlayFootstep();
            distanceWalked = 0f;
        }
    }

    private void PlayFootstep()
    {
        footstepEvent.Post(gameObject);
    }
}