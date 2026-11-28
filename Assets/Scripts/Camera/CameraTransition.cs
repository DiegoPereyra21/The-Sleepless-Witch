using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;


public class CameraTransition : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private Transform cinemachineCameraTransform;
    [SerializeField] private float blendDuration = 0.5f;


    [SerializeField] private Camera targetCamera;
    [SerializeField] private float basketViewFov = 87f;
    private float defaultFov;

    private Coroutine blendRoutine;
    private Transform activeAnchor;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        defaultFov = targetCamera.fieldOfView;
    }


    public float BasketViewFov => basketViewFov;
    // true mientras la cámara viaja desde una vista fija hacia la del player
    public bool IsReturningToPlayer { get; private set; }
    // true mientras la cámara está viajando, de ida o de vuelta
    public bool IsBlending => blendRoutine != null;

    public void TransitionTo(Transform anchor, Action onComplete = null) => TransitionTo(anchor, defaultFov, onComplete);

    // duration: segundos que tarda el viaje; en 0 usa Blend Duration
    public void TransitionTo(Transform anchor, float targetFov, Action onComplete = null, float duration = 0f)
    {
        cinemachineBrain.enabled = false;
        activeAnchor = null;
        IsReturningToPlayer = false;

        SetCursorLocked(false);

        StartBlend(anchor, targetFov, duration, () =>
        {
            activeAnchor = anchor;
            onComplete?.Invoke();
        });
    }

    public void TransitionToPlayer(Action onComplete = null, float duration = 0f)
    {
        activeAnchor = null;
        RefreshFollowCamera();

        // La cámara de destino depende de si el player está en una zona de 1ra persona
        Transform liveCamera = cinemachineCameraTransform;
        float liveFov = defaultFov;
        PlayerCameraRig rig = RefreshRig();
        if (rig != null)
        {
            liveCamera = rig.ActiveCamera.transform;
            liveFov = rig.ActiveCamera.Lens.FieldOfView;
        }

        IsReturningToPlayer = true;
        StartBlend(liveCamera, liveFov, duration, () =>
        {
            IsReturningToPlayer = false;
            cinemachineBrain.enabled = true;
            SetCursorLocked(true);
            onComplete?.Invoke();
        });
    }

    // Vuelve a la cámara del player de golpe: al aparecer o al teletransportarse
    public void SnapToPlayer()
    {
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = null;
        activeAnchor = null;
        IsReturningToPlayer = false;

        RefreshFollowCamera();
        PlayerCameraRig rig = RefreshRig();
        if (rig != null) rig.ActiveCamera.PreviousStateIsValid = false;

        cinemachineBrain.enabled = true;
        cinemachineBrain.ResetState(); // sin mezcla desde la cámara anterior
        SetCursorLocked(true);
    }

    // Salta a una cámara fija sin mezcla y sin soltar el mouse
    public void CutTo(Transform anchor)
    {
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = null;
        IsReturningToPlayer = false;

        cinemachineBrain.enabled = false;
        activeAnchor = anchor;
        targetCamera.fieldOfView = defaultFov;
        transform.SetPositionAndRotation(anchor.position, anchor.rotation);
    }

    private PlayerCameraRig RefreshRig()
    {
        PlayerCameraRig rig = cinemachineCameraTransform.GetComponentInParent<PlayerCameraRig>();
        if (rig != null) rig.RefreshMode();
        return rig;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Con el brain apagado la cámara de seguimiento no se actualiza: se recoloca detrás del player antes del blend
    private void RefreshFollowCamera()
    {
        if (!cinemachineCameraTransform.TryGetComponent(out CinemachineCamera followCamera)) return;

        if (followCamera.Follow != null)
        {
            PlayerLook look = followCamera.Follow.GetComponentInParent<PlayerLook>();
            if (look != null) look.ApplyRotation();
        }

        followCamera.PreviousStateIsValid = false;
        followCamera.UpdateCameraState(Vector3.up, -1f);
    }

    private void LateUpdate()
    {
        if (activeAnchor != null && blendRoutine == null)
        {
            transform.position = activeAnchor.position;
            transform.rotation = activeAnchor.rotation;
        }
    }

    private void StartBlend(Transform liveTarget, float targetFov, float duration, Action onComplete)
    {
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        if (duration <= 0f) duration = blendDuration;
        blendRoutine = StartCoroutine(BlendRoutine(liveTarget, targetFov, duration, onComplete));
    }

    private IEnumerator BlendRoutine(Transform liveTarget, float targetFov, float duration, Action onComplete)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFov = targetCamera.fieldOfView;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            transform.position = Vector3.Lerp(startPos, liveTarget.position, p);
            transform.rotation = Quaternion.Slerp(startRot, liveTarget.rotation, p);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, p);
            yield return null;
        }

        transform.position = liveTarget.position;
        transform.rotation = liveTarget.rotation;
        targetCamera.fieldOfView = targetFov;
        blendRoutine = null;
        onComplete?.Invoke();
    }
}