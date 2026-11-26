using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;


public class CameraTransition : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private Transform cinemachineCameraTransform;
    [SerializeField] private float blendDuration = 0.5f;
    [SerializeField] private Transform startingAnchor;


    [SerializeField] private Camera targetCamera;
    [SerializeField] private float basketViewFov = 87f;
    private float defaultFov;

    private Coroutine blendRoutine;
    private Transform activeAnchor;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        defaultFov = targetCamera.fieldOfView;

        if (startingAnchor != null)
        {
            cinemachineBrain.enabled = false;
            transform.position = startingAnchor.position;
            transform.rotation = startingAnchor.rotation;
        }
    }


    public float BasketViewFov => basketViewFov;

    public void TransitionTo(Transform anchor, Action onComplete = null) => TransitionTo(anchor, defaultFov, onComplete);

    public void TransitionTo(Transform anchor, float targetFov, Action onComplete = null)
    {
        cinemachineBrain.enabled = false;
        activeAnchor = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartBlend(anchor, targetFov, () =>
        {
            activeAnchor = anchor;
            onComplete?.Invoke();
        });
    }

    public void TransitionToPlayer(Action onComplete = null)
    {
        activeAnchor = null;
        RefreshFollowCamera();

        // La cámara de destino depende de si el player está en una zona de 1ra persona
        Transform liveCamera = cinemachineCameraTransform;
        float liveFov = defaultFov;
        PlayerCameraRig rig = cinemachineCameraTransform.GetComponentInParent<PlayerCameraRig>();
        if (rig != null)
        {
            rig.RefreshMode();
            liveCamera = rig.ActiveCamera.transform;
            liveFov = rig.ActiveCamera.Lens.FieldOfView;
        }

        StartBlend(liveCamera, liveFov, () =>
        {
            cinemachineBrain.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            onComplete?.Invoke();
        });
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

    private void StartBlend(Transform liveTarget, float targetFov, Action onComplete)
    {
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = StartCoroutine(BlendRoutine(liveTarget, targetFov, onComplete));
    }

    private IEnumerator BlendRoutine(Transform liveTarget, float targetFov, Action onComplete)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFov = targetCamera.fieldOfView;
        float t = 0f;

        while (t < blendDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / blendDuration);
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