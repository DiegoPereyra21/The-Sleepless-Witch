using System.Linq;
using UnityEngine;

// Interactuable que está al alcance mientras el player está dentro de su trigger.
[RequireComponent(typeof(Collider))]
public abstract class TriggerInteractable : Interactable
{
    // Distancia máxima del player a la caja para seguir considerándolo adentro
    private const float MaxDistance = 4f;

    private Collider zone;
    private bool playerInside;

    // Al mover al player de golpe no siempre llega el aviso de salida: se comprueba también la distancia
    public override bool IsInRange(Vector3 playerPosition)
    {
        if (zone == null) zone = GetComponents<Collider>().FirstOrDefault(c => c.isTrigger);

        return playerInside && zone != null && zone.bounds.SqrDistance(playerPosition) <= MaxDistance * MaxDistance;
    }

    protected void ForgetPlayer() => playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
