using System.Collections.Generic;
using UnityEngine;

public class Mailbox : Interactable
{
    private const string SaveKey = "Mailbox_Data";

    [SerializeField] private QuestManager questManager;
    [SerializeField] private List<QuestData> allLetters = new List<QuestData>(); //el catálogo completo, se configura en el Inspector
    [SerializeField] private float interactRadius = 2f;

    private List<QuestData> pendingLetters = new List<QuestData>();

    public int RemainingCount
    {
        get
        {
            int currentDay = GameProgressManager.Instance.CurrentDay;
            int count = 0;
            foreach (QuestData letter in pendingLetters)
                if (letter.AvailableDay <= currentDay) count++;
            return count;
        }
    }

    private void Awake() => Load();

    protected override void OnEnable()
    {
        base.OnEnable();
        GameProgressManager.Instance.OnNightStarted += DiscardExpiredLetters;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameProgressManager.Instance.OnNightStarted -= DiscardExpiredLetters;
    }

    private void DiscardExpiredLetters()
    {
        int currentDay = GameProgressManager.Instance.CurrentDay;
        int before = pendingLetters.Count;
        pendingLetters.RemoveAll(letter => letter.AvailableDay < currentDay);
        if (pendingLetters.Count != before) Save();
    }

    public override string Prompt => Localized("Revisar el buzón", "Check the mailbox");
    public override bool IsInRange(Vector3 playerPosition) => IsWithin(playerPosition, interactRadius);

    public override void Interact(PlayerInteractor player)
    {
        // Cartas que ya están activas (tomadas en una sesión anterior) no tienen que seguir bloqueando la lista
        pendingLetters.RemoveAll(q => questManager.IsQuestActive(q));

        int currentDay = GameProgressManager.Instance.CurrentDay;
        QuestData letter = pendingLetters.Find(q => q.AvailableDay <= currentDay);

        if (letter == null)
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "No tengo más cartas por ahora.");
            return;
        }

        if (questManager.AddQuest(letter))
        {
            pendingLetters.Remove(letter);
            Save();
        }
        else
        {
            DialogueUI.Instance.ShowMessage("Ofelia", "Ya tengo demasiados pedidos, no puedo aceptar más por ahora.");
        }
    }

    private void Save()
    {
        if (TutorialModeFlag.IsActive) return;

        SaveData data = new SaveData();
        foreach (QuestData letter in pendingLetters)
            data.letterIds.Add(letter.Id);

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (TutorialModeFlag.IsActive || !PlayerPrefs.HasKey(SaveKey))
        {
            pendingLetters = new List<QuestData>(allLetters);
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        pendingLetters = new List<QuestData>();
        foreach (string id in data.letterIds)
        {
            QuestData letter = allLetters.Find(q => q.Id == id);
            if (letter != null) pendingLetters.Add(letter);
        }
    }

    [System.Serializable]
    private class SaveData
    {
        public List<string> letterIds = new List<string>();
    }
}