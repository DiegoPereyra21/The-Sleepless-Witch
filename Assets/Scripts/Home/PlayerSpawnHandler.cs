using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    [SerializeField] private CharacterController playerController;
    [SerializeField] private Transform doorInsideSpawnPoint;
    [SerializeField] private CameraTransition cameraTransition;

    private void Start()
    {
        if (GameProgressManager.Instance.ShouldSpawnAtDoor)
            playerController.GetComponent<PlayerMovement>().Teleport(doorInsideSpawnPoint);

        cameraTransition.SnapToPlayer();
    }
}