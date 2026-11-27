using System;
using System.Collections;
using UnityEngine;

// Secuencia de preparar: revolver, resolver la receta y mostrar la poción
public class CauldronBrewer : MonoBehaviour, ICraftingPart
{
    [SerializeField] private CauldronLadle ladle;
    [SerializeField] private PotionShowcase showcase;

    public event Action<PotionData> PotionCrafted;

    private CraftingService service;
    private bool brewing;

    public void Bind(CraftingService craftingService) => service = craftingService;

    private void OnEnable() => ladle.Clicked += Brew;

    private void OnDisable()
    {
        ladle.Clicked -= Brew;

        if (!brewing) return;

        brewing = false;
        service.EndBusy();
    }

    private void Brew()
    {
        if (brewing || service == null || !service.CanBrew()) return;

        StartCoroutine(BrewRoutine());
    }

    private IEnumerator BrewRoutine()
    {
        brewing = true;
        service.BeginBusy();

        yield return ladle.Stir();

        PotionData potion = service.Brew();
        if (potion != null)
            yield return showcase.Play(potion);

        brewing = false;
        service.EndBusy();

        if (potion != null)
            PotionCrafted?.Invoke(potion);
    }
}
