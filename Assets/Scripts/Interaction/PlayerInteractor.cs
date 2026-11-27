using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Único lector de la acción Interact: elige un solo interactuable, lo resalta y lo activa.
[RequireComponent(typeof(PlayerInput), typeof(PlayerMovement))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Margen en grados para dar por mirado algo en 1ra persona")]
    [SerializeField, Range(0f, 30f)] private float lookTolerance = 8f;

    // Entre dos cosas miradas gana la que está más centrada
    private const float CenterWeight = 0.25f;

    private PlayerMovement movement;
    private PlayerCameraRig cameraRig;
    private PlayerCollector collector;
    private InputAction interactAction;
    private readonly List<Interactable> candidates = new List<Interactable>();

    public Interactable Focused { get; private set; }
    // Mientras algo se apropia de la tecla (ej. la mesa de crafteo), solo se le avisa a eso
    public Interactable Captured { get; private set; }
    public bool IsLooking => cameraRig != null && cameraRig.IsFirstPerson;
    public bool CanInteract => Time.timeScale > 0f && movement.IsFollowView && !movement.IsFrozen;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        cameraRig = GetComponent<PlayerCameraRig>();
        collector = GetComponent<PlayerCollector>();
        interactAction = GetComponent<PlayerInput>().actions["Interact"];

        if (GetComponent<InteractionPromptUI>() == null)
            gameObject.AddComponent<InteractionPromptUI>();
    }

    private void OnEnable() => interactAction.performed += OnInteract;

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;
        SetFocus(null);
    }

    private void Update() => SetFocus(Captured == null && CanInteract ? FindTarget() : null);

    public void Capture(Interactable owner)
    {
        Captured = owner;
        SetFocus(null);
    }

    public void Release(Interactable owner)
    {
        if (Captured == owner) Captured = null;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (Time.timeScale <= 0f) return;

        if (Captured != null)
        {
            Captured.Interact(this);
            return;
        }

        if (!CanInteract) return;

        Interactable target = FindTarget();
        if (target != null) target.Interact(this);
    }

    private void SetFocus(Interactable next)
    {
        if (next == Focused) return;

        if (Focused != null) Focused.SetFocused(false);
        Focused = next;
        if (Focused != null) Focused.SetFocused(true);
    }

    private Interactable FindTarget()
    {
        candidates.Clear();
        candidates.AddRange(Interactable.Listed);
        if (collector != null) collector.AddNearby(candidates);

        Camera view = Camera.main;
        bool looking = IsLooking && view != null;

        Interactable best = null;
        float bestScore = float.MaxValue;

        foreach (Interactable candidate in candidates)
        {
            if (!candidate.IsAvailable || !candidate.IsInRange(transform.position)) continue;

            float score;
            if (looking)
            {
                if (!TryScoreLook(view.transform, candidate, out score)) continue;
            }
            else
            {
                score = Vector3.Distance(transform.position, candidate.transform.position);
            }

            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    // Menor puntaje = más centrado. false si no se lo está mirando
    private bool TryScoreLook(Transform view, Interactable candidate, out float score)
    {
        float missAngle = candidate.LookMissAngle(view.position, view.forward);
        float centerAngle = Vector3.Angle(view.forward, candidate.LookBounds.center - view.position);

        score = missAngle + centerAngle * CenterWeight;
        return missAngle <= lookTolerance;
    }
}
