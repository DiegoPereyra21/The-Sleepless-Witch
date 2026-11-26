using UnityEngine;
using Game.Collectibles;

public class FairyHover : MonoBehaviour
{
    [Header("Reposo")]
    [Tooltip("Cuánto se aleja como mucho del lugar donde se calmó")]
    [SerializeField] private float idleRadius = 2.5f;
    [SerializeField] private Vector2 idleSpeed = new Vector2(0.8f, 2.4f);
    [SerializeField] private float idleAcceleration = 3.5f;
    [Tooltip("Cuánto serpentea en lugar de ir derecho")]
    [SerializeField] private float idleCurve = 0.8f;
    [Tooltip("Cuánto puede bajar (x) y subir (y) respecto de su altura normal")]
    [SerializeField] private Vector2 idleHeightRange = new Vector2(-0.4f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float pauseChance = 0.35f;
    [SerializeField] private Vector2 pauseDuration = new Vector2(0.4f, 1.4f);
    [SerializeField, Range(0f, 1f)] private float twirlChance = 0.2f;
    [SerializeField] private float twirlDuration = 0.8f;

    [Header("Aleteo")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float flutterAmount = 0.12f;

    [Header("Huida")]
    [SerializeField] private float detectionRadius = 9f;
    [Tooltip("Distancia a la que se calma y deja de escapar")]
    [SerializeField] private float safeRadius = 14f;
    [Tooltip("0 = velocidad de caminar del jugador, 1 = velocidad de correr")]
    [SerializeField, Range(0.05f, 0.95f)] private float fleeSpeedBlend = 0.4f;
    [SerializeField] private float fleeAcceleration = 70f;
    [Tooltip("Cómo frena al calmarse: más bajo, planea más")]
    [SerializeField] private float calmDeceleration = 30f;
    [Tooltip("Grados que zigzaguea mientras escapa")]
    [SerializeField, Range(0f, 25f)] private float fleeWeave = 15f;
    [Tooltip("Hasta dónde se puede alejar de donde apareció")]
    [SerializeField] private float maxFleeDistance = 40f;

    [Header("Rotación")]
    [SerializeField] private float turnSmoothness = 6f;
    [SerializeField] private float maxPitch = 20f;
    [SerializeField] private float maxBank = 25f;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask groundLayer = 1 << 7;
    [SerializeField] private LayerMask obstacleLayers = ~((1 << 2) | (1 << 6));
    [SerializeField] private float bodyRadius = 0.4f;
    [SerializeField] private float lookAhead = 2.5f;
    [SerializeField] private float maxClimb = 2f;

    // ángulos a probar respecto de la dirección de escape, del mejor al peor
    private static readonly float[] escapeAngles = { 0f, 25f, 50f, 75f, 100f, 130f };
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private Vector3 anchor; // posición real, sin el aleteo
    private Vector3 home;
    private Vector3 velocity; // solo horizontal
    private Vector3 idleCenter;
    private Vector3 idleTarget;
    private Quaternion facing;
    private float verticalVelocity;
    private float hoverHeight;
    private float idleHeight;
    private float cruiseSpeed;
    private float targetTimer;
    private float pauseTimer;
    private float twirlTime;
    private float twirlSide;
    private float preferredSide;
    private float seed;
    private bool fleeing;
    private bool caught;

    private PlayerMovement player;
    private Collectible collectible;

    private void Start()
    {
        anchor = transform.position;
        home = anchor;
        idleCenter = anchor;
        facing = transform.rotation;
        seed = Random.Range(0f, 100f);
        preferredSide = Random.value < 0.5f ? -1f : 1f;
        twirlTime = twirlDuration;

        player = FindAnyObjectByType<PlayerMovement>();
        collectible = GetComponent<Collectible>();
        hoverHeight = TryGetGroundHeight(anchor, out float ground) ? anchor.y - ground : 0f;

        PickIdleTarget();
    }

    private void Update()
    {
        // atrapada: se queda quieta para que Collectible haga su animación
        if (caught || (collectible != null && collectible.IsCollected)) return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Vector3 previousVelocity = velocity;

        UpdateFleeState();

        if (fleeing) UpdateFlee(deltaTime);
        else UpdateIdle(deltaTime);

        Move(deltaTime);
        UpdateHeight(deltaTime);

        transform.position = anchor + Flutter();
        UpdateRotation(previousVelocity, deltaTime);
    }

    public void Catch()
    {
        caught = true;
    }

    private void UpdateFleeState()
    {
        bool wasFleeing = fleeing;

        if (player != null && GameProgressManager.Instance.IsNightActive)
        {
            float distance = FlatOffsetFromPlayer().magnitude;

            if (distance < detectionRadius) fleeing = true;
            else if (distance > safeRadius) fleeing = false;
        }
        else
        {
            fleeing = false;
        }

        if (wasFleeing && !fleeing)
        {
            idleCenter = anchor;
            pauseTimer = 0f;
            PickIdleTarget();
        }
    }

    private void UpdateFlee(float deltaTime)
    {
        twirlTime = twirlDuration;

        float weave = (Mathf.PerlinNoise(Time.time * 1.3f, seed) - 0.5f) * 2f * fleeWeave;
        Vector3 away = Quaternion.Euler(0f, weave, 0f) * FlatOffsetFromPlayer().normalized;

        Vector3 targetVelocity = Vector3.zero;
        if (TryGetEscapeDirection(away, out Vector3 direction))
            targetVelocity = direction * Mathf.Lerp(player.WalkSpeed, player.RunSpeed, fleeSpeedBlend);

        velocity = Vector3.MoveTowards(velocity, targetVelocity, fleeAcceleration * deltaTime);
    }

    private void UpdateIdle(float deltaTime)
    {
        // todavía viene con envión: planea hasta frenar y se queda por esa zona
        if (velocity.magnitude > idleSpeed.y * 1.5f)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, calmDeceleration * deltaTime);
            idleCenter = anchor;
            PickIdleTarget();
            return;
        }

        Vector3 targetVelocity = Vector3.zero;

        if (pauseTimer > 0f)
        {
            pauseTimer -= deltaTime;
            if (pauseTimer <= 0f) PickIdleTarget();
        }
        else
        {
            Vector3 toTarget = idleTarget - anchor;
            toTarget.y = 0f;
            targetTimer -= deltaTime;

            if (toTarget.magnitude < 0.35f || targetTimer <= 0f)
            {
                FinishIdleMove();
            }
            else
            {
                float curve = (Mathf.PerlinNoise(seed, Time.time * 0.6f) - 0.5f) * 2f * idleCurve;
                Vector3 direction = toTarget.normalized;
                direction += Vector3.Cross(Vector3.up, direction) * curve;

                // afloja al llegar
                float speed = cruiseSpeed * Mathf.Clamp(toTarget.magnitude, 0.3f, 1f);
                targetVelocity = direction.normalized * speed;
            }
        }

        velocity = Vector3.MoveTowards(velocity, targetVelocity, idleAcceleration * deltaTime);
    }

