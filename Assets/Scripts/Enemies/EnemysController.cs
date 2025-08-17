/**
 * @file EnemysController.cs
 * @brief IA básica de enemigo 2D que persigue al jugador y hace daño por contacto (con cooldown).
 *
 * @details
 * Este compa:
 * - Busca al Player más cercano en la escena.
 * - Si está dentro de un radio de detección, camina hacia él y voltea el sprite según la dirección.
 * - Cuando choca o entra a su trigger, intenta hacer daño una vez cada `hitCooldown` segundos.
 * - El daño se manda por red usando un RPC al dueño del Player (Photon), para que el knockback/vida sean consistentes.
 *
 * Notas rápidas:
 * - Requiere `Rigidbody2D` (para mover con `MovePosition`) y `Animator` (bool "inMovement").
 * - Para ver el radio en el editor, selecciona el objeto y activa gizmos (se dibuja un círculo rojo).
 * - El Player debe tener Tag "Player" y un `PhotonView`.
 */

using System.Linq;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
/**
 * @class EnemysController
 * @brief Controla el movimiento de persecución y el daño por contacto del enemigo.
 */
public class EnemysController : MonoBehaviour
{
    [Header("Movimiento")]
    /// @brief Qué tan lejos “huele” a los jugadores para empezar a perseguir.
    public float detectionRadius = 5f;

    /// @brief Velocidad de movimiento lineal hacia el target.
    public float speed = 2f;

    [Header("Daño de contacto")]
    /// @brief Cuánto daño aplica al tocar al jugador.
    public int contactDamage = 1;

    /// @brief Enfriamiento entre golpes (en segundos) para no spamear daño.
    public float hitCooldown = 0.75f;

    /// @brief Físicas del enemigo (para mover con MovePosition).
    Rigidbody2D rb;

    /// @brief Controla animaciones como caminar (bool "inMovement").
    Animator animator;

    /// @brief Timestamp del último golpe aplicado (para cooldown).
    float lastHitTime = -999f;

    /// @brief Referencia al jugador que estamos persiguiendo.
    Transform target;

    /**
     * @brief Cachea referencias a Rigidbody2D y Animator.
     */
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    /**
     * @brief Loop principal: busca al player más cercano, decide si moverse y anima.
     *
     * @details
     * - Si no hay target o se desactivó, se busca uno nuevo.
     * - Solo se mueve si el jugador está dentro del radio de detección.
     * - Cambia el `localScale.x` para voltear el sprite a la izquierda/derecha.
     */
    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            target = FindClosestPlayer();

        Vector2 movement = Vector2.zero;
        bool inMovement = false;

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= detectionRadius)
            {
                Vector2 dir = (target.position - transform.position).normalized;

                // Voltear sprite según la dirección X
                if (dir.x < 0) transform.localScale = new Vector3(-1, 1, 1);
                else if (dir.x > 0) transform.localScale = new Vector3(1, 1, 1);

                movement = dir;
                inMovement = true;
            }
        }

        // Movimiento suave por físicas (mejor que alterar transform directamente)
        rb.MovePosition(rb.position + movement * speed * Time.deltaTime);

        // Animación de caminar/parado
        animator.SetBool("inMovement", inMovement);
    }

    /**
     * @brief Encuentra el PlayerController más cercano en la escena.
     * @return Transform del jugador más cercano o null si no hay.
     */
    Transform FindClosestPlayer()
    {
        var players = FindObjectsOfType<PlayerController>();
        if (players == null || players.Length == 0) return null;

        return players
            .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
            .First()
            .transform;
    }

    /**
     * @brief Al primer toque por colisión, intenta aplicar daño.
     * @param col Info de la colisión.
     */
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.contactCount > 0) TryDamage(col.collider, col.GetContact(0).point);
    }

    /**
     * @brief Mientras siga colisionando, vuelve a intentar (respetando cooldown).
     * @param col Info de la colisión.
     */
    void OnCollisionStay2D(Collision2D col)
    {
        if (col.contactCount > 0) TryDamage(col.collider, col.GetContact(0).point);
    }

    /**
     * @brief Si usas colliders en modo Trigger, también aplica daño al entrar.
     * @param other El collider que entró al trigger.
     */
    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, other.transform.position);
    }

    /**
     * @brief Lógica de daño: valida Player + cooldown y manda RPC al dueño para que reciba daño.
     * @param col Collider del objetivo (esperamos Tag "Player").
     * @param hitPos Punto de contacto para calcular dirección de knockback.
     *
     * @details
     * - Valida cooldown para no pegar cada frame.
     * - Calcula la dirección desde el punto de impacto hacia el jugador (para el rebote).
     * - Llama `PlayerController.RPC_ReceiveDamage` al Owner del PhotonView del player.
     */
    void TryDamage(Collider2D col, Vector3 hitPos)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        if (col == null || !col.CompareTag("Player")) return;

        var pv = col.GetComponent<PhotonView>();
        if (pv == null) return;

        lastHitTime = Time.time;

        Vector2 dir = ((Vector2)col.transform.position - (Vector2)hitPos).normalized;
        Debug.Log($"[ENEMY] hit -> {col.name} owner={pv.OwnerActorNr} isMine={pv.IsMine}");
        pv.RPC(nameof(PlayerController.RPC_ReceiveDamage), pv.Owner, dir, contactDamage);
    }

    /**
     * @brief Dibuja en el editor el radio de detección del enemigo.
     */
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
