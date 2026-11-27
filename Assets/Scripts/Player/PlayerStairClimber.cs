using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lleva al personaje solo por un StairPath, con la animación de subir o bajar.
[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerStairClimber : MonoBehaviour
{
    [Tooltip("Velocidad del recorrido (unidades de mundo por segundo)")]
    [SerializeField, Min(1f)] private float speed = 11f;
    [Tooltip("Qué tan rápido gira siguiendo la curva")]
    [SerializeField, Min(1f)] private float turnSpeed = 10f;

    [Header("Animación")]
    [SerializeField, Min(0.1f)] private float upAnimationSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float downAnimationSpeed = 1f;
    [SerializeField, Min(0f)] private float blendTime = 0.2f;

    [Header("Cámara")]
    [Tooltip("Mientras recorre la escalera se ve a Faunita. Apagado se queda en primera persona")]
    [SerializeField] private bool showCharacter = true;
    [Tooltip("Altura sobre los pies a la que apunta esa cámara")]
    [SerializeField] private float lookHeight = 4f;
    [SerializeField, Min(0f)] private float cameraFollowSpeed = 6f;

    private const string WalkState = "Locomotion";
    private const string UpState = "StairsUp";
    private const string DownState = "StairsDown";
    private const int SamplesPerSegment = 8;

    private PlayerMovement movement;
    private PlayerLook look;
    private CharacterController controller;
    private Animator animator;

    public bool IsClimbing { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    // arrival: hacia dónde queda mirando al llegar (flecha azul)
    public void Climb(StairPath path, bool goingUp, Transform arrival, CameraTransition cameraTransition, Action onArrived)
    {
        if (IsClimbing) return;
        StartCoroutine(ClimbRoutine(path, goingUp, arrival, cameraTransition, onArrived));
    }

    private IEnumerator ClimbRoutine(StairPath path, bool goingUp, Transform arrival, CameraTransition cameraTransition, Action onArrived)
    {
        // Distancia entre el pivote y los pies cuando está apoyado
        Vector3 bottom = controller.center + Vector3.down * (controller.height * 0.5f);
        Vector3 pivotOffset = Vector3.up * (transform.position.y - transform.TransformPoint(bottom).y + controller.skinWidth);

        List<Vector3> points = path.GetPoints(goingUp);
        Route route = new Route(transform.position - pivotOffset, points);

        IsClimbing = true;
        controller.enabled = false;
        movement.ResetMotion();

        animator.SetBool("IsWalking", true);
        animator.SetFloat("VelX", 0f);
        animator.SetFloat("VelZ", speed);
        animator.SetFloat("AnimSpeed", 1f);
        animator.SetFloat("Turn", 0f);
        animator.SetFloat("StairSpeed", goingUp ? upAnimationSpeed : downAnimationSpeed);

        Transform view = null;
        bool onStairs = false;
        float travelled = 0f;

        while (travelled < route.Length)
        {
            travelled = Mathf.Min(travelled + speed * Time.deltaTime, route.Length);
            Vector3 feet = route.PointAt(travelled);
            transform.position = feet + pivotOffset;

            Vector3 heading = route.PointAt(travelled + 1.5f) - route.PointAt(travelled - 0.5f);
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.001f)
                Turn(heading.normalized);

            movement.SetScriptedVelocity(transform.forward * speed);

            // Al principio y al final camina por el piso
            bool shouldBeOnStairs = travelled > route.StairsStart && travelled < route.StairsEnd;
            if (shouldBeOnStairs != onStairs)
            {
                onStairs = shouldBeOnStairs;
                string state = !onStairs ? WalkState : goingUp ? UpState : DownState;
                animator.CrossFadeInFixedTime(state, blendTime);
            }

            if (showCharacter)
                view = Follow(path.CameraFor(feet), view, feet, cameraTransition);

            yield return null;
        }

        animator.SetBool("IsWalking", false);
        animator.SetFloat("VelZ", 0f);

        controller.enabled = true;
        movement.ResetMotion();
        IsClimbing = false;

        look.FaceDirection(arrival.forward);
        movement.ResetMotion(); // para que el giro de llegada no dispare la animación de girar
        cameraTransition.SnapToPlayer();
        onArrived?.Invoke();
    }

    private void Turn(Vector3 heading)
    {
        Quaternion target = Quaternion.LookRotation(heading);
        Quaternion next = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);

        // En 1ra persona manda PlayerLook: hay que girar su orientación
        if (!showCharacter) look.SetHeading(next * Vector3.forward);
        else transform.rotation = next;
    }

    private Transform Follow(Transform next, Transform current, Vector3 feet, CameraTransition cameraTransition)
    {
        Quaternion aim = Quaternion.LookRotation(feet + Vector3.up * lookHeight - next.position);

        if (next != current)
        {
            next.rotation = aim;
            cameraTransition.CutTo(next);
        }
        else
        {
            next.rotation = Quaternion.Slerp(next.rotation, aim, cameraFollowSpeed * Time.deltaTime);
        }

        return next;
    }

    // Curva suave que pasa por los puntos, recorrida por distancia
    private class Route
    {
        private readonly List<Vector3> samples = new List<Vector3>();
        private readonly List<float> distances = new List<float>();

        public float Length => distances[distances.Count - 1];
        // Tramo de escalones: entre el primer punto del recorrido y el anteúltimo
        public float StairsStart { get; }
        public float StairsEnd { get; }

        public Route(Vector3 start, List<Vector3> points)
        {
            Add(start);
            Add(points[0]);
            StairsStart = Length;
            StairsEnd = Length;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 before = points[Mathf.Max(i - 1, 0)];
                Vector3 after = points[Mathf.Min(i + 2, points.Count - 1)];

                for (int s = 1; s <= SamplesPerSegment; s++)
                    Add(CatmullRom(before, points[i], points[i + 1], after, s / (float)SamplesPerSegment));

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
