using UnityEngine;
using UnityEngine.InputSystem;

public enum CraftingZone
{
    Left,
    Center,
    Right
}

// Las 3 vistas del crafteo: el caldero al centro y una mesa a cada lado.
// Se navega con la acción Move (A/D, flechas, stick) o con las flechas en pantalla.
public class CraftingCameraRail : CraftingView
{
    [SerializeField] private PlayerInput playerInput;

    [Header("Vistas laterales")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;
    [Tooltip("Mesa que se ve en la vista izquierda")]
    [SerializeField] private WorkTable leftTable;
    [Tooltip("Mesa que se ve en la vista derecha")]
    [SerializeField] private WorkTable rightTable;

    [Tooltip("Cuánto hay que inclinar Move para cambiar de zona.")]
    [SerializeField, Range(0.1f, 1f)] private float inputThreshold = 0.5f;

    public CraftingZone CurrentZone { get; private set; } = CraftingZone.Center;
    // Vista a la que apunta el tutorial (la flecha que lleva hacia ahí titila)
    public CraftingZone? Hint { get; private set; }
    private InputAction moveAction;
    private int lastDirection;

    private void Awake()
    {
        moveAction = playerInput.actions["Move"];

        if (GetComponent<CraftingNavArrows>() == null)
            gameObject.AddComponent<CraftingNavArrows>();
    }

    public override void Activate()
    {
        base.Activate(); // muestra la vista del caldero
        CurrentZone = CraftingZone.Center;
        lastDirection = ReadDirection();
    }

    private void Update()
    {
        if (!IsActive || Time.timeScale <= 0f) return;

        int direction = ReadDirection();
        if (direction != 0 && direction != lastDirection && !IsBusy)
            Move(direction);

        lastDirection = direction;
    }

    public override bool IsPointerOverUI => CraftingNavArrows.PointerOverUI;

    public override string ControlsHint => "Flechas de la pantalla (o A y D): cambiar de mesa";

    public override string DirectionsTo(TableType table)
    {
        string tableName = table == TableType.Mortar ? "del mortero" : "de la tabla de picar";
        return $"Está en la mesa {tableName}: hacé click en la flecha de la pantalla para ir hasta ahí.";
    }

    public override void PointTo(TableType? table)
    {
        Hint = null;
        if (table == null) return;

        Hint = leftTable != null && leftTable.Table == table ? CraftingZone.Left : CraftingZone.Right;
    }

    public string LabelFor(CraftingZone zone)
    {
        if (zone == CraftingZone.Center) return "Caldero";

        WorkTable table = zone == CraftingZone.Left ? leftTable : rightTable;
        if (table == null) return zone == CraftingZone.Left ? "Izquierda" : "Derecha";

        return table.Table == TableType.Mortar ? "Mortero" : "Tabla de picar";
    }

    public void Navigate(int direction)
    {
        if (!IsActive || IsBusy || Time.timeScale <= 0f) return;
        Move(direction);
    }

    public Transform GetAnchor(CraftingZone zone)
    {
        switch (zone)
        {
            case CraftingZone.Left: return leftAnchor;
            case CraftingZone.Right: return rightAnchor;
            default: return MainAnchor;
        }
    }

    private int ReadDirection()
    {
        float x = moveAction.ReadValue<Vector2>().x;
        if (x > inputThreshold) return 1;
        if (x < -inputThreshold) return -1;
        return 0;
    }

    private void Move(int direction)
    {
        CraftingZone next = (CraftingZone)Mathf.Clamp((int)CurrentZone + direction, 0, 2);
        if (next == CurrentZone) return;

        CurrentZone = next;
        Show(GetAnchor(next));
    }
}
