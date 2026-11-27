using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Cartel con el nombre de lo que se señala en la mesa
[RequireComponent(typeof(UIDocument))]
public class PointerTooltipUI : MonoBehaviour
{
    [SerializeField] private CraftingPointer pointer;
    [SerializeField] private Vector2 offset = new Vector2(18f, 18f);

    private Label label;

    private void OnEnable()
    {
        label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("PointerTooltip");
        pointer.HoverTextChanged += Show;
        Show(null);
    }

    private void OnDisable() => pointer.HoverTextChanged -= Show;

    private void Show(string text)
    {
        label.text = text;
        label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        Follow();
    }

    private void Update() => Follow();

    private void Follow()
    {
        if (label.panel == null || Mouse.current == null || string.IsNullOrEmpty(label.text)) return;

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 position = RuntimePanelUtils.ScreenToPanel(label.panel, new Vector2(screen.x, Screen.height - screen.y));

        label.style.left = position.x + offset.x;
        label.style.top = position.y + offset.y;
    }
}
