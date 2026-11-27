using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Cierre en círculo, como en los cuentos: la pantalla se pone negra alrededor de un punto y vuelve a abrirse.
// Quien lo usa decide en cada cuadro cuánto está cerrado.
public class ScreenIris : MonoBehaviour
{
    private const int Segments = 96;
    private const float EdgeSoftness = 5f;
    private const float PanelOrder = 100f;

    private UIDocument document;
    private PanelSettings panelSettings;
    private VisualElement overlay;
    private float amount;
    private Vector3 focus;
    private Vector2 center;
    private float radius;
    private float outerRadius;

    // closed: 0 abierto, 1 todo negro. worldFocus: punto del mundo en el que se centra el círculo
    public void Set(float closed, Vector3 worldFocus)
    {
        amount = Mathf.Clamp01(closed);
        focus = worldFocus;

        // Se arma en el primer uso, antes de que haga falta cerrarlo, para que ya tenga medidas
        if (overlay == null && !TryBuild()) return;
        Refresh();
    }

    // La cámara se mueve después de Update: el centro se recalcula al final del cuadro
    private void LateUpdate()
    {
        if (overlay != null) Refresh();
    }

    private void Refresh()
    {
        Rect area = overlay.contentRect;

        // En pausa se esconde para no tapar el menú ni el libro
        bool visible = amount > 0f && Time.timeScale > 0f && area.width > 0f;
        // Con visibility (y no display) el elemento conserva sus medidas mientras está escondido
        overlay.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        if (!visible) return;

        Camera view = Camera.main;
        center = area.center;
        if (view != null)
        {
            // La cámara ocupa toda la pantalla: la posición relativa vale para cualquier tamaño de interfaz
            Vector3 point = view.WorldToViewportPoint(focus);
            if (point.z > 0f)
                center = new Vector2(area.xMin + point.x * area.width, area.yMin + (1f - point.y) * area.height);
        }

        // Con este radio el círculo deja ver toda la pantalla, esté donde esté el centro
        float open = new Vector2(
            Mathf.Max(center.x - area.xMin, area.xMax - center.x),
            Mathf.Max(center.y - area.yMin, area.yMax - center.y)).magnitude;

        // Cuadrático: se achica rápido de lejos y se demora al cerrarse sobre el personaje
        radius = open * (1f - amount) * (1f - amount);
        outerRadius = open * 1.1f + EdgeSoftness * 2f;
        overlay.MarkDirtyRepaint();
    }

    private bool TryBuild()
    {
        if (document == null)
        {
            // Copia de un PanelSettings existente, en una capa propia por encima de todo el HUD
            PanelSettings source = FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(d => d.panelSettings)
                .FirstOrDefault(p => p != null);
            if (source == null) return false;

            panelSettings = Instantiate(source);
            panelSettings.name = "ScreenIrisPanel";
            panelSettings.sortingOrder = PanelOrder;

            document = new GameObject("ScreenIrisUI").AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
        }

        VisualElement root = document.rootVisualElement;
        if (root == null) return false;

        root.pickingMode = PickingMode.Ignore;

        overlay = new VisualElement { pickingMode = PickingMode.Ignore };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.visibility = Visibility.Hidden;
        overlay.generateVisualContent += Draw;
        root.Add(overlay);
        return true;
    }

    // Un anillo negro alrededor del agujero, lo bastante grande para cubrir la pantalla
    private void Draw(MeshGenerationContext context)
    {
        Color32 solid = Color.black;
        Color32 clear = new Color32(0, 0, 0, 0);

        // Radios de adentro hacia afuera: borde del agujero, fin del suavizado y exterior
        bool closed = radius < 0.5f;
        float[] radii = { radius, radius + Mathf.Min(EdgeSoftness, radius), outerRadius };
        Color32[] colors = { closed ? solid : clear, solid, solid };

        MeshWriteData mesh = context.Allocate(Segments * radii.Length, Segments * (radii.Length - 1) * 6);

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            for (int ring = 0; ring < radii.Length; ring++)
            {
                Vector2 point = center + direction * radii[ring];
                mesh.SetNextVertex(new Vertex { position = new Vector3(point.x, point.y, Vertex.nearZ), tint = colors[ring] });
            }
        }

        for (int i = 0; i < Segments; i++)
        {
            int next = (i + 1) % Segments;
            for (int ring = 0; ring < radii.Length - 1; ring++)
            {
                ushort inner = (ushort)(i * radii.Length + ring);
                ushort outer = (ushort)(inner + 1);
                ushort nextInner = (ushort)(next * radii.Length + ring);
                ushort nextOuter = (ushort)(nextInner + 1);

                mesh.SetNextIndex(inner);
                mesh.SetNextIndex(outer);
                mesh.SetNextIndex(nextOuter);
                mesh.SetNextIndex(inner);
                mesh.SetNextIndex(nextOuter);
                mesh.SetNextIndex(nextInner);
            }
        }
    }

    private void OnDestroy()
    {
        if (document != null) Destroy(document.gameObject);
        if (panelSettings != null) Destroy(panelSettings);
    }
}
