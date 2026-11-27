using System.Collections.Generic;
using UnityEngine;
using Game.Collectibles;
//para q el player al apretar "E" agfgarre directamente el hongo, hada o baya o lo que sea
public class PlayerCollector : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f;
    [SerializeField] private LayerMask collectibleLayer;
    [SerializeField] private PlayerInventory inventory;
    //para cesto interactivo
    [SerializeField] private BasketDisplay basketDisplay;
    //para avisar q tiene q pausarse el movimiento del player cuando recolecta algo, usar oninteract
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    // Sonido de pickup
    [SerializeField] private AK.Wwise.Event pickupEvent;

    [SerializeField] private float pickupDelay = 0.2f; //tiempo para que la animación de agacharse se asiente antes de soltar el item en el canasto
    [SerializeField] private float dropPauseDuration = 0.15f; //pausa la animación mientras el item cae al canasto

    public bool CollectionBlocked { get; set; }
    public void Collect(Collectible target)
    {
        if (basketDisplay.IsOpen) return;
        if (playerMovement.IsPickupInProgress) return;
        if (inventory.IsFull) return;
        if (target == null || target.IsCollected) return;

        if (!GameProgressManager.Instance.IsNightActive)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "Ya pasó demasiado tiempo, esto ya no sirve de nada.");
            return;
        }

        if (CollectionBlocked)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "Todavía no acepté todos los pedidos, mejor reviso el buzón primero.");
            return;
        }

        // el hada deja de escapar apenas se la agarra
        if (target.TryGetComponent(out FairyHover fairy))
            fairy.Catch();

        playerMovement.FreezeMovement(target.FreezeDuration);
        animator.SetTrigger("Interact");

        StartCoroutine(CollectAfterDelay(target));
    }

    private System.Collections.IEnumerator CollectAfterDelay(Collectible target)
    {
        yield return new WaitForSeconds(pickupDelay);
        if (target == null) yield break;

        ItemData item = target.Item;
        int value = target.Value;

        target.Collect(() =>
        {
            pickupEvent.Post(gameObject);

            if (!inventory.TryAdd(item, value)) return;

            for (int i = 0; i < value; i++)
                basketDisplay.Drop(item);

            StartCoroutine(PauseAnimatorBriefly());
        });
    }

    private System.Collections.IEnumerator PauseAnimatorBriefly()
    {
        animator.speed = 0f;
        yield return new WaitForSeconds(dropPauseDuration);
        animator.speed = 1f;
    }

    //hace como un collider frente al player para que "agarre" lo que tenga al frente suyo(luego tengo q hacer un inventario en el player para q los "guarde")
    public void AddNearby(List<Interactable> results)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, collectRadius, collectibleLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Collectible c))
                results.Add(c);
        }
    }
}