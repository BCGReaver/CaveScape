using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    public float jumpForce = 10f;
    public float raycastLength = 0.1f;
    public LayerMask floorLayer;

    private bool onFloor;
    private Rigidbody2D rb;

    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
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

        transform.position = new Vector3(speedX + position.x, position.y, position.z);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLength, floorLayer);
        onFloor = hit.collider != null;

        if (onFloor && Input.GetKeyDown(KeyCode.Space)) 
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        }

        animator.SetBool("onFloor", onFloor);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastLength);
    }
}
