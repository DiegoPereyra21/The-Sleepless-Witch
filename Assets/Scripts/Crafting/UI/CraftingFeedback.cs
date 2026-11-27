using UnityEngine;

// Lo que dice Ofelia según lo que pasa en la mesa
public class CraftingFeedback : MonoBehaviour, ICraftingPart
{
    [SerializeField] private string speaker = "Ofelia";

    private CraftingService service;

    public void Bind(CraftingService craftingService)
    {
        service = craftingService;
        service.ProcessRejected += HandleProcessRejected;
        service.BrewFailed += HandleBrewFailed;
        service.PotionBrewed += HandlePotionBrewed;
    }

    private void OnDestroy()
    {
        if (service == null) return;

        service.ProcessRejected -= HandleProcessRejected;
        service.BrewFailed -= HandleBrewFailed;
        service.PotionBrewed -= HandlePotionBrewed;
    }

    private void HandleProcessRejected(ProcessCheck reason, TableType station)
    {
        bool mortar = station == TableType.Mortar;

        if (reason == ProcessCheck.WrongStation)
        {
            Say(mortar
                ? GameText.Pick("Esto no va en el mortero.", "That doesn't go in the mortar.")
                : GameText.Pick("Esto no va en la tabla de picar.", "That doesn't go on the cutting board."));
        }
        else if (reason == ProcessCheck.NotProcessable)
        {
            Say(mortar
                ? GameText.Pick("Esto no se puede moler.", "I can't grind this.")
                : GameText.Pick("Esto no se puede cortar.", "I can't chop this."));
        }
    }

    private void HandleBrewFailed(BrewFailure reason)
    {
        Say(reason == BrewFailure.EmptyCauldron
            ? GameText.Pick("El caldero está vacío.", "The cauldron is empty.")
            : GameText.Pick("Ninguna receta coincide con los ingredientes en el caldero.", "No recipe matches what's in the cauldron."));
    }

    private void HandlePotionBrewed(PotionData potion) =>
        Say(GameText.Pick($"Creaste: {potion.DisplayName}", $"You brewed: {potion.DisplayName}"));

    private void Say(string message)
    {
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.ShowMessage(speaker, message);
    }
}
