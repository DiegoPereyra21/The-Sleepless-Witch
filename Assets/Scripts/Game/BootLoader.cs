using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

// Primera escena del juego: espera lo necesario y pasa al menú
public class BootLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainMenu";

    private IEnumerator Start()
    {
        // Así el menú aparece ya traducido
        yield return LocalizationSettings.InitializationOperation;

        SceneManager.LoadScene(nextSceneName);
    }
}
