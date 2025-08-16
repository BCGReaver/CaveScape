using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviourPun
{
    public float speed = 5f;
    public int vida = 3;
    public Image[] Heart;
    public int crystals = 0;
    public GameObject final_Canvas;
    public GameObject won_Canvas;

    public float jumpForce = 10f;
    public float reboundForce = 10f;
    public float raycastLength = 0.1f;
    public LayerMask floorLayer;

    bool onFloor, receivingDamage, attacking, dead;
    Rigidbody2D rb;
    public TextMeshProUGUI textMeshPro;
    public Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // UI solo para el dueño
        if (!photonView.IsMine)
        {
            if (final_Canvas) final_Canvas.SetActive(false);
            if (won_Canvas) won_Canvas.SetActive(false);
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (!attacking)
        {
            Movement();

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLength, floorLayer);
            onFloor = hit.collider != null;

            if (onFloor && Input.GetKeyDown(KeyCode.Space) && !receivingDamage)
                rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        }

        if (Input.GetMouseButtonDown(0) && !attacking && onFloor)
            Attacking();

        animator.SetBool("onFloor", onFloor);
        animator.SetBool("receiveDamage", receivingDamage);
        animator.SetBool("Attacking", attacking);
    }

    void Movement()
    {
        float speedX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;

        animator.SetFloat("movement", speedX * speed);
        if (speedX < 0) transform.localScale = new Vector3(-1, 1, 1);
        if (speedX > 0) transform.localScale = new Vector3(1, 1, 1);

        if (!receivingDamage)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x + speedX, p.y, p.z);
        }
    }

    public void receiveDamage(Vector2 direction, int amountDamage)
    {
        if (!photonView.IsMine) return;

        if (!receivingDamage)
        {
            receivingDamage = true;
            vida -= amountDamage;
            actualizarCorazones();

            Vector2 rebound = new Vector2(transform.position.x - direction.x, 1).normalized;
            rb.AddForce(rebound * reboundForce, ForceMode2D.Impulse);

            if (vida <= 0)
            {
                if (final_Canvas) final_Canvas.SetActive(true);
                Time.timeScale = 0.0f;
            }
        }
    }

    public void desactiveDamage()
    {
        receivingDamage = false;
        rb.linearVelocity = Vector2.zero; // <-- CORREGIDO
    }

    public void Attacking() { attacking = true; }
    public void desactiveAttack() { attacking = false; }

    public void actualizarCorazones()
    {
        if (!photonView.IsMine) return;
        for (int i = 0; i < Heart.Length; i++)
            if (Heart[i]) Heart[i].gameObject.SetActive(i < vida);
    }

    public void actualizarCrystals()
    {
        if (!photonView.IsMine) return;
        crystals++;
        if (textMeshPro) textMeshPro.text = (crystals.ToString() + " x");
        if (crystals == 30)
        {
            if (won_Canvas) won_Canvas.SetActive(true);
            Time.timeScale = 0.0f;
        }
    }
}
