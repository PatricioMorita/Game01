using UnityEngine;

/// <summary>
/// Controla al jugador: movimiento horizontal, salto con física,
/// gravedad ajustada al caer y muerte por caída al vacío.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Gravedad ajustada")]
    [Tooltip("Multiplica la gravedad mientras el jugador cae, para un salto menos 'flotante'.")]
    [SerializeField] private float fallGravityMultiplier = 2f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;          // objeto hijo ubicado en los pies
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;          // capa "Ground" del Tilemap

    [Header("Límites del nivel")]
    [SerializeField] private float deathHeight = -10f;       // bajo esta altura el jugador muere

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSfx;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float baseGravity;
    private float inputX;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseGravity = rb.gravityScale;
    }

    // Update: leer input (se ejecuta cada frame)
    private void Update()
    {
        if (isDead) return;

        inputX = Input.GetAxisRaw("Horizontal");             // A/D o flechas
        if (Input.GetButtonDown("Jump")) jumpRequested = true; // Espacio

        // Voltear el sprite según la dirección
        if (spriteRenderer != null && inputX != 0)
            spriteRenderer.flipX = inputX < 0;

        // Condición de derrota: caer fuera del nivel
        if (transform.position.y < deathHeight) Die();
    }

    // FixedUpdate: aplicar física (paso fijo, independiente de los FPS)
    private void FixedUpdate()
    {
        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            AudioManager.PlaySfx(jumpSfx);
        }
        jumpRequested = false;

        // Gravedad más fuerte al caer
        rb.gravityScale = rb.linearVelocity.y < 0 ? baseGravity * fallGravityMultiplier : baseGravity;
    }

    /// <summary>Mata al jugador y avisa al GameManager (lo llaman Hazard o la caída).</summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GameManager.Instance.PlayerDied();
    }


    // Dibuja el círculo de detección de suelo en el editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
