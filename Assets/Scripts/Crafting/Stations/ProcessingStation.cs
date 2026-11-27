using System.Collections;
using UnityEngine;

// Mortero o tabla: se le suelta un ingrediente crudo y lo devuelve procesado a la despensa
public class ProcessingStation : MonoBehaviour, IPointerTarget, IDropTarget, ICraftingPart
{
    [SerializeField] private TableType table;
    [SerializeField] private StationAnimation toolAnimation;
    [SerializeField] private InteractableOutline outline;
    [SerializeField] private AK.Wwise.Event processSound;

    [Header("Ingrediente sobre la estación")]
    [SerializeField] private Transform workPoint;
    [SerializeField, Min(0.05f)] private float itemSize = 0.55f;
    [Tooltip("Activado: al terminar se ve el resultado un momento. Desactivado: el ingrediente se va deshaciendo")]
    [SerializeField] private bool showResult = true;
    [SerializeField, Min(0f)] private float resultTime = 0.45f;

    private CraftingService service;
    private ItemData processing;
    private Transform visual;

    public TableType Table => table;
    public InteractableOutline Outline => outline;

    public string HoverText => table == TableType.Mortar
        ? GameText.Pick("Mortero: arrastrá algo para molerlo", "Mortar: drag something here to grind it")
        : GameText.Pick("Tabla: arrastrá algo para cortarlo", "Cutting board: drag something here to chop it");

    public void Bind(CraftingService craftingService) => service = craftingService;

    public void SetHovered(bool hovered)
    {
        if (outline != null) outline.SetHighlighted(hovered);
    }

    public void Drop(ItemKey key)
    {
        if (processing != null || !service.TryStartProcessing(key, table)) return;

        StartCoroutine(ProcessRoutine(key.Item));
    }

    private IEnumerator ProcessRoutine(ItemData item)
    {
        processing = item;
        service.BeginBusy();

        visual = CraftingModels.Create(ItemKey.Raw(item), workPoint, itemSize);

        if (processSound != null && processSound.IsValid())
            processSound.Post(gameObject);

        yield return toolAnimation.Play(progress =>
        {
            if (!showResult && visual != null)
                visual.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, progress);
        });

        ClearVisual();

        if (showResult)
        {
            visual = CraftingModels.Create(ItemKey.Processed(item), workPoint, itemSize);
            yield return new WaitForSeconds(resultTime);
            ClearVisual();
        }

        processing = null;
        service.FinishProcessing(item);
        service.EndBusy();
    }

    // Si la mesa se apaga a mitad de camino, el ingrediente vuelve crudo
    private void OnDisable()
    {
        if (processing == null) return;

        ClearVisual();
        toolAnimation.ResetPose();
        service.CancelProcessing(processing);
        service.EndBusy();
        processing = null;
    }

    private void ClearVisual()
    {
        if (visual != null) Destroy(visual.gameObject);
        visual = null;
    }
}
