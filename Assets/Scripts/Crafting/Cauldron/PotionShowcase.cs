using System.Collections;
using UnityEngine;

// Presenta la poción recién hecha sobre el caldero y la hace desaparecer
public class PotionShowcase : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float potionSize = 1.3f;
    [SerializeField] private float duration = 2.5f;
    [SerializeField] private float swayAngle = 12f;
    [SerializeField] private float swaySpeed = 3f;
    [SerializeField, Min(0.01f)] private float shrinkDuration = 0.4f;

    private Transform potionVisual;

    public IEnumerator Play(PotionData potion)
    {
        if (potion.Model != null)
            potionVisual = CraftingModels.Create(potion.Model, potion.DisplayName, transform, potionSize);

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            if (potionVisual != null)
                potionVisual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * swaySpeed) * swayAngle);
            yield return null;
        }

        for (float time = 0f; time < shrinkDuration && potionVisual != null; time += Time.deltaTime)
        {
            potionVisual.localScale = Vector3.one * (1f - time / shrinkDuration);
            yield return null;
        }

        Clear();
    }

    private void OnDisable() => Clear();

    private void Clear()
    {
        if (potionVisual != null) Destroy(potionVisual.gameObject);
        potionVisual = null;
    }
}
