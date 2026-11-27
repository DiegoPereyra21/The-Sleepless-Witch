using UnityEngine;

// Arma los modelos que se muestran en la mesa: quietos, a una misma escala y apoyados en su origen
public static class CraftingModels
{
    public static Transform Create(ItemKey key, Transform parent, float size)
    {
        GameObject model = key.Item.GetModel(key.State);

        if (model == null)
        {
            Debug.LogWarning($"[CraftingModels] '{key}' no tiene modelo asignado.", parent);
            return null;
        }

        return Create(model, key.DisplayName, parent, size * key.Item.GetTableScale(key.State));
    }

    // size es la medida de su lado más largo
    public static Transform Create(GameObject model, string name, Transform parent, float size)
    {
        Transform root = new GameObject(name).transform;
        Transform instance = Object.Instantiate(model, root).transform;
        instance.localPosition = Vector3.zero;
        instance.localRotation = Quaternion.identity;

        DisableBehaviour(instance.gameObject);

        if (TryGetBounds(instance, out Bounds bounds))
        {
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float factor = largest > 0.0001f ? size / largest : 1f;

            instance.localScale *= factor;
            instance.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * factor;
        }

        root.SetParent(parent, false);
        SetLayer(root, parent.gameObject.layer);
        return root;
    }

    // Apaga física y detección: al modelo lo detecta el lugar que lo contiene
    private static void DisableBehaviour(GameObject model)
    {
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            Object.Destroy(body);
        }

        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        // Cada uno tiraría su propio rayo en cada frame; en la mesa señala CraftingPointer
        foreach (HoverOutline hover in model.GetComponentsInChildren<HoverOutline>(true))
            hover.enabled = false;
    }

    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;

            if (found) bounds.Encapsulate(renderer.bounds);
            else bounds = renderer.bounds;
            found = true;
        }

        return found;
    }

    private static void SetLayer(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        foreach (Transform child in target)
            SetLayer(child, layer);
    }
}
