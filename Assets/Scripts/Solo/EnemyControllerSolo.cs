using System.Linq;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemysControllerSolo : MonoBehaviour
{
    [Header("Movimiento")]
    public float detectionRadius = 5f;
    public float speed = 2f;

    [Header("Daño de contacto")]
    public int contactDamage = 1;
    public float hitCooldown = 0.75f;

    Rigidbody2D rb;
    Animator animator;
    float lastHitTime = -999f;
    Transform target;

    bool hasScreamed = false; // Para que no suene en cada frame del Update

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

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
                // --- Lógica del Audio ---
                if (!hasScreamed)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFXVariable(AudioManager.Instance.ghostWakeUp);
                    hasScreamed = true;
                }

                // --- Lógica de Movimiento ---
                Vector2 dir = (target.position - transform.position).normalized;

                if (dir.x < 0) transform.localScale = new Vector3(-1, 1, 1);
                else if (dir.x > 0) transform.localScale = new Vector3(1, 1, 1);

                movement = dir;
                inMovement = true;
            }
            else
            {
                // IMPORTANTE: Si sale del radio, permitimos que vuelva a gritar la próxima vez
                hasScreamed = false;
            }
        }

        rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
        animator.SetBool("inMovement", inMovement);
    }

    Transform FindClosestPlayer()
    {
        var players = FindObjectsOfType<PlayerControllerSolo>();
        if (players == null || players.Length == 0) return null;

        return players
            .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
            .First()
            .transform;
    }

    void OnCollisionEnter2D(Collision2D col) { TryDamage(col.collider, col.GetContact(0).point); }
    void OnCollisionStay2D(Collision2D col) { TryDamage(col.collider, col.GetContact(0).point); }
    void OnTriggerEnter2D(Collider2D other) { TryDamage(other, other.transform.position); }

    void TryDamage(Collider2D col, Vector3 hitPos)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        if (col == null || !col.CompareTag("Player")) return;

        // Intentamos obtener el script de control del jugador
        var playerScript = col.GetComponent<PlayerControllerSolo>();
        if (playerScript == null) return;

        lastHitTime = Time.time;
        Vector2 knockbackDir = ((Vector2)col.transform.position - (Vector2)hitPos).normalized;

        // --- LÓGICA HÍBRIDA ---
        var pv = col.GetComponent<PhotonView>();

        if (PhotonNetwork.IsConnected && pv != null)
        {
            // MODO MULTIJUGADOR: Mandamos RPC al dueño del jugador
            pv.RPC("RPC_ReceiveDamage", pv.Owner, knockbackDir, contactDamage);
        }
        else
        {
            // MODO SINGLE PLAYER: Llamamos a la función de daño directamente
            // Nota: Asegúrate de que RPC_ReceiveDamage sea 'public' en PlayerController
            playerScript.RPC_ReceiveDamage(knockbackDir, contactDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}