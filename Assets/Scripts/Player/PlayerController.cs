/**
 * @file PlayerController.cs
 * @brief Control del jugador 2D con movimiento, salto, daño, ataque y HUD, sincronizado con Photon.
 *
 * @details
 * Este script vive en el Player y se encarga de:
 * - Mover al mono lateralmente y saltar cuando está en el piso.
 * - Recibir daño por RPC (para que todos vean el golpe) y mostrar derrota si te quedas sin vida.
 * - Llevar la cuenta de cristales y mostrar victoria al llegar a 30 (todo esto **local**).
 * - Conectarse con el HUD (corazones, contador de cristales, canvases) ya sea por inyección directa
 *   (HUDBinder) o buscándolos por TAG si no se asignaron en el inspector.
 *
 * Notas rápidas:
 * - Solo el dueño del PhotonView (IsMine) procesa input y toca el HUD local.
 * - El raycast hacia abajo detecta si estás pisando suelo para habilitar el salto.
 * - `LocalPlayer.TagObject` lo usa tu spawner para evitar doble spawn (ver NetworkSpawner).
 *
 * @author Tú
 * @date 2025-08-16
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Linq;
using System.Text.RegularExpressions;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
/**
 * @class PlayerController
 * @brief Control principal del personaje: movimiento, salto, daño con rebote, ataque y UI local.
 *
 * @remarks
 * - Asegúrate de que el objeto tenga PhotonView, Rigidbody2D y Animator.
 * - El Animator espera bools/triggers: "onFloor", "receiveDamage", "Attacking" y float "movement".
 */
public class PlayerController : MonoBehaviourPun
{
    // =================== Ajustes de movimiento ===================

    [Header("Movement")]
    /// @brief Velocidad horizontal base (se multiplica por input y deltaTime).
    public float speed = 5f;

    /// @brief Fuerza del salto en unidades de impulso (Impulse).
    public float jumpForce = 5f;

    /// @brief Fuerza con la que sales disparado cuando te pegan (knockback).
    public float reboundForce = 3f;

    /// @brief Largo del raycast hacia abajo para checar el piso (ajústalo a tu sprite/colisionador).
    public float raycastLength = 0.56f;

    /// @brief Capas que cuentan como “suelo” para el raycast.
    public LayerMask floorLayer;

    // =================== Vida y HUD ===================

    [Header("Health")]
    /// @brief Vida actual del jugador (máximo visualizado por corazoncitos).
    public int vida = 3;

    /// @brief Arreglo de imágenes de corazones en el HUD (se puede llenar en runtime).
    public Image[] Heart; // Se llena en runtime (HUDBinder o por TAG)

    [Header("UI (opcional, se resuelve por TAG si está vacío)")]
    /// @brief Canvas de derrota (se activa al morir, local).
    public GameObject final_Canvas; // derrota

    /// @brief Canvas de victoria (se activa al ganar por cristales, local).
    public GameObject won_Canvas;   // victoria

    [Header("Crystals (local)")]
    /// @brief Contador local de cristales (esto no es compartido en red por ahora).
    public int crystals = 0;

    /// @brief Referencia al Animator para disparar estados (correr, salto, daño, ataque).
    public Animator animator;

    // =================== Internos (runtime) ===================

    /// @brief Rigid del player para fuerzas, salto y knockback.
    private Rigidbody2D rb;

    /// @brief Flags de estado: en piso, recibiendo daño, atacando.
    private bool onFloor, receivingDamage, attacking;

    // HUD cacheado
    /// @brief Texto TMP para mostrar el conteo de cristales.
    private TMP_Text crystalText;

    /// @brief Si el HUD trae un script Health con los corazones, lo usamos primero.
    private Health hudHealth;

    // =================== TAGs de fallback (por si HUDBinder no conectó) ===================

    [SerializeField] private string crystalCounterTag = "HUD_CrystalText";
    [SerializeField] private string heartsRootTag = "HUD_HeartsRoot";
    [SerializeField] private string loseCanvasTag = "HUD_LoseCanvas";
    [SerializeField] private string winCanvasTag = "HUD_WinCanvas";

    /**
     * @brief Inicializa referencias y resuelve HUD (si soy el dueño del PhotonView).
     *
     * @details
     * - Desactiva canvases de win/lose para que no salgan al inicio.
     * - Intenta cablear HUD automáticamente por TAG si no vino por HUDBinder.
     * - Actualiza corazones y contador de cristales.
     */
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
            // Si no soy el dueño, aseguro canvases apagados por si acaso
            if (final_Canvas) final_Canvas.SetActive(false);
            if (won_Canvas) won_Canvas.SetActive(false);
        }
    }

    /**
     * @brief Loop por frame para input local, salto y estados del Animator.
     *
     * @details
     * - Solo procesa input si IsMine.
     * - Mueve al jugador y hace raycast hacia abajo para detectar el piso.
     * - Permite ataque con clic izq cuando estás en el piso.
     * - Tecla K (Editor) simula daño por RPC; L te cura 1 (tope en 3) para probar HUD.
     */
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
        // Teclas de prueba en editor
        if (Input.GetKeyDown(KeyCode.K)) photonView.RPC(nameof(RPC_ReceiveDamage), photonView.Owner, Vector2.left, 1);
        if (Input.GetKeyDown(KeyCode.L)) { vida = Mathf.Min(3, vida + 1); actualizarCorazones(); }
