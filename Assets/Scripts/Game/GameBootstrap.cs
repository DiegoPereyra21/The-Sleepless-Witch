using UnityEngine;

// Crea los sistemas persistentes antes de la primera escena,
// así se puede dar Play desde cualquiera.
public static class GameBootstrap
{
    // Tiene que estar en Assets/Resources/Systems.prefab
    private const string SystemsPath = "Systems";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSystems()
    {
        GameObject prefab = Resources.Load<GameObject>(SystemsPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameBootstrap] No se encontró 'Resources/{SystemsPath}.prefab'.");
            return;
        }

        GameObject systems = Object.Instantiate(prefab);
        systems.name = SystemsPath;
        Object.DontDestroyOnLoad(systems);
    }

    // Con la primera escena cargada, el motor de audio ya está inicializado
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySettings()
    {
        AudioSettingsUI.ApplySavedVolumes();
    }
}
