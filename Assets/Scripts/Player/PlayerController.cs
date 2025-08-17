using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Linq;
using System.Text.RegularExpressions;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviourPun
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float reboundForce = 3f;
    public float raycastLength = 0.56f;
    public LayerMask floorLayer;

    [Header("Health")]
    public int vida = 3;
    public Image[] Heart; // Se llena en runtime (HUDBinder o por TAG)

    [Header("UI (opcional, se resuelve por TAG si está vacío)")]
    public GameObject final_Canvas; // derrota
    public GameObject won_Canvas;   // victoria

    [Header("Crystals (local)")]
    public int crystals = 0;

    public Animator animator;

    // Internos
    private Rigidbody2D rb;
    private bool onFloor, receivingDamage, attacking;

    // HUD
    private TMP_Text crystalText;
    private Health hudHealth; // si el HUD tiene este script con hearts, se usa primero

    // TAGs usados como fallback
    [SerializeField] private string crystalCounterTag = "HUD_CrystalText";
    [SerializeField] private string heartsRootTag = "HUD_HeartsRoot";
    [SerializeField] private string loseCanvasTag = "HUD_LoseCanvas";
    [SerializeField] private string winCanvasTag = "HUD_WinCanvas";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (photonView.IsMine)
        {
            // Fallback por TAG (por si HUDBinder no está)
            if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
            if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);
            if (final_Canvas) final_Canvas.SetActive(false);
            if (won_Canvas) won_Canvas.SetActive(false);

            AttemptAutoWireHUD(); // llena Heart[] y crystalText si están vacíos

            actualizarCorazones();
            if (crystalText) crystalText.text = crystals + " x";
        }
        else
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
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        if (Input.GetMouseButtonDown(0) && !attacking && onFloor)
            Attacking();

        animator.SetBool("onFloor", onFloor);
        animator.SetBool("receiveDamage", receivingDamage);
        animator.SetBool("Attacking", attacking);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K)) photonView.RPC(nameof(RPC_ReceiveDamage), photonView.Owner, Vector2.left, 1);
        if (Input.GetKeyDown(KeyCode.L)) { vida = Mathf.Min(3, vida + 1); actualizarCorazones(); }
#endif
    }

    void Movement()
    {
        float speedX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;

        animator.SetFloat("movement", Mathf.Abs(speedX) * speed);
        if (speedX < 0) transform.localScale = new Vector3(-1, 1, 1);
        if (speedX > 0) transform.localScale = new Vector3(1, 1, 1);

        if (!receivingDamage)
            transform.position += new Vector3(speedX, 0f, 0f);
    }

    // ========= Daño por RPC =========
    [PunRPC]
    public void RPC_ReceiveDamage(Vector2 direction, int amountDamage)
    {
        receiveDamage(direction, amountDamage);
    }

    public void receiveDamage(Vector2 direction, int amountDamage)
    {
        if (!photonView.IsMine) return;
        if (receivingDamage) return;

        receivingDamage = true;
        vida -= amountDamage;
        actualizarCorazones();

        Vector2 rebound = new Vector2(transform.position.x - direction.x, 1).normalized;
        rb.AddForce(rebound * reboundForce, ForceMode2D.Impulse);

        if (vida <= 0)
        {
            TryShowLose();         // muestra derrota solo en este cliente
            Time.timeScale = 0.0f; // pausa local
        }
    }

    public void desactiveDamage()
    {
        receivingDamage = false;
        rb.linearVelocity = Vector2.zero; // ¡no uses linearVelocity!
    }

    public void Attacking() { attacking = true; }
    public void desactiveAttack() { attacking = false; }

    public void actualizarCorazones()
    {
        if (!photonView.IsMine) return;

        if (hudHealth && hudHealth.Heart != null && hudHealth.Heart.Length > 0)
        {
            hudHealth.actualizarCorazones(vida);
            return;
        }

        if (Heart == null || Heart.Length == 0) return;

        for (int i = 0; i < Heart.Length; i++)
            if (Heart[i]) Heart[i].gameObject.SetActive(i < vida);
    }

    public void actualizarCrystals()
    {
        if (!photonView.IsMine) return;

        crystals++;
        if (crystalText) crystalText.text = crystals + " x";
        if (crystals >= 30)
        {
            TryShowWin();
            Time.timeScale = 0.0f;
        }
    }

    // ========= Métodos que HUDBinder puede llamar (para evitar CS1061) =========
    public void BindHUD(Image[] heartsFromScene, TMP_Text crystalFromScene)
    {
        if (!photonView.IsMine) return;

        if (heartsFromScene != null && heartsFromScene.Length > 0)
            Heart = heartsFromScene;

        if (crystalFromScene != null)
            crystalText = crystalFromScene;

        actualizarCorazones();
        if (crystalText) crystalText.text = crystals + " x";
    }

    public void BindHUD(Image[] heartsFromScene, TMP_Text crystalFromScene,
                        GameObject finalCanvasFromScene, GameObject winCanvasFromScene)
    {
        if (!photonView.IsMine) return;

        if (heartsFromScene != null && heartsFromScene.Length > 0)
            Heart = heartsFromScene;

        if (crystalFromScene != null)
            crystalText = crystalFromScene;

        if (finalCanvasFromScene != null) final_Canvas = finalCanvasFromScene;
        if (winCanvasFromScene != null) won_Canvas = winCanvasFromScene;

        if (final_Canvas) final_Canvas.SetActive(false);
        if (won_Canvas) won_Canvas.SetActive(false);

        actualizarCorazones();
        if (crystalText) crystalText.text = crystals + " x";
    }

    // ========= Helpers HUD =========
    void AttemptAutoWireHUD()
    {
        // canvases por TAG si no los asignó HUDBinder
        if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
        if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);

        // hearts: primero intenta script Health
        hudHealth = FindObjectOfType<Health>(true);
        bool healthOk = hudHealth && hudHealth.Heart != null && hudHealth.Heart.Length > 0;

        if (!healthOk && (Heart == null || Heart.Length == 0))
        {
            var root = FindWithTagSafe(heartsRootTag);
            if (root)
            {
                var imgs = root.GetComponentsInChildren<Image>(true);
                Heart = imgs
                    .Where(i => i && i.gameObject.name.ToLower().Contains("heart"))
                    .OrderBy(i => ExtractIndex(i.gameObject.name))
                    .ToArray();
            }
        }

        // contador de cristales
        var ct = FindWithTagSafe(crystalCounterTag);
        if (!ct) ct = GameObject.Find("Contador_Crystals");
        if (ct) crystalText = ct.GetComponent<TMP_Text>();
    }

    void TryShowLose()
    {
        if (!photonView.IsMine) return;
        if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
        if (final_Canvas) final_Canvas.SetActive(true);
    }

    void TryShowWin()
    {
        if (!photonView.IsMine) return;
        if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);
        if (won_Canvas) won_Canvas.SetActive(true);
    }

    GameObject FindWithTagSafe(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return null;
        try { return GameObject.FindWithTag(tagName); }
        catch { return null; } // si el tag no existe
    }

    int ExtractIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        var m = Regex.Match(name, @"\((\d+)\)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) return n;
        return 0;
    }
}