#endif
    }

    /**
     * @brief Aplica movimiento horizontal y voltea el sprite según dirección.
     *
     * @details
     * - Usa GetAxis("Horizontal") para compatibilidad con teclado/joystick.
     * - Anima "movement" con el valor absoluto de la velocidad.
     * - No mueve si está en estado de recibir daño (knockback manda).
     */
    void Movement()
    {
        float speedX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;

        animator.SetFloat("movement", Mathf.Abs(speedX) * speed);
        if (speedX < 0) transform.localScale = new Vector3(-1, 1, 1);
        if (speedX > 0) transform.localScale = new Vector3(1, 1, 1);

        if (!receivingDamage)
            transform.position += new Vector3(speedX, 0f, 0f);
    }

    // =================== Daño por RPC ===================

    /**
     * @brief RPC para que todos sepan que este player recibió daño.
     * @param direction Dirección aproximada del golpe (para el rebote).
     * @param amountDamage Cantidad de daño recibido.
     */
    [PunRPC]
    public void RPC_ReceiveDamage(Vector2 direction, int amountDamage)
    {
        receiveDamage(direction, amountDamage);
    }

    /**
     * @brief Lógica local de recibir daño: resta vida, actualiza HUD y aplica rebote.
     * @param direction Dirección desde donde vino el golpe.
     * @param amountDamage Cuánto daño aplicar.
     *
     * @details
     * - Ignora si no soy el dueño o si ya estaba en animación de daño.
     * - Si la vida llega a 0, muestra derrota y pausa el tiempo (local).
     */
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

    /**
     * @brief Fin de la animación/estado de daño; resetea flags y velocidad.
     * @note Suele llamarse desde un Animation Event al terminar la animación de “hit”.
     * @bug Aquí se usa `rb.linearVelocity`, pero en Rigidbody2D la propiedad correcta es `velocity`.
     *      Cambia a: `rb.velocity = Vector2.zero;`
     */
    public void desactiveDamage()
    {
        receivingDamage = false;
        rb.linearVelocity = Vector2.zero; // ¡no uses linearVelocity! (ver nota @bug arriba)
    }

    /// @brief Activa el estado de ataque (normalmente un trigger de animación).
    public void Attacking() { attacking = true; }

    /// @brief Desactiva el estado de ataque (Animation Event al terminar el golpe).
    public void desactiveAttack() { attacking = false; }

    /**
     * @brief Refresca los corazones del HUD según la vida actual.
     *
     * @details
     * - Si hay script Health en el HUD, delega ahí (tiene su propia lógica).
     * - Si no, prendo/apago imágenes del arreglo `Heart`.
     */
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

    /**
     * @brief Suma 1 cristal, actualiza texto y checa victoria al llegar a 30 (local).
     */
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

    // =================== API para HUDBinder (evitar CS1061) ===================

    /**
     * @brief Conecta HUD básico (corazones + texto de cristales).
     * @param heartsFromScene Arreglo de imágenes de corazón.
     * @param crystalFromScene Texto TMP para el contador de cristales.
     */
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

    /**
     * @brief Conecta HUD extendido (corazones + cristales + canvases de win/lose).
     */
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

    // =================== Helpers para cablear HUD por TAG ===================

    /**
     * @brief Intenta encontrar HUD automáticamente por TAG/nombre si no se inyectó por HUDBinder.
     *
     * @details
     * - Busca canvases por TAG (lose/win).
     * - Si no hay script Health con hearts, busca imágenes llamadas "Heart (n)" bajo un root.
     * - Busca el TMP_Text del contador por TAG o por nombre "Contador_Crystals".
     */
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

    /**
     * @brief Muestra el canvas de derrota (solo local).
     */
    void TryShowLose()
    {
        if (!photonView.IsMine) return;
        if (!final_Canvas) final_Canvas = FindWithTagSafe(loseCanvasTag);
        if (final_Canvas) final_Canvas.SetActive(true);
    }

    /**
     * @brief Muestra el canvas de victoria (solo local).
     */
    void TryShowWin()
    {
        if (!photonView.IsMine) return;
        if (!won_Canvas) won_Canvas = FindWithTagSafe(winCanvasTag);
        if (won_Canvas) won_Canvas.SetActive(true);
    }

    /**
     * @brief Versión segura de `FindWithTag` que no truena si el Tag no existe.
     * @param tagName Nombre del Tag a buscar.
     * @return GameObject con ese Tag o null si no hay/si el Tag no está definido.
     */
    GameObject FindWithTagSafe(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return null;
        try { return GameObject.FindWithTag(tagName); }
        catch { return null; } // si el tag no existe
    }

    /**
     * @brief Extrae el índice entre paréntesis del nombre tipo "Heart (2)".
     * @param name Nombre del GameObject.
     * @return El número del paréntesis o 0 si no hay.
     */
    int ExtractIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        var m = Regex.Match(name, @"\((\d+)\)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) return n;
        return 0;
    }
}
