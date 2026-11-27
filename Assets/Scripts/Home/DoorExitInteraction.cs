using System.Collections;
using UnityEngine;

public class DoorExitInteraction : TriggerInteractable
{
    [SerializeField] private CameraTransition cameraTransition;
    //activa desactiva el canasto
    [SerializeField] private BasketDisplay basketDisplay;

    [SerializeField] private CharacterController playerController;
    [SerializeField] private DoorPassage passage;
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

        playerController.GetComponent<PlayerAutoWalk>()
            .PassDoor(passage, false, cameraTransition, GoOutside, () => OnUsed?.Invoke());
    }

    // Pasa con la pantalla en negro, ya del lado de afuera
    private IEnumerator GoOutside()
    {
        ForgetPlayer();

        basketDisplay.SetAvailable(true);
        GameProgressManager.Instance.MarkWentOutside();
        GameProgressManager.Instance.MarkOutside();
        yield break;
    }
}