using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityURP.Outline;

public class InteractableOutline : MonoBehaviour
{
    private const float PulsePeriod = 0.9f;
    private const float PulseVisibleRatio = 0.6f;

    [SerializeField] private OutlineRenderer outlineRenderer;

    // El paquete no expone la lista de mallas que resalta
    private static readonly FieldInfo RenderersField =
        typeof(OutlineRenderer).GetField("_outlineRenderers", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool highlighted;
    private bool pulsing;

    private void Awake()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool value)
    {
        highlighted = value;
        Apply();
    }

    // Caja que envuelve las mallas resaltadas
    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        if (outlineRenderer == null || !(RenderersField?.GetValue(outlineRenderer) is List<Renderer> renderers))
            return false;

        bool found = false;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            if (found) bounds.Encapsulate(r.bounds);
            else bounds = r.bounds;
            found = true;
        }

        return found;
    }

    public void SetPulsing(bool value)
    {
        pulsing = value;
        Apply();
    }

    private void Update()
    {
        if (pulsing) Apply();
    }

    private void Apply()
    {
        if (outlineRenderer == null) return;

        bool visible = highlighted
            || (pulsing && Mathf.Repeat(Time.unscaledTime, PulsePeriod) < PulsePeriod * PulseVisibleRatio);

        if (outlineRenderer.enabled != visible)
            outlineRenderer.enabled = visible;
    }
}