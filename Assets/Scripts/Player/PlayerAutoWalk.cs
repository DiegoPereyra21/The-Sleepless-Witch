using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mueve al personaje solo: recorre una escalera (StairPath) o cruza una puerta (DoorPassage),
// con un cierre en círculo cuando salta de un lugar a otro.
[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAutoWalk : MonoBehaviour
{
    [Tooltip("Velocidad en la escalera (unidades de mundo por segundo)")]
    [SerializeField, Min(1f)] private float speed = 11f;
    [Tooltip("Velocidad al cruzar una puerta")]
    [SerializeField, Min(1f)] private float doorSpeed = 8f;
    [Tooltip("Qué tan rápido gira al caminar")]
    [SerializeField, Min(1f)] private float turnSpeed = 10f;

    [Header("Animación de escalera")]
    [SerializeField, Min(0.1f)] private float upAnimationSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float downAnimationSpeed = 1f;
    [SerializeField, Min(0f)] private float blendTime = 0.2f;

    [Header("Cierre en círculo (segundos)")]
    [Tooltip("Cuánto tarda en cerrarse el círculo")]
    [SerializeField, Min(0.05f)] private float closeTime = 0.35f;
    [Tooltip("Cuánto queda la pantalla en negro")]
    [SerializeField, Min(0f)] private float blackTime = 0.15f;
    [Tooltip("Cuánto tarda en abrirse del otro lado")]
    [SerializeField, Min(0.05f)] private float openTime = 0.4f;

    [Header("Cámara")]
    [Tooltip("Mientras camina solo se ve a Faunita. Apagado se queda en primera persona")]
    [SerializeField] private bool showCharacter = true;
    [Tooltip("Altura sobre los pies a la que apunta esa cámara")]
    [SerializeField] private float lookHeight = 4f;
    [SerializeField, Min(0f)] private float cameraFollowSpeed = 6f;

    private const string WalkState = "Locomotion";
    private const string UpState = "StairsUp";
    private const string DownState = "StairsDown";
    private const int SamplesPerSegment = 8;
    private const float DoorClearance = 2f; // pasos desde el vano antes de que se cierre la puerta

    private PlayerMovement movement;
    private PlayerLook look;
    private CharacterController controller;
    private Animator animator;
    private ScreenIris iris;
    private Vector3 pivotOffset; // distancia entre los pies y el pivote
    private Transform view;      // cámara fija que se está usando

    public bool IsBusy { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        iris = gameObject.AddComponent<ScreenIris>();
    }

    // arrival: hacia dónde queda mirando al llegar (flecha azul)
    public void Climb(StairPath path, bool goingUp, Transform arrival, CameraTransition cameraTransition, Action onArrived)
    {
        if (IsBusy) return;
        StartCoroutine(ClimbRoutine(path, goingUp, arrival, cameraTransition, onArrived));
    }

    // whileCovered: lo que pasa con la pantalla en negro; no se abre hasta que termine
    public void PassDoor(DoorPassage passage, bool entering, CameraTransition cameraTransition,
        Func<IEnumerator> whileCovered, Action onArrived)
    {
        if (IsBusy) return;
        StartCoroutine(DoorRoutine(passage, entering, cameraTransition, whileCovered, onArrived));
    }

    // ---------- Escalera ----------

    private IEnumerator ClimbRoutine(StairPath path, bool goingUp, Transform arrival, CameraTransition cameraTransition, Action onArrived)
    {
        Begin(speed);
        Route route = new Route(transform.position - pivotOffset, path.GetPoints(goingUp));

        // En escaleras largas: un tramo en cada piso y el del medio se salta con la pantalla en negro
        bool hasCut = path.TryGetCut(goingUp, out int leaveAt, out int resumeAt);
        float cutFrom = hasCut ? route.DistanceTo(leaveAt) : 0f;
        float cutTo = hasCut ? route.DistanceTo(resumeAt) : 0f;
        bool jumped = false;

        animator.SetFloat("StairSpeed", goingUp ? upAnimationSpeed : downAnimationSpeed);
        bool onStairs = false;
        float travelled = 0f;

        while (travelled < route.Length)
        {
            travelled = Mathf.Min(travelled + speed * Time.deltaTime, route.Length);

            bool cutNow = hasCut && !jumped && travelled >= cutFrom;
            if (cutNow) travelled = cutFrom;

            Vector3 feet = route.PointAt(travelled);
            Place(feet);
            Turn(route.HeadingAt(travelled), turnSpeed * Time.deltaTime);
            movement.SetScriptedVelocity(transform.forward * speed);

            // Al principio y al final camina por el piso
            bool shouldBeOnStairs = travelled > route.StairsStart && travelled < route.StairsEnd;
            if (shouldBeOnStairs != onStairs)
            {
                onStairs = shouldBeOnStairs;
                string state = !onStairs ? WalkState : goingUp ? UpState : DownState;
                animator.CrossFadeInFixedTime(state, blendTime);
            }

            // Con corte, cada tramo se ve desde la cámara de su piso
            bool upperPart = jumped == goingUp;
            Follow(hasCut ? (upperPart ? path.UpperCamera : path.LowerCamera) : path.CameraFor(feet), feet, cameraTransition);

            if (hasCut)
            {
                // Se cierra antes de llegar al corte y se abre mientras sigue caminando del otro lado
                float closed = jumped
                    ? 1f - Mathf.InverseLerp(cutTo, cutTo + speed * openTime, travelled)
                    : Mathf.InverseLerp(cutFrom - speed * closeTime, cutFrom, travelled);
                ShowIris(closed, feet);
            }

            yield return null;

            if (cutNow)
            {
                yield return new WaitForSeconds(blackTime);

                jumped = true;
                travelled = cutTo;
                feet = route.PointAt(travelled);
                Place(feet);
                Turn(route.HeadingAt(travelled), 1f); // ya orientado, sin girar a la vista
                view = null;                          // la próxima cámara entra apuntando
                Follow(goingUp ? path.UpperCamera : path.LowerCamera, feet, cameraTransition);
                ShowIris(1f, feet);
                yield return null;
            }
        }

        End(arrival.forward, cameraTransition, onArrived);
    }

    // ---------- Puerta ----------

    private IEnumerator DoorRoutine(DoorPassage passage, bool entering, CameraTransition cameraTransition,
        Func<IEnumerator> whileCovered, Action onArrived)
    {
        DoorPassage.Side from = entering ? passage.Outside : passage.Inside;
        DoorPassage.Side to = entering ? passage.Inside : passage.Outside;

        Begin(doorSpeed);
        Vector3 feet = transform.position - pivotOffset;

        // La puerta se abre alejándose de Faunita; ella arranca un instante después
        // y el círculo se cierra mientras cruza
        Vector3 target = OnGround(from.door.position);
        Vector3 towardsDoor = Flat(from.door.position - from.end.position);
        float walkDelay = passage.SwingTime * 0.4f;
        float closeDelay = passage.SwingTime * 0.75f;
        animator.SetBool("IsWalking", false);

        for (float time = 0f; time < closeDelay + closeTime || feet != target; time += Time.deltaTime)
        {
            passage.SetOpen(time / passage.SwingTime, !entering);

            Vector3 remaining = Flat(target - feet);
            Vector3 facing = remaining.magnitude > 0.5f ? remaining : towardsDoor;

            if (time >= walkDelay) feet = WalkTowards(feet, target, facing);
            else Turn(facing.normalized, turnSpeed * Time.deltaTime);

            Follow(from.camera, feet, cameraTransition);
            ShowIris((time - closeDelay) / closeTime, feet);
            yield return null;
        }

        ShowIris(1f, feet);
        yield return null;

        // Con la pantalla en negro aparece del otro lado, en el vano, con la puerta abierta detrás
        passage.SetOpen(1f, entering);
        feet = OnGround(to.door.position);
        Vector3 doorway = feet;
        target = OnGround(to.end.position);
        Vector3 heading = Flat(target - feet);

        Place(feet);
        Turn(heading.normalized, 1f);
        view = null;
        Follow(to.camera, feet, cameraTransition);
        animator.SetBool("IsWalking", false);

        if (whileCovered != null) yield return whileCovered();
        yield return new WaitForSeconds(blackTime);

        // Sale caminando y la puerta se cierra cuando ya dio unos pasos
        float shut = 0f;

        for (float time = 0f; time < openTime || feet != target || shut < 1f; time += Time.deltaTime)
        {
            feet = WalkTowards(feet, target, heading);

            if (feet == target || Flat(feet - doorway).magnitude > DoorClearance)
                shut = Mathf.MoveTowards(shut, 1f, Time.deltaTime / passage.SwingTime);
            passage.SetOpen(1f - shut, entering);

            Follow(to.camera, feet, cameraTransition);
            ShowIris(1f - time / openTime, feet);
            yield return null;
        }

        passage.SetOpen(0f, entering);
        End(heading, cameraTransition, onArrived);
    }

    private Vector3 WalkTowards(Vector3 feet, Vector3 target, Vector3 heading)
    {
        feet = Vector3.MoveTowards(feet, target, doorSpeed * Time.deltaTime);
        bool walking = feet != target;

        Place(feet);
        Turn(heading.normalized, turnSpeed * Time.deltaTime);
        animator.SetBool("IsWalking", walking);
        movement.SetScriptedVelocity(walking ? transform.forward * doorSpeed : Vector3.zero);
        return feet;
    }

    // Apoya el punto en el suelo que tenga debajo
    private static Vector3 OnGround(Vector3 point)
    {
        bool found = Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        return found ? hit.point : point;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    // ---------- Común ----------

    private void Begin(float walkSpeed)
    {
        Vector3 bottom = controller.center + Vector3.down * (controller.height * 0.5f);
        pivotOffset = Vector3.up * (transform.position.y - transform.TransformPoint(bottom).y + controller.skinWidth);

        IsBusy = true;
        view = null;
        controller.enabled = false;
        movement.ResetMotion();

        animator.SetBool("IsWalking", true);
        animator.SetFloat("VelX", 0f);
        animator.SetFloat("VelZ", walkSpeed);
        animator.SetFloat("AnimSpeed", 1f);
        animator.SetFloat("Turn", 0f);
    }

    private void End(Vector3 facing, CameraTransition cameraTransition, Action onArrived)
    {
        iris.Set(0f, transform.position);

        animator.SetBool("IsWalking", false);
        animator.SetFloat("VelZ", 0f);

        controller.enabled = true;
        IsBusy = false;

        look.FaceDirection(facing);
        movement.ResetMotion(); // para que el giro de llegada no dispare la animación de girar
        cameraTransition.SnapToPlayer();
        onArrived?.Invoke();
    }

    private void Place(Vector3 feet) => transform.position = feet + pivotOffset;

    // En 1ra persona el círculo queda en el centro; si no, sigue al personaje
    private void ShowIris(float closed, Vector3 feet)
    {
        Vector3 focus = showCharacter || Camera.main == null
            ? feet + Vector3.up * lookHeight
            : Camera.main.transform.position + Camera.main.transform.forward;

        iris.Set(closed, focus);
    }

    // amount: 0 no gira, 1 queda mirando hacia heading
    private void Turn(Vector3 heading, float amount)
    {
        if (heading.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(heading);
        Quaternion next = Quaternion.Slerp(transform.rotation, target, amount);

        // En 1ra persona manda PlayerLook: hay que girar su orientación
        if (!showCharacter) look.SetHeading(next * Vector3.forward);
        else transform.rotation = next;
    }

    // Sigue al personaje con una cámara fija; al cambiar de cámara entra ya apuntando
    private void Follow(Transform next, Vector3 feet, CameraTransition cameraTransition)
    {
        if (!showCharacter) return;

        Quaternion aim = Quaternion.LookRotation(feet + Vector3.up * lookHeight - next.position);

        if (next != view)
        {
            view = next;
            next.rotation = aim;
            cameraTransition.CutTo(next);
        }
        else
        {
            next.rotation = Quaternion.Slerp(next.rotation, aim, cameraFollowSpeed * Time.deltaTime);
        }
    }

    // Curva suave que pasa por los puntos, recorrida por distancia
    private class Route
    {
        private readonly List<Vector3> samples = new List<Vector3>();
        private readonly List<float> distances = new List<float>();
        private readonly List<float> pointDistances = new List<float>();

        public float Length => distances[distances.Count - 1];
        // Tramo de escalones: entre el primer punto del recorrido y el anteúltimo
        public float StairsStart { get; }
        public float StairsEnd { get; }

        // Distancia recorrida al pasar por ese punto
        public float DistanceTo(int point) => pointDistances[point];

        // Hacia dónde se camina en ese tramo, en horizontal
        public Vector3 HeadingAt(float distance) => Flat(PointAt(distance + 1.5f) - PointAt(distance - 0.5f)).normalized;

        public Route(Vector3 start, List<Vector3> points)
        {
            Add(start);
            Add(points[0]);
            StairsStart = Length;
            StairsEnd = Length;
            pointDistances.Add(Length);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 before = points[Mathf.Max(i - 1, 0)];
                Vector3 after = points[Mathf.Min(i + 2, points.Count - 1)];

                for (int s = 1; s <= SamplesPerSegment; s++)
                    Add(CatmullRom(before, points[i], points[i + 1], after, s / (float)SamplesPerSegment));

                pointDistances.Add(Length);

                if (i == points.Count - 3) StairsEnd = Length;
            }
        }

        public Vector3 PointAt(float distance)
        {
            if (distance <= 0f) return samples[0];

            for (int i = 1; i < samples.Count; i++)
            {
                if (distance <= distances[i])
                    return Vector3.Lerp(samples[i - 1], samples[i], Mathf.InverseLerp(distances[i - 1], distances[i], distance));
            }

            return samples[samples.Count - 1];
        }

        private void Add(Vector3 point)
        {
            distances.Add(samples.Count == 0 ? 0f : Length + Vector3.Distance(samples[samples.Count - 1], point));
            samples.Add(point);
        }

        private static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            return 0.5f * (2f * b + (c - a) * t
                + (2f * a - 5f * b + 4f * c - d) * (t * t)
                + (3f * b - a - 3f * c + d) * (t * t * t));
        }
    }
}
