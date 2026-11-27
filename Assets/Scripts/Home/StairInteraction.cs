using UnityEngine;

public class StairInteraction : Interactable
{
    [SerializeField] private CharacterController playerController;
    [SerializeField] private CameraTransition cameraTransition;
    [Tooltip("Punto del otro piso: indica hacia dónde queda mirando al llegar")]
    [SerializeField] private Transform teleportDestination;
    [SerializeField] private StairPath path;
    [SerializeField] private float interactRadius = 1.5f;
    public bool EntryBlocked { get; set; }
    public string BlockedMessage { get; set; } = "Todavía tengo cosas pendientes antes de subir.";

    public event System.Action OnUsed;

    private bool GoesUp => teleportDestination.position.y > transform.position.y;

    public override string Prompt => GoesUp ? Localized("Subir", "Go up") : Localized("Bajar", "Go down");
    public override bool IsInRange(Vector3 playerPosition) => IsWithin(playerPosition, interactRadius);

    public override void Interact(PlayerInteractor player)
    {
        if (EntryBlocked)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", BlockedMessage);
            return;
        }

        playerController.GetComponent<PlayerAutoWalk>()
            .Climb(path, GoesUp, teleportDestination, cameraTransition, () => OnUsed?.Invoke());
    }
}