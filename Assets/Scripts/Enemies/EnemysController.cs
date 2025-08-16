using System.Linq;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemysController : MonoBehaviour
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
                Vector2 dir = (target.position - transform.position).normalized;
                if (dir.x < 0) transform.localScale = new Vector3(-1, 1, 1);
                else if (dir.x > 0) transform.localScale = new Vector3(1, 1, 1);

                movement = dir;
                inMovement = true;
            }
        }

        rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
        animator.SetBool("inMovement", inMovement);
    }

    Transform FindClosestPlayer()
    {
        var players = FindObjectsOfType<PlayerController>();
        if (players == null || players.Length == 0) return null;

        return players.OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
                      .First().transform;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.contactCount > 0) TryDamage(col.collider, col.GetContact(0).point);
    }
    void OnCollisionStay2D(Collision2D col)
    {
        if (col.contactCount > 0) TryDamage(col.collider, col.GetContact(0).point);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, other.transform.position);
    }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