    private void FinishIdleMove()
    {
        if (Random.value < twirlChance)
        {
            twirlTime = 0f;
            twirlSide = Random.value < 0.5f ? -1f : 1f;
        }

        if (Random.value < pauseChance)
            pauseTimer = Random.Range(pauseDuration.x, pauseDuration.y);
        else
            PickIdleTarget();
    }

    private void PickIdleTarget()
    {
        cruiseSpeed = Random.Range(idleSpeed.x, idleSpeed.y);
        idleHeight = Random.Range(idleHeightRange.x, idleHeightRange.y);
        targetTimer = 4f;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 point = Random.insideUnitCircle * idleRadius;
            Vector3 candidate = idleCenter + new Vector3(point.x, 0f, point.y);
            candidate.y = anchor.y;

            Vector3 path = candidate - anchor;
            if (path.magnitude < idleRadius * 0.4f) continue;
            if (!TryGetGroundHeight(candidate, out float ground)) continue;

            candidate.y = ground + hoverHeight;
            if (candidate.y - anchor.y > maxClimb) continue;

            path = candidate - anchor;
            if (IsBlocked(path.normalized, path.magnitude, out _)) continue;

            idleTarget = candidate;
            return;
        }

        // sin lugar libre: flota donde está un rato
        idleTarget = anchor;
        pauseTimer = pauseDuration.x;
    }

    private void Move(float deltaTime)
    {
        if (velocity.sqrMagnitude < 0.000001f) return;

        Vector3 step = velocity * deltaTime;

        if (IsBlocked(step.normalized, step.magnitude, out Vector3 normal))
        {
            // resbala por el costado del obstáculo en vez de frenar en seco
            normal.y = 0f;
            velocity = Vector3.ProjectOnPlane(velocity, normal.normalized);
            step = velocity * deltaTime;

            if (step == Vector3.zero || IsBlocked(step.normalized, step.magnitude, out _))
            {
                velocity = Vector3.zero;
                if (!fleeing) PickIdleTarget();
                return;
            }
        }

        anchor += step;
    }

    private void UpdateHeight(float deltaTime)
    {
        if (!TryGetGroundHeight(anchor, out float ground)) return;

        float height = hoverHeight;
        if (!fleeing)
            height = Mathf.Max(hoverHeight + idleHeight, Mathf.Min(hoverHeight, 0.5f));

        float smoothTime = fleeing ? 0.15f : 0.6f;
        anchor.y = Mathf.SmoothDamp(anchor.y, ground + height, ref verticalVelocity, smoothTime, Mathf.Infinity, deltaTime);
    }

    // pequeño vaivén irregular para que nunca quede clavada en el aire
    private Vector3 Flutter()
    {
        float time = Time.time;

        float x = Mathf.PerlinNoise(time * 1.1f, seed + 10f) - 0.5f;
        float z = Mathf.PerlinNoise(seed + 20f, time * 1.1f) - 0.5f;
        float y = Mathf.Sin(time * bobSpeed + seed) * bobHeight
                + Mathf.Sin(time * bobSpeed * 3.7f + seed) * bobHeight * 0.25f;

        // al girar sobre sí misma sube un poquito
        y += Mathf.Sin(TwirlProgress() * Mathf.PI) * 0.25f;

        return new Vector3(x * flutterAmount * 2f, y, z * flutterAmount * 2f);
    }

    private void UpdateRotation(Vector3 previousVelocity, float deltaTime)
    {
        float speed = velocity.magnitude;
        float yaw = facing.eulerAngles.y;

        if (speed > 0.3f)
        {
            yaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
        }
        else if (player != null)
        {
            // cuando flota quieta, mira con curiosidad al jugador si lo tiene cerca
            Vector3 toPlayer = -FlatOffsetFromPlayer();
            if (toPlayer.magnitude < safeRadius * 1.5f)
                yaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
        }

        float pitch = Mathf.Clamp01(speed / 8f) * maxPitch;

        float turn = Vector3.SignedAngle(previousVelocity, velocity, Vector3.up) / deltaTime;
        float bank = Mathf.Clamp(-turn * 0.15f, -maxBank, maxBank);
        bank += Mathf.Sin(Time.time * bobSpeed * 0.7f + seed) * 6f;

        facing = Quaternion.Slerp(facing, Quaternion.Euler(pitch, yaw, bank), turnSmoothness * deltaTime);

        twirlTime = Mathf.Min(twirlTime + deltaTime, twirlDuration);
        float spin = Mathf.SmoothStep(0f, 360f, TwirlProgress()) * twirlSide;

        transform.rotation = Quaternion.Euler(0f, spin, 0f) * facing;
    }

    private float TwirlProgress()
    {
        return Mathf.Clamp01(twirlTime / twirlDuration);
    }

    private Vector3 FlatOffsetFromPlayer()
    {
        Vector3 offset = anchor - player.transform.position;
        offset.y = 0f;
        return offset;
    }

    private bool TryGetEscapeDirection(Vector3 away, out Vector3 direction)
    {
        foreach (float escapeAngle in escapeAngles)
        {
            // primero del lado por el que ya venía esquivando, para que no zigzaguee
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? preferredSide : -preferredSide;
                direction = Quaternion.Euler(0f, escapeAngle * side, 0f) * away;

                if (!CanFleeTowards(direction)) continue;

                if (escapeAngle > 0f) preferredSide = side;
                return true;
            }
        }

        direction = Vector3.zero;
        return false;
    }

    private bool CanFleeTowards(Vector3 direction)
    {
        Vector3 target = anchor + direction * lookAhead;

        Vector3 fromHome = target - home;
        fromHome.y = 0f;
        Vector3 currentFromHome = anchor - home;
        currentFromHome.y = 0f;

        bool goingFarther = fromHome.sqrMagnitude > currentFromHome.sqrMagnitude;
        if (goingFarther && fromHome.magnitude > maxFleeDistance) return false;

        if (!TryGetGroundHeight(target, out float ground)) return false;

        target.y = ground + hoverHeight;
        if (target.y - anchor.y > maxClimb) return false;

        Vector3 path = target - anchor;
        return !IsBlocked(path.normalized, path.magnitude, out _);
    }

    private bool IsBlocked(Vector3 direction, float distance, out Vector3 normal)
    {
        int count = Physics.SphereCastNonAlloc(anchor, bodyRadius, direction, hitBuffer, distance, obstacleLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];

            if (hit.distance <= 0f) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            // el piso caminable no cuenta como obstáculo
            if (hit.normal.y > 0.7f) continue;

            normal = hit.normal;
            return true;
        }

        normal = Vector3.zero;
        return false;
    }

    private bool TryGetGroundHeight(Vector3 position, out float height)
    {
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 40f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(Application.isPlaying ? idleCenter : transform.position, idleRadius);
    }
}
