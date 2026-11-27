using UnityEngine;
using UnityEngine.InputSystem;

// Mouse/stick: el cuerpo gira en horizontal con la cámara y el pitch va solo en cameraTarget.
// Solo actúa con la cámara que sigue al personaje, no en las vistas fijas.
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerInput))]
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;

    [Header("Sensibilidad")]
    [SerializeField] private float mouseDegreesPerPixel = 0.1f;
    [SerializeField] private float stickDegreesPerSecond = 140f;

    [Header("Límites verticales")]
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 70f;

    [Tooltip("Hacia dónde mira la cámara al empezar (grados)")]
    [SerializeField] private float startYaw;

    private PlayerMovement movement;
    private InputAction lookAction;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        lookAction = GetComponent<PlayerInput>().actions["Look"];
        yaw = startYaw;
    }

    private void Update()
    {
        if (!movement.IsFollowView) return;

        if (Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked)
        {
            InputControl control = lookAction.activeControl;
            bool pad = control != null && control.device is Gamepad;
            float scale = pad ? stickDegreesPerSecond * Time.deltaTime : mouseDegreesPerPixel;
            Vector2 look = lookAction.ReadValue<Vector2>() * scale;

            yaw = Mathf.Repeat(yaw + look.x, 360f);
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        }

        ApplyRotation();
    }

    // Deja al personaje mirando hacia una dirección, con la vista nivelada (al teletransportarse)
    public void FaceDirection(Vector3 direction)
    {
        pitch = 0f;
        SetHeading(direction);
    }

    // Gira en horizontal sin tocar hacia dónde se mira en vertical
    public void SetHeading(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        yaw = Quaternion.LookRotation(direction).eulerAngles.y;
        ApplyRotation();
    }

    // Recupera la orientación de la cámara (el personaje pudo haber girado en una vista fija)
    public void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
