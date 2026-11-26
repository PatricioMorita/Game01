using UnityEngine;

/// <summary>
/// Meta del nivel: al tocarla el jugador gana (si junta las monedas requeridas).
/// Requiere un Collider2D con "Is Trigger" activado.
/// </summary>
public class Goal : MonoBehaviour
{
    [Tooltip("Monedas mínimas para poder ganar (0 = sin requisito).")]
    [SerializeField] private int requiredScore = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (GameManager.Instance.Score < requiredScore) return;

        GameManager.Instance.PlayerWon();
    }
}
