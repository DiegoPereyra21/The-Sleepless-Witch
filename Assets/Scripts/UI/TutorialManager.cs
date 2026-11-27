using System.Collections.Generic;
using System.Linq;
using Game.Collectibles;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    private const string Title = "Tutorial";

    [SerializeField] private StairInteraction downstairs;
    [SerializeField] private DoorExitInteraction doorExit;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private CraftingRoom craftingRoom;
    [SerializeField] private DeliveryInteraction deliveryInteraction;
    [SerializeField] private PlayerCollector playerCollector;
    [SerializeField] private DoorInteraction doorInteraction;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private StairInteraction upstairs;
    [SerializeField] private int questsNeededForNextStep = 3;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private BasketDisplay basketDisplay;

    [Header("Guía visual")]
    [Tooltip("Radio alrededor del player en el que titilan los ingredientes que faltan juntar.")]
    [SerializeField] private float collectibleHighlightRadius = 40f;
    [Tooltip("Cada cuánto se recalcula qué titila y qué indicación mostrar.")]
    [SerializeField] private float guideRefreshInterval = 0.2f;

    private bool inventoryOpenedOnce;
    private bool inventoryClosedMessageShown;
    private bool collectedMessageShown;
    private bool grimoireTipShown;
    private enum Step
    {
        Movement, GoOutside, CheckMailbox, CollectItems,
        CraftingIntro, CraftPotion, Deliver, WaitingToSleep, Done
    }

    private Step currentStep = Step.Movement;
    private bool nightPhaseDone;

    // Guía visual (se busca todo en la escena, no hace falta asignar nada)
    private readonly TutorialHighlighter highlighter = new TutorialHighlighter();
    private Mailbox mailbox;
    private BedInteraction bed;
    private Collectible[] collectibles;
    private float guideTimer;
    private string lastCraftingHint;

    private void Start()
    {
        mailbox = FindFirstObjectByType<Mailbox>();
        bed = FindFirstObjectByType<BedInteraction>();

        DialogueUI.Instance.SetCompact();
        Show("Movete con WASD, mirá con el mouse y usá con E lo que se ilumine. Empezá bajando las escaleras.");
    }

    private void Update()
    {
        if (currentStep == Step.CollectItems && doorInteraction != null)
        {
            bool hasAll = HasAllQuestMaterials();
            doorInteraction.EntryBlocked = !hasAll;
            doorInteraction.BlockedMessage = "Todavía me falta recolectar ingredientes para mis pedidos.";

            if (hasAll && !collectedMessageShown)
            {
                collectedMessageShown = true;
                Show("¡Ya tenés todo! Volvé a la casa para preparar las pociones.");
            }
        }

        if (upstairs != null)
        {
            bool inCraftingPhase = currentStep == Step.CraftingIntro
                || currentStep == Step.CraftPotion
                || currentStep == Step.Deliver;

            if (inCraftingPhase && !AllQuestsDelivered())
            {
                upstairs.EntryBlocked = true;
                upstairs.BlockedMessage = HomeStorage.Instance.CraftedPotions.Count > 0
                    ? "Entrega las pociones antes de acostarte."
                    : "Crea las pociones antes de acostarte.";
            }
            else
            {
                upstairs.EntryBlocked = false;
            }
        }

        guideTimer -= Time.unscaledDeltaTime;
        if (guideTimer <= 0f)
        {
            guideTimer = guideRefreshInterval;
            RefreshGuide();
        }
    }

    private bool HasAllQuestMaterials() => QuestRequirements.HasAllMaterials(questManager, inventory);

    private bool AllQuestsDelivered()
    {
        foreach (QuestData quest in questManager.ActiveQuests)
        {
            if (!questManager.PendingDeliveries.Contains(quest))
                return false;
        }

        return true;
    }

    private void OnEnable()
    {
        if (downstairs != null) downstairs.OnUsed += HandleWentDownstairs;
        if (doorExit != null) doorExit.OnUsed += HandleWentOutside;
        if (questManager != null) questManager.OnQuestsChanged += HandleQuestsChanged;
        if (inventory != null) inventory.OnInventoryChanged += HandleInventoryChanged;
        if (basketDisplay != null) basketDisplay.OnOpened += HandleInventoryOpened;
        if (basketDisplay != null) basketDisplay.OnClosed += HandleInventoryClosed;

        if (craftingRoom != null)
        {
            craftingRoom.OnEntered += HandleEnteredCrafting;
            craftingRoom.Table.PotionCrafted += HandlePotionCrafted;
        }

        if (deliveryInteraction != null)
            deliveryInteraction.OnInteracted += HandleDelivered;

        GameProgressManager.Instance.OnDayStarted += HandleWentInside;
        GameProgressManager.Instance.OnNightTimeExpired += HandleWentInside;
        GameProgressManager.Instance.OnNightStarted += HandleSlept;
    }

    private void OnDisable()
    {
        if (downstairs != null) downstairs.OnUsed -= HandleWentDownstairs;
        if (doorExit != null) doorExit.OnUsed -= HandleWentOutside;
        if (questManager != null) questManager.OnQuestsChanged -= HandleQuestsChanged;
        if (inventory != null) inventory.OnInventoryChanged -= HandleInventoryChanged;
        if (basketDisplay != null) basketDisplay.OnOpened -= HandleInventoryOpened;
        if (basketDisplay != null) basketDisplay.OnClosed -= HandleInventoryClosed;

        if (craftingRoom != null)
        {
            craftingRoom.OnEntered -= HandleEnteredCrafting;
            craftingRoom.Table.PotionCrafted -= HandlePotionCrafted;
        }

        if (deliveryInteraction != null)
            deliveryInteraction.OnInteracted -= HandleDelivered;

        GameProgressManager.Instance.OnDayStarted -= HandleWentInside;
        GameProgressManager.Instance.OnNightTimeExpired -= HandleWentInside;
        GameProgressManager.Instance.OnNightStarted -= HandleSlept;

        highlighter.Clear();
    }

    // ---------------------------------------------------------------- Pasos

    private void HandleWentDownstairs()
    {
        if (currentStep != Step.Movement) return;

        currentStep = Step.GoOutside;
        Show("¡Bien! Salí por la puerta: afuera están el buzón y los ingredientes.");
    }

    private void HandleWentOutside()
    {
        if (currentStep != Step.GoOutside) return;

        currentStep = Step.CheckMailbox;
        Show("Mirá el buzón y pulsá E para aceptar los pedidos. Cada pedido es una poción para preparar.");

        if (playerCollector != null) playerCollector.CollectionBlocked = true;

        if (doorInteraction != null)
        {
            doorInteraction.EntryBlocked = true;
            doorInteraction.BlockedMessage = "Todavía no acepté todos los pedidos del buzón, no puedo entrar sin eso.";
        }
    }

    private void HandleQuestsChanged()
    {
        if (currentStep != Step.CheckMailbox) return;
        if (questManager.ActiveQuests.Count < questsNeededForNextStep) return;

        currentStep = Step.CollectItems;
        Show("Juntá con E los ingredientes que brillan. El diario de objetivos muestra cuáles faltan y con TAB ves tu canasto.");

        if (playerCollector != null) playerCollector.CollectionBlocked = false;
        if (doorInteraction != null) doorInteraction.EntryBlocked = false;
    }

    private void HandleInventoryOpened()
    {
        if (currentStep != Step.CollectItems || inventoryOpenedOnce) return;
        inventoryOpenedOnce = true;

        Show("Este es tu canasto: hacé click en lo que no sirva para tirarlo. Cerralo con TAB.");
    }

    private void HandleInventoryClosed()
    {
        if (currentStep != Step.CollectItems || !inventoryOpenedOnce || inventoryClosedMessageShown) return;
        inventoryClosedMessageShown = true;

        Show("Juntá todo antes de que amanezca: de día los ingredientes se marchitan.");
    }

    // Aviso del grimorio (tecla J): sale una sola vez al juntar el primer ingrediente.
    private void HandleInventoryChanged()
    {
        if (currentStep != Step.CollectItems || grimoireTipShown) return;
        if (inventory.TotalCount <= 0) return;

        grimoireTipShown = true;
        Show("¿No conocés un ingrediente? Pulsá J y mirá la pestaña Compendio del grimorio.");
    }

    // Entrar a la casa cierra la parte de afuera pase lo que pase (te saltees el buzón o no) —
    // así nunca queda trabado esperando algo que el jugador decidió no hacer.
    private void HandleWentInside()
    {
        if (nightPhaseDone) return;
        if (currentStep == Step.Movement || currentStep == Step.GoOutside) return;

        nightPhaseDone = true;
        currentStep = Step.CraftingIntro;
        Show("A preparar las pociones: mirá el caldero de la mesa y pulsá E.");
    }

    private void HandleEnteredCrafting()
    {
        if (currentStep != Step.CraftingIntro) return;

        currentStep = Step.CraftPotion;
        lastCraftingHint = null;
        guideTimer = 0f; // la guía del crafteo arranca en el próximo frame
    }

    private void HandlePotionCrafted(PotionData potion)
    {
        if (currentStep != Step.CraftPotion) return;

        // Si quedan pedidos sin poción, la guía sigue con la próxima receta
        if (QuestRequirements.GetUncoveredQuests(questManager).Count > 0) return;

        currentStep = Step.Deliver;
        Show("¡Pociones listas! Salí de la mesa con E y entregalas en el mostrador.");
    }

    private void HandleDelivered()
    {
        if (currentStep != Step.Deliver) return;
        if (!AllQuestsDelivered()) return;

        currentStep = Step.WaitingToSleep;
        Show("Cobrás los pedidos al dormir. Juntá pronto para el ingrediente especial de tu poción del sueño pesado… o ya vas a ver. Subí y andá a la cama.");
    }

    private void HandleSlept()
    {
        if (currentStep != Step.WaitingToSleep) return;
        FinishTutorial();
    }

    private void FinishTutorial()
    {
        currentStep = Step.Done;
        highlighter.Clear();
        TutorialUI.Instance.Hide();
        TutorialModeFlag.IsActive = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void Show(string text) => TutorialUI.Instance.Show(Title, text);

    // ---------------------------------------------------------------- Guía visual

    // Decide qué titila según el paso actual (y en el crafteo, también qué indicación mostrar).
    private void RefreshGuide()
    {
        switch (currentStep)
        {
            case Step.Movement:
                highlighter.Add(downstairs);
                break;

            case Step.GoOutside:
                highlighter.Add(doorExit);
                break;

            case Step.CheckMailbox:
                highlighter.Add(mailbox);
                break;

            case Step.CollectItems:
                if (collectedMessageShown) highlighter.Add(doorInteraction);
                else HighlightMissingCollectibles();
                break;

            case Step.CraftingIntro:
                highlighter.Add(craftingRoom);
                break;

            case Step.CraftPotion:
                GuideCrafting();
                break;

            case Step.Deliver:
                highlighter.Add(deliveryInteraction);
                break;

            case Step.WaitingToSleep:
                highlighter.Add(upstairs);
                highlighter.Add(bed);
                break;
        }

        if (craftingRoom != null) TutorialUI.Instance.SetCompact(craftingRoom.IsInside);

        highlighter.Apply();
    }

    private void HighlightMissingCollectibles()
    {
        HashSet<ItemData> missing = GetMissingItems();
        if (missing.Count == 0) return;

        if (collectibles == null)
            collectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);

        Vector3 playerPosition = playerCollector != null ? playerCollector.transform.position : Vector3.zero;

        foreach (Collectible collectible in collectibles)
        {
            if (collectible == null || collectible.IsCollected) continue;
            if (collectible.Item == null || !missing.Contains(collectible.Item)) continue;
            if (playerCollector != null && Vector3.Distance(playerPosition, collectible.transform.position) > collectibleHighlightRadius) continue;

            highlighter.Add(collectible);
        }
    }

    // Ítems que piden los pedidos todavía sin poción y que el jugador aún no juntó.
    private HashSet<ItemData> GetMissingItems()
    {
        Dictionary<ItemData, int> needed = new Dictionary<ItemData, int>();

        foreach (QuestData quest in QuestRequirements.GetUncoveredQuests(questManager))
        {
            foreach (KeyValuePair<ItemData, int> pair in QuestRequirements.GetItemTotals(quest.Potion))
            {
                needed.TryGetValue(pair.Key, out int current);
                needed[pair.Key] = current + pair.Value;
            }
        }

        HashSet<ItemData> missing = new HashSet<ItemData>();
        foreach (KeyValuePair<ItemData, int> pair in needed)
        {
            if (QuestRequirements.GetOwned(pair.Key, inventory) < pair.Value)
                missing.Add(pair.Key);
        }

        return missing;
    }

    // ---------------------------------------------------------------- Guía del crafteo

    private void GuideCrafting()
    {
        if (craftingRoom == null) return;

        if (!craftingRoom.IsInside)
        {
            highlighter.Add(craftingRoom);
            ShowCraftingHint("Mirá el caldero de la mesa y pulsá E para seguir preparando.");
            return;
        }

        // Mientras algo se está moliendo, cortando o revolviendo no hay nada que indicar
        if (craftingRoom.Table.Service == null || craftingRoom.IsBusy) return;

        PotionData target = GetTargetPotion();
        if (target == null)
        {
            ShowCraftingHint("¡Muy bien! Ya tenés todas las pociones.");
            return;
        }

        string instruction = BuildCraftingInstruction(target);

        ShowCraftingHint(instruction);
    }

    private PotionData GetTargetPotion()
    {
        foreach (QuestData quest in QuestRequirements.GetUncoveredQuests(questManager))
        {
            if (quest.Potion != null)
                return quest.Potion;
        }

        return null;
    }

    // Indica el próximo paso para la receta y hace titilar lo que hay que tocar.
    private string BuildCraftingInstruction(PotionData target)
    {
        CraftingTable table = craftingRoom.Table;
        ItemContainer pantry = table.Service.Pantry;
        ItemContainer pot = table.Service.Cauldron;
        Dictionary<ItemKey, int> required = target.GetRequirements();

        // 1) Algo en el caldero que no va (o de más)
        foreach (KeyValuePair<ItemKey, int> pair in pot.Contents)
        {
            required.TryGetValue(pair.Key, out int needed);
            if (pair.Value <= needed) continue;

            foreach (InteractableOutline outline in table.Cauldron.OutlinesOf(pair.Key))
                highlighter.Add(outline);

            return $"Sobra {TutorialUI.Item(pair.Key.DisplayName)} en el caldero: hacé click encima para sacarlo.";
        }

        // 2) El primer ingrediente que falta, en el orden de la receta
        foreach (PotionIngredient ingredient in target.Ingredients)
        {
            if (ingredient == null || ingredient.Item == null) continue;

            ItemKey key = ingredient.Key;
            required.TryGetValue(key, out int total);
            int missing = total - pot.Count(key);
            if (missing <= 0) continue;

            string amount = missing > 1 ? $" (faltan {missing})" : "";

            if (pantry.Count(key) > 0)
            {
                HighlightSlot(key);
                string extra = key.State == ItemState.Raw ? " Va sin procesar." : "";
                return $"Hacé click en {TutorialUI.Item(key.DisplayName)} para echarlo al caldero{amount}.{extra}";
            }

            ItemKey raw = ItemKey.Raw(key.Item);
            if (key.State == ItemState.Processed && pantry.Count(raw) > 0)
            {
                HighlightSlot(raw);
                highlighter.Add(table.FindStation(key.Item.Table));

                string where = key.Item.Table == TableType.Mortar ? "al mortero para molerlo" : "a la tabla de picar para cortarlo";
                return $"Arrastrá {TutorialUI.Item(raw.DisplayName)} {where}{amount}.";
            }

            return $"No te queda {TutorialUI.Item(key.Item.DisplayName)}: salí a recolectarlo.";
        }

        // 3) Está todo: a revolver
        highlighter.Add(table.Ladle);
        return "¡Está todo! Hacé click en el cucharón para preparar la poción.";
    }

    private void HighlightSlot(ItemKey key)
    {
        IngredientSlot slot = craftingRoom.Table.FindSlot(key);
        if (slot == null) return;

        foreach (InteractableOutline outline in slot.Outlines)
            highlighter.Add(outline);
    }

    private void ShowCraftingHint(string text)
    {
        if (text == lastCraftingHint) return;

        lastCraftingHint = text;
        Show(text);
    }
}
