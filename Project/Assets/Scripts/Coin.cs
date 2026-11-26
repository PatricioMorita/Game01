using UnityEngine;

/// <summary>
/// Moneda recolectable: suma puntos al tocarla. Usar como prefab.
/// Requiere un Collider2D con "Is Trigger" activado.
/// </summary>
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private AudioClip pickupSfx;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponent<PlayerController>() == null) return;
        collected = true;

        GameManager.Instance.AddScore(value);
        AudioManager.PlaySfx(pickupSfx);
        Destroy(gameObject);
    }
}
