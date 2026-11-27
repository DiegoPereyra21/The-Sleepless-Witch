using UnityEngine.Localization.Settings;

// Elige el texto según el idioma del juego
public static class GameText
{
    public static string Pick(string spanish, string english)
    {
        var locale = LocalizationSettings.SelectedLocale;
        return locale != null && locale.Identifier.Code != "es" ? english : spanish;
    }
}
