using System;
using System.Collections;
using UnityEngine;

// El cucharón: se clickea para preparar la poción. Gira alrededor de su pivote al revolver.
public class CauldronLadle : MonoBehaviour, IClickTarget
{
    [SerializeField] private InteractableOutline outline;
    [SerializeField, Min(1)] private int turns = 2;
    [SerializeField, Min(0.1f)] private float stirTime = 1.2f;

    public event Action Clicked;

    public InteractableOutline Outline => outline;
    public string HoverText => GameText.Pick("Revolver y preparar", "Stir and brew");

    public void Click() => Clicked?.Invoke();

    public void SetHovered(bool hovered)
    {
        if (outline != null) outline.SetHighlighted(hovered);
    }

    public IEnumerator Stir()
    {
        Quaternion rest = transform.localRotation;

        for (float time = 0f; time < stirTime; time += Time.deltaTime)
        {
            float angle = 360f * turns * Mathf.SmoothStep(0f, 1f, time / stirTime);
            transform.localRotation = rest * Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }

        transform.localRotation = rest;
    }
}
