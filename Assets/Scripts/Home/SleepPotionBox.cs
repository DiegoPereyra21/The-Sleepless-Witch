using UnityEngine;

public class SleepPotionBox : TriggerInteractable
{
    [SerializeField] private int potionCost = 100;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private BasketDisplay basketDisplay;

    public override string Prompt => Localized("Comerciar", "Trade");
    public override bool IsAvailable => base.IsAvailable && !GameProgressManager.Instance.SleepIngredientObtained;


    private bool CanDeliverToday(GameProgressManager progress)
    {
        return progress.SleepIngredientPurchased && progress.CurrentDay > progress.SleepIngredientPurchaseDay;
    }

    public override void Interact(PlayerInteractor player)
    {
        GameProgressManager progress = GameProgressManager.Instance;

        if (progress.SleepIngredientObtained) return;

        if (progress.SleepIngredientPurchased)
        {
            if (!CanDeliverToday(progress))
            {
                DialogueUI.Instance.ShowMessage("Ofelia", "Vuelve mañana y te tendre el ingrediente especial.");
                return;
            }

            ItemData sleepIngredient = GameDatabase.Instance.SleepIngredient;

            if (!inventory.TryAdd(sleepIngredient))
            {
                DialogueUI.Instance.ShowMessage("Ofelia", "No te puedo dar el ojo, tenes el canasto lleno.");
                return;
            }

            basketDisplay.Drop(sleepIngredient);

            progress.MarkSleepIngredientObtained();
            progress.Save();

            DialogueUI.Instance.ShowMessage("Ofelia", "Toma el ingrediente espacial y crea la pocion de sueño profundo con el.");
            return;
        }

        if (progress.Money < potionCost)
        {
            int missing = potionCost - progress.Money;
            DialogueUI.Instance.ShowMessage("Ofelia", $"Todavía te faltan {missing} monedas, no te lo dare gratis.");
            return;
        }

        progress.TrySpendMoney(potionCost);
        progress.MarkSleepIngredientPurchased();
        progress.Save();

        DialogueUI.Instance.ShowMessage("Ofelia", "Vuelve mañana, te tendre el ingrediente espacial listo.");
    }
}
