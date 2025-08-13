using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
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

    private bool onFloor;
    private bool receivingDamage;
    private bool attacking;
    private bool dead;
    private Rigidbody2D rb;

    public TextMeshProUGUI textMeshPro;

    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!attacking)
        {
            Movement();

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLength, floorLayer);
            onFloor = hit.collider != null;

            if (onFloor && Input.GetKeyDown(KeyCode.Space) && !receivingDamage)
            {
                rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            }
        }
        

        if (Input.GetMouseButtonDown(0) && !attacking && onFloor)
        {
            Attacking();
        }

        animator.SetBool("onFloor", onFloor);
        animator.SetBool("receiveDamage", receivingDamage);
        animator.SetBool("Attacking", attacking);
    }

    public void Movement()
    {
        float speedX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;

        animator.SetFloat("movement", speedX * speed);

        if (speedX < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (speedX > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        Vector3 position = transform.position;

        if (!receivingDamage)
        {
            transform.position = new Vector3(speedX + position.x, position.y, position.z);
        }
    }
    public void receiveDamage(Vector2 direction, int amountDamage)
    {
        if (!receivingDamage)
        {
            receivingDamage = true;
            vida -= amountDamage;
            actualizarCorazones();
            Vector2 rebound = new Vector2(transform.position.x - direction.x, 1).normalized;
            rb.AddForce(rebound * reboundForce, ForceMode2D.Impulse);

            if(vida <= 0)
            {
                final_Canvas.SetActive(true);
                Time.timeScale = 0.0f;
            }
        }
    }

    public void desactiveDamage()
    {
        receivingDamage = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void Attacking()
    {
        attacking = true;
    }

    public void desactiveAttack()
    {
        attacking = false;
    }

    public void actualizarCorazones()
    {
        for (int i = 0; i < Heart.Length; i++)
        {
            Heart[i].gameObject.SetActive(i<vida);
        }
    }

    public void actualizarCrystals()
    {
        crystals++;
        textMeshPro.text = (crystals.ToString() + " x");

        if (crystals == 30)
        {
            won_Canvas.SetActive(true);
            Time.timeScale = 0.0f;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastLength);
    }
}
