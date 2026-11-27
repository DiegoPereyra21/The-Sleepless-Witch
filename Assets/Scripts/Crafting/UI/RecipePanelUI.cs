using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Receta del pedido pendiente: va tildando lo que ya está en el caldero.
// Mientras se craftea reemplaza al diario de objetivos, que taparía un lado de la mesa.
[RequireComponent(typeof(UIDocument))]
public class RecipePanelUI : MonoBehaviour, ICraftingPart
{
    [SerializeField] private CraftingRoom room;
    [SerializeField] private CraftingPointer pointer;
    [Tooltip("Si se deja vacío se busca en la escena")]
    [SerializeField] private QuestManager questManager;

    private CraftingService service;
    private QuestJournalUI journal;
    private VisualElement panel;
    private Label header;
    private Label title;
    private VisualElement rows;
    private Button previous;
    private Button next;
    private int index;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q<VisualElement>("RecipePanel");
        header = root.Q<Label>("RecipeHeader");
        title = root.Q<Label>("RecipeTitle");
        rows = root.Q<VisualElement>("RecipeRows");
        previous = root.Q<Button>("RecipePrevious");
        next = root.Q<Button>("RecipeNext");

        previous.clicked += ShowPrevious;
        next.clicked += ShowNext;
        panel.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
        panel.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);

        room.OnEntered += Open;
        room.OnExited += Close;

        Close();
    }

    private void OnDisable()
    {
        previous.clicked -= ShowPrevious;
        next.clicked -= ShowNext;
        panel.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
        panel.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);

        room.OnEntered -= Open;
        room.OnExited -= Close;
    }

    public void Bind(CraftingService craftingService)
    {
        service = craftingService;
        service.Pantry.OnChanged += Refresh;
        service.Cauldron.OnChanged += Refresh;
        service.PotionBrewed += HandlePotionBrewed;
    }

    private void OnDestroy()
    {
        if (service == null) return;

        service.Pantry.OnChanged -= Refresh;
        service.Cauldron.OnChanged -= Refresh;
        service.PotionBrewed -= HandlePotionBrewed;
    }

    private void Open()
    {
        if (questManager == null)
            questManager = FindFirstObjectByType<QuestManager>();

        if (journal == null)
            journal = FindFirstObjectByType<QuestJournalUI>();

        if (journal != null)
            journal.SetVisible(false);

        index = 0;
        panel.style.display = DisplayStyle.Flex;
        Refresh();
    }

    private void Close()
    {
        panel.style.display = DisplayStyle.None;
        pointer.IsOverUI = false;

        if (journal != null)
            journal.SetVisible(true);
    }

    private void HandlePointerEnter(PointerEnterEvent evt) => pointer.IsOverUI = true;
    private void HandlePointerLeave(PointerLeaveEvent evt) => pointer.IsOverUI = false;

    private void HandlePotionBrewed(PotionData potion) => Refresh();

    private void ShowPrevious()
    {
        index--;
        Refresh();
    }

    private void ShowNext()
    {
        index++;
        Refresh();
    }

    private void Refresh()
    {
        if (service == null || questManager == null || !room.IsInside) return;

        List<QuestData> pending = QuestRequirements.GetUncoveredQuests(questManager);
        rows.Clear();

        bool several = pending.Count > 1;
        previous.style.display = several ? DisplayStyle.Flex : DisplayStyle.None;
        next.style.display = several ? DisplayStyle.Flex : DisplayStyle.None;

        if (pending.Count == 0)
        {
            header.text = GameText.Pick("Pedidos", "Orders");
            title.text = GameText.Pick("No quedan pociones por preparar", "No potions left to brew");
            return;
        }

        index = (index % pending.Count + pending.Count) % pending.Count;
        QuestData quest = pending[index];

        string order = GameText.Pick("Pedido", "Order");
        header.text = several ? $"{order} {index + 1}/{pending.Count}" : order;
        if (!string.IsNullOrWhiteSpace(quest.VillagerName))
            header.text += $" - {quest.VillagerName}";

        title.text = quest.Potion.DisplayName;
        AddIngredients(quest.Potion);
    }

    private void AddIngredients(PotionData potion)
    {
        Dictionary<ItemKey, int> required = potion.GetRequirements();

        foreach (KeyValuePair<ItemKey, int> pair in required)
        {
            ItemKey key = pair.Key;
            int inCauldron = service.Cauldron.Count(key);
            int missing = pair.Value - inCauldron;
            string text = $"{key.DisplayName}  {inCauldron}/{pair.Value}";

            if (missing == 0)
                AddRow("[x]", text, "row--done");
            else if (missing < 0)
                AddRow("!", $"{text}  {GameText.Pick("(sobra)", "(too many)")}", "row--wrong");
            else if (service.Pantry.Count(key) > 0)
                AddRow("[ ]", text, null);
            else if (key.State == ItemState.Processed && service.Pantry.Count(ItemKey.Raw(key.Item)) > 0)
                AddRow("[ ]", $"{text}  {ProcessHint(key.Item.Table)}", null);
            else
                AddRow("[ ]", $"{text}  (0)", "row--missing");
        }

        // Lo que está en el caldero y esta receta no lleva
        foreach (KeyValuePair<ItemKey, int> pair in service.Cauldron.Contents)
        {
            if (!required.ContainsKey(pair.Key))
                AddRow("!", $"{pair.Key.DisplayName}  {GameText.Pick("no va", "doesn't belong")}", "row--wrong");
        }
    }

    private static string ProcessHint(TableType table) => table == TableType.Mortar
        ? GameText.Pick("(moler)", "(grind)")
        : GameText.Pick("(cortar)", "(chop)");

    private void AddRow(string mark, string text, string modifier)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("row");
        if (modifier != null) row.AddToClassList(modifier);

        Label markLabel = new Label(mark);
        markLabel.AddToClassList("row__mark");

        Label textLabel = new Label(text);
        textLabel.AddToClassList("row__text");

        row.Add(markLabel);
        row.Add(textLabel);
        rows.Add(row);
    }
}
