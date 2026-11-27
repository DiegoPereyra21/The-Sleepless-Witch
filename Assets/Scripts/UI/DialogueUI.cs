using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private float fadeDuration = 0.3f;

    private VisualElement root;
    private Label nameLabel;
    private Label textLabel;
    private Coroutine activeRoutine;
    private bool isShowing;
    private void Awake()
    {
        Instance = this;

        VisualElement uiRoot = GetComponent<UIDocument>().rootVisualElement;
        root = uiRoot.Q<VisualElement>("ConversationPanel");
        nameLabel = root.Q<Label>("ConversationName");
        textLabel = root.Q<Label>("Conversation");

        root.style.display = DisplayStyle.None;
        root.style.opacity = 0f;
    }

    // Durante el tutorial convive con su cartel: se achica para no ocupar media pantalla
    public void SetCompact()
    {
        root.style.width = 780;
        root.style.left = 570;
        root.style.top = 24;
        root.style.minHeight = 0;
        root.style.paddingTop = 8;
        root.style.paddingBottom = 12;
        root.style.paddingLeft = 24;
        root.style.paddingRight = 24;
        root.style.borderTopLeftRadius = 18;
        root.style.borderTopRightRadius = 18;
        root.style.borderBottomLeftRadius = 18;
        root.style.borderBottomRightRadius = 18;
        root.style.wordSpacing = 0;
        root.style.letterSpacing = 0;

        nameLabel.style.fontSize = 24;
        textLabel.style.fontSize = 28;
        textLabel.style.marginTop = 2;
        textLabel.style.letterSpacing = 0;
    }

    public void ShowMessage(string speakerName, string message, float? duration = null)
    {
        if (isShowing) return;

        nameLabel.text = speakerName;
        textLabel.text = message;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine(duration ?? defaultDuration));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        isShowing = true;
        root.style.display = DisplayStyle.Flex;
        yield return Fade(0f, 1f);

        yield return new WaitForSeconds(duration);

        yield return Fade(1f, 0f);
        root.style.display = DisplayStyle.None;
        isShowing = false;
        activeRoutine = null;
    }
    
    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            root.style.opacity = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        root.style.opacity = to;
    }
}