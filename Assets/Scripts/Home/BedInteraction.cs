using UnityEngine;

[RequireComponent(typeof(DayTransition))]
public class BedInteraction : TriggerInteractable
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private PotionDisplayArea potionDisplayArea;
    private DayTransition dayTransition;

    private void Awake() => dayTransition = GetComponent<DayTransition>();

    public override string Prompt => Localized("Acostarse", "Go to bed");

    //para que no pueda ir a dormir sin haber salido nunca

    public override void Interact(PlayerInteractor player)
    {
        if (!GameProgressManager.Instance.HasBeenOutsideThisCycle)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "No puedo acostarme ahora, tengo que recolectar.");
            return;
        }

        if (HomeStorage.Instance.RemoveCraftedPotion(GameDatabase.Instance.SleepPotion))
        {
            questManager.ProcessPendingDeliveries();
            questManager.Save();
            HomeStorage.Instance.Save();
            dayTransition.PlayWinEnding();
            return;
        }

        if (GameProgressManager.Instance.CurrentDay >= 7)
        {
            questManager.ProcessPendingDeliveries();
            questManager.Save();
            dayTransition.PlayLoseEnding();
            return;
        }

        int moneyBeforeSleep = GameProgressManager.Instance.Money;

        HomeStorage.Instance.ReturnCauldronToPantry(); //lo que quedó en el caldero vuelve a su mesa
        questManager.ProcessPendingDeliveries();
        potionDisplayArea.ClearAll();
        GameProgressManager.Instance.Sleep();
        GameSaveSystem.SaveAll(questManager);
        dayTransition.PlayDayIntro(GameProgressManager.Instance.CurrentDay, moneyBeforeSleep);
    }

    private void Start()
    {
        if (GameProgressManager.Instance.ShouldSpawnAtDoor) return;

        if (GameProgressManager.Instance.ConsumeWelcomeFade())
            dayTransition.PlayDayIntro(GameProgressManager.Instance.CurrentDay, GameProgressManager.Instance.Money);
    }
}
