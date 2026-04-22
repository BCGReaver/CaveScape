using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Linq;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerControllerSolo : MonoBehaviourPun
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float reboundForce = 3f;
    public float raycastLength = 0.56f;
    public LayerMask floorLayer;

    [Header("Health")]
    public int vida = 3;
    public Image[] Heart;

    [Header("UI Fallback Tags")]
    public GameObject final_Canvas;
    public GameObject won_Canvas;
    public int crystals = 0;
    public Animator animator;

    private Rigidbody2D rb;
    private bool onFloor, receivingDamage, attacking;
    private TMP_Text crystalText;
    private Health hudHealth;

    [SerializeField] private string crystalCounterTag = "HUD_CrystalText";
    [SerializeField] private string heartsRootTag = "HUD_HeartsRoot";
    [SerializeField] private string loseCanvasTag = "HUD_LoseCanvas";
    [SerializeField] private string winCanvasTag = "HUD_WinCanvas";

    // Propiedad m�gica: Si no hay internet/Photon, somos "due�os" por defecto.
    // Si no hay photonView (modo solo total) o si es mío, tenemos control.
    private bool IsLocalControl => photonView == null || !PhotonNetwork.IsConnected || photonView.IsMine;

    void Start()
    {
        
        // Si estamos en la escena "Solo", podemos ignorar a Photon
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Solo")
            {
         // Esto es un truco: si no hay photonView, el script seguirá
        Debug.Log("Modo Solo detectado: Control habilitado");
        }
            // ... resto de tu código
        
        bool isStandalone = (photonView == null || !PhotonNetwork.IsConnected);

        if (isStandalone || photonView.IsMine)
        {
            // ... (tu código de inicialización de UI)
        }
        rb = GetComponent<Rigidbody2D>();

        if (IsLocalControl)
        {
            if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
            if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);
            if (final_Canvas) final_Canvas.SetActive(false);
            if (won_Canvas) won_Canvas.SetActive(false);

            AttemptAutoWireHUD();
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
        if (!IsLocalControl) return;

        if (!attacking)
        {
            Movement();

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLength, floorLayer);
            onFloor = hit.collider != null;

            if (onFloor && Input.GetKeyDown(KeyCode.Space) && !receivingDamage)
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.jump);
        }

        if (Input.GetMouseButtonDown(0) && !attacking && onFloor)
            Attacking();

        animator.SetBool("onFloor", onFloor);
        animator.SetBool("receiveDamage", receivingDamage);
        animator.SetBool("Attacking", attacking);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (PhotonNetwork.IsConnected)
                photonView.RPC(nameof(RPC_ReceiveDamage), RpcTarget.All, (Vector2)transform.position + Vector2.right, 1);
            else
                receiveDamage((Vector2)transform.position + Vector2.right, 1);
        }
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

    [PunRPC]
    public void RPC_ReceiveDamage(Vector2 direction, int amountDamage)
    {
        receiveDamage(direction, amountDamage);
    }

    public void receiveDamage(Vector2 direction, int amountDamage)
    {
        if (!IsLocalControl) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.getHit);

        if (receivingDamage) return;

        receivingDamage = true;
        vida -= amountDamage;
        actualizarCorazones();

        Vector2 rebound = new Vector2(transform.position.x - direction.x, 1).normalized;
        rb.AddForce(rebound * reboundForce, ForceMode2D.Impulse);


        if (vida <= 0)
        {
            TryShowLose();
            Time.timeScale = 0.0f;
        }
    }

    public void desactiveDamage()
    {
        receivingDamage = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void Attacking() { attacking = true; }
    public void desactiveAttack() { attacking = false; }

    public void actualizarCorazones()
    {
        if (!IsLocalControl) return;

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
        // Quitamos el IsLocalControl de aquí para que el cristal pueda llamarlo
        // sin que Photon ponga trabas
        crystals++;
        Debug.Log("Crystals actuales: " + crystals);

        if (crystalText) crystalText.text = crystals + " x";

        if (crystals >= 30)
        {
            TryShowWin();
            Time.timeScale = 0.0f;
        }
    }

    // --- M�TODOS QUE FALTABAN (Los "Helpers") ---

    void AttemptAutoWireHUD()
    {
        if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
        if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);

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

        var ct = FindWithTagSafe(crystalCounterTag);
        if (!ct) ct = GameObject.Find("Contador_Crystals");
        if (ct) crystalText = ct.GetComponent<TMP_Text>();
    }

    void TryShowLose()
    {
        if (!IsLocalControl) return;
        if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
        if (final_Canvas) final_Canvas.SetActive(true);
    }

    void TryShowWin()
    {
        if (!IsLocalControl) return;
        if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);
        if (won_Canvas) won_Canvas.SetActive(true);
    }

    GameObject FindWithTagSafe(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return null;
        try { return GameObject.FindWithTag(tagName); }
        catch { return null; }
    }

    int ExtractIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        var m = Regex.Match(name, @"\((\d+)\)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) return n;
        return 0;
    }

    // API para inyecci�n manual desde otros scripts
    public void BindHUD(Image[] hearts, TMP_Text text, GameObject lose, GameObject win)
    {
        if (!IsLocalControl) return;
        Heart = hearts;
        crystalText = text;
        final_Canvas = lose;
        won_Canvas = win;
        actualizarCorazones();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Asumiendo que tus cristales tienen el Tag "Crystal"
        if (collision.CompareTag("Crystal"))
        {
            actualizarCrystals();
            Destroy(collision.gameObject); // Borra el cristal del mapa
        }
    }

    /*public void PlayFootstepSound()
    {
        if (onFloor && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.footsteps);
    }*/

    public void PlayFootstepSound()
    {
        if (onFloor && AudioManager.Instance != null)
        {
            // El pitch variable para que no canse
            float randomPitch = Random.Range(0.9f, 1.1f);

            // Llamamos a la rutina: audio de pasos, duración de 0.4 segundos
            StartCoroutine(PlayStepCut(AudioManager.Instance.footsteps, 0.4f, randomPitch));
        }
    }

    private System.Collections.IEnumerator PlayStepCut(AudioClip clip, float duration, float pitch)
    {
        GameObject tempStep = new GameObject("StepAudio");
        AudioSource source = tempStep.AddComponent<AudioSource>();
        source.clip = clip;
        source.pitch = pitch;
        source.Play();

        yield return new WaitForSeconds(duration);

        source.Stop();
        Destroy(tempStep);
    }
}