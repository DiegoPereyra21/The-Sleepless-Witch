using UnityEngine;
using TMPro;

public class MailboxIndicatorTMP : MonoBehaviour
{
    [SerializeField] private Mailbox mailbox;
    [SerializeField] private TextMeshPro label;

    private void Awake()
    {
        // El cartel siempre mira a la cámara, así no se lee al revés
        if (label.GetComponent<Billboard>() == null)
            label.gameObject.AddComponent<Billboard>();
    }

    private void LateUpdate()
    {
        int remaining = mailbox.RemainingCount;

        if (remaining <= 0)
        {
            label.text = "";
            return;
        }

        label.text = $"{remaining} cartas";
    }
}