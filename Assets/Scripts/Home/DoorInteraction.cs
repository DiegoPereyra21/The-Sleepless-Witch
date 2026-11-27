using UnityEngine;

public class DoorInteraction : Interactable
{
    [SerializeField] private float interactRadius = 2f;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private BasketDisplay basketDisplay;
    [SerializeField] private CameraTransition cameraTransition;
    [SerializeField] private QuestManager questManager;

    [SerializeField] private CharacterController playerController;
    [SerializeField] private Transform teleportDestination;
    [SerializeField] private DayTransition dayTransition;

    public bool EntryBlocked { get; set; }
    public string BlockedMessage { get; set; } = "No puedo entrar todavía.";

    public override string Prompt => Localized("Entrar a la casa", "Go inside");
    public override bool IsAvailable => base.IsAvailable && GameProgressManager.Instance.IsOutside;
    public override bool IsInRange(Vector3 playerPosition) => IsWithin(playerPosition, interactRadius);

    public override void Interact(PlayerInteractor player)
    {
        if (EntryBlocked)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", BlockedMessage);
            return;
        }

        int itemsCollected = inventory.TotalCount;

        HomeStorage.Instance.Deposit(inventory);
        GameProgressManager.Instance.MarkEnteredHouse();
        HomeStorage.Instance.Save();
        //GameProgressManager.Instance.Save();
        questManager.Save();
        basketDisplay.ClearAll();
        basketDisplay.SetAvailable(false);

        // El salto adentro se hace con la pantalla ya cubierta
        dayTransition.PlayEndOfNight(itemsCollected, () =>
        {
            playerController.GetComponent<PlayerMovement>().Teleport(teleportDestination);
            GameProgressManager.Instance.EnterHouse();
            cameraTransition.SnapToPlayer();
        });
    }
}