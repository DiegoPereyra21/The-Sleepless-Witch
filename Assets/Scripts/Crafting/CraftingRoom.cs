using System;
using UnityEngine;

// Entrar y salir de la mesa de crafteo
public class CraftingRoom : Interactable
{
    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [Tooltip("Raíz del player: se ocultan sus renderers mientras se craftea.")]
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private Light playerSpotlight;

    [Header("Cámara")]
    [SerializeField] private CameraTransition cameraTransition;
    [Tooltip("Qué se ve mientras se craftea")]
    [SerializeField] private CraftingView view;

    [Header("Mesa")]
    [SerializeField] private CraftingTable table;
    [SerializeField] private CraftingPointer pointer;
    [SerializeField] private float interactRadius = 2f;

    public event Action OnEntered;
    public event Action OnExited;

    public bool IsInside { get; private set; }
    public bool IsBusy => table.IsBusy;
    public CraftingTable Table => table;

    private PlayerInteractor user;

    public override string Prompt => IsInside
        ? Localized("Salir de la mesa", "Leave the table")
        : Localized("Preparar pociones", "Brew potions");

    public override bool IsInRange(Vector3 playerPosition) => IsWithin(playerPosition, interactRadius);

    public override void Interact(PlayerInteractor player)
    {
        if (IsBusy) return;

        if (IsInside) Exit();
        else Enter(player);
    }

    private void Enter(PlayerInteractor player)
    {
        IsInside = true;
        user = player;
        user.Capture(this); // la tecla queda para salir
        playerMovement.SetFrozen(true);
        SetPlayerVisible(false);
        if (playerSpotlight != null) playerSpotlight.enabled = false;

        float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
        cameraTransition.TransitionTo(view.Anchor, view.FieldOfViewFor(aspect));
        OnEntered?.Invoke();
    }

    private void Exit()
    {
        pointer.Release();

        IsInside = false;
        playerMovement.SetFrozen(false);
        SetPlayerVisible(true);
        if (playerSpotlight != null) playerSpotlight.enabled = true;

        cameraTransition.TransitionToPlayer();
        user.Release(this);

        OnExited?.Invoke();
    }

    private void SetPlayerVisible(bool visible)
    {
        foreach (Renderer r in playerRoot.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }
}
