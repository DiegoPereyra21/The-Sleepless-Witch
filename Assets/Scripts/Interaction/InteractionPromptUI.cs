using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Punto central en 1ra persona y cartel "E + acción". Lo crea solo PlayerInteractor.
public class InteractionPromptUI : MonoBehaviour
{
    private static readonly Color Background = new Color(0.08f, 0.05f, 0.12f, 0.8f);
    private static readonly Color Gold = new Color(1f, 0.84f, 0.29f, 1f);

    private PlayerInteractor interactor;
    private PlayerMovement movement;
    private UIDocument document;
    private VisualElement container;
    private VisualElement dot;
    private VisualElement prompt;
    private Label promptText;
    private bool failed;

    private void Awake()
    {
        interactor = GetComponent<PlayerInteractor>();
        movement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (failed) return;

        if (container == null)
        {
            TryBuild();
            return;
        }

        bool playing = Time.timeScale > 0f;
        Interactable target = interactor.Captured != null ? interactor.Captured : interactor.Focused;

        Show(dot, playing && interactor.IsLooking && movement.IsFollowView && interactor.Captured == null);
        Show(prompt, playing && target != null);

        if (target == null) return;

        promptText.text = target.Prompt;
        // En la mesa de crafteo el cartel va abajo para no tapar la vista
        prompt.parent.style.top = Length.Percent(interactor.Captured != null ? 90f : 58f);
    }

    private void TryBuild()
    {
        if (document == null)
        {
            // Reutiliza el PanelSettings de cualquier UI que ya exista en la escena
            PanelSettings settings = FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(d => d.panelSettings)
                .FirstOrDefault(p => p != null);

            if (settings == null)
            {
                failed = true;
                return;
            }

            // Suelto en la escena: si colgara del player heredaría su escala
            GameObject go = new GameObject("InteractionPromptUI");
            document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.sortingOrder = 40;
        }

        VisualElement root = document.rootVisualElement;
        if (root == null) return; // se reintenta el próximo frame

        root.pickingMode = PickingMode.Ignore;

        container = new VisualElement { pickingMode = PickingMode.Ignore };
        Stretch(container);

        dot = new VisualElement { pickingMode = PickingMode.Ignore };
        dot.style.position = Position.Absolute;
        dot.style.left = Length.Percent(50);
        dot.style.top = Length.Percent(50);
        dot.style.width = 6;
        dot.style.height = 6;
        dot.style.marginLeft = -3;
        dot.style.marginTop = -3;
        dot.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
        Round(dot, 3);

        // Fila de ancho completo para centrar el cartel
        VisualElement row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.style.position = Position.Absolute;
        row.style.left = 0;
        row.style.right = 0;
        row.style.alignItems = Align.Center;

        prompt = new VisualElement { pickingMode = PickingMode.Ignore };
        prompt.style.flexDirection = FlexDirection.Row;
        prompt.style.alignItems = Align.Center;
        prompt.style.backgroundColor = Background;
        prompt.style.paddingLeft = 10;
        prompt.style.paddingRight = 16;
        prompt.style.paddingTop = 8;
        prompt.style.paddingBottom = 8;
        Round(prompt, 12);

        Label key = CreateLabel("E");
        key.style.color = Background;
        key.style.backgroundColor = Gold;
        key.style.width = 32;
        key.style.height = 32;
        key.style.unityTextAlign = TextAnchor.MiddleCenter;
        key.style.marginRight = 10;
        Round(key, 6);

        promptText = CreateLabel(string.Empty);
        promptText.style.color = Color.white;

        prompt.Add(key);
        prompt.Add(promptText);
        row.Add(prompt);
        container.Add(dot);
        container.Add(row);
        root.Add(container);

        Show(dot, false);
        Show(prompt, false);
    }

    private void OnDestroy()
    {
        if (document != null) Destroy(document.gameObject);
    }

    private static Label CreateLabel(string text)
    {
        Label label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = 22;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 0;
        label.style.marginBottom = 0;
        label.style.paddingTop = 0;
        label.style.paddingBottom = 0;
        return label;
    }

    private static void Show(VisualElement element, bool visible) =>
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

    private static void Stretch(VisualElement element)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.right = 0;
        element.style.top = 0;
        element.style.bottom = 0;
    }

    private static void Round(VisualElement element, float radius)
    {
        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
    }
}
