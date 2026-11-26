using UnityEngine;

/// <summary>
/// Trampa (pinchos, lava, enemigo quieto): mata al jugador al tocarla.
/// Funciona con Collider2D normal o con "Is Trigger". Usar como prefab.
/// </summary>
public class Hazard : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D other) => TryKill(other.gameObject);

    private void TryKill(GameObject target)
    {
        PlayerController player = target.GetComponent<PlayerController>();
        if (player != null) player.Die();
    }
}
