using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DeliveryInteraction : TriggerInteractable
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private PotionDisplayArea potionDisplayArea;

    public event System.Action OnInteracted;

    public override string Prompt => Localized("Entregar pedidos", "Deliver orders");

    private void Start()
    {
        foreach (QuestData quest in questManager.PendingDeliveries)
            potionDisplayArea.AddOne(quest.Potion);
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!GameProgressManager.Instance.IsCraftingTimeActive)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "Ya es demasiado tarde, ya no podes enviar pedidos a esta hora.");
            return;
        }

        DeliverMatchingQuests();
        OnInteracted?.Invoke();
    }

    private void DeliverMatchingQuests()
    {
        List<QuestData> delivered = new List<QuestData>();

        foreach (QuestData quest in questManager.ActiveQuests)
        {
            if (quest.Potion == null) continue;
            if (questManager.PendingDeliveries.Contains(quest)) continue;

            if (HomeStorage.Instance.RemoveCraftedPotion(quest.Potion))
            {
                questManager.MarkPendingDelivery(quest);
                delivered.Add(quest);
                potionDisplayArea.AddOne(quest.Potion);
            }
        }

        if (delivered.Count > 0)
        {
            GameSaveSystem.SaveAll(questManager);
            DialogueUI.Instance.ShowMessage("Ofelia", $"Entregaste {delivered.Count} pocion(es) de pedidos, ahora podes ir a acostarte.");
        }
        else
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "No tengo ninguna poción que pueda entregar.");
        }
    }
}
