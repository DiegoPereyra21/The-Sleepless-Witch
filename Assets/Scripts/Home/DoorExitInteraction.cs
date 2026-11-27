using UnityEngine;

public class DoorExitInteraction : TriggerInteractable
{
    [SerializeField] private CameraTransition cameraTransition;
    //activa desactiva el canasto
    [SerializeField] private BasketDisplay basketDisplay;

    //para que por ahora me tepee al otro lado de la puerta, luego agregaremos animacion de puerta y que se mueva obligadamente hacia afuera
    [SerializeField] private CharacterController playerController;
    [SerializeField] private Transform teleportDestination;
    public event System.Action OnUsed;

    public override string Prompt => Localized("Salir", "Go outside");
    public override bool IsAvailable => base.IsAvailable && !GameProgressManager.Instance.IsOutside;

    public override void Interact(PlayerInteractor player)
    {
        if (!GameProgressManager.Instance.IsNightActive)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "No puedo salir sin dormir primero.");
            return;
        }

        playerController.GetComponent<PlayerMovement>().Teleport(teleportDestination);
        ForgetPlayer();

        basketDisplay.SetAvailable(true);
        GameProgressManager.Instance.MarkWentOutside();
        GameProgressManager.Instance.MarkOutside();
        cameraTransition.SnapToPlayer();
        OnUsed?.Invoke();
    }
}