/**
 * @file HUDBinder.cs
 * @brief Conecta (bindea) el HUD de la escena con el Player local que está controlando el usuario.
 *
 * @details
 * Este script se monta en algún GameObject de la escena (por ejemplo, un "HUD" vacío).
 * Lo que hace es esperar a que exista un PlayerController que sea "mío" (IsMine con Photon),
 * y cuando lo encuentra, le pasa las referencias del HUD (corazones, contador de cristales
 * y canvases de win/lose) para que el player actualice esos elementos cuando tome daño,
 * gane cristales o termine la partida.
 *
 * Cosas a tener en mente:
 * - Usa un IEnumerator Start() para esperar frame a frame hasta que aparezca el Player local.
 * - Si hay canvases de victoria/derrota, llama a la sobrecarga que los recibe; si no, usa la básica.
 * - Esto evita null refs cuando el Player aparece un pelín después del HUD.
 *
 * @author Tú
 * @date 2025-08-16
 */

using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * @class HUDBinder
 * @brief Script que amarra los elementos del HUD con el Player local (el que tú controlas).
 *
 * @remarks
 * Piensa que este compa es el "cable" entre la UI y tu Player. Él no dibuja nada,
 * solo conecta referencias para que el Player las use.
 */
public class HUDBinder : MonoBehaviour
{
    [Header("Arrastra desde la escena")]

    /// @brief Arreglo de imágenes para las vidas (corazones). 
    /// @details Orden típico: Heart, Heart (1), Heart (2). Si tienes más o menos, no pasa nada.
    public Image[] hearts;              // Heart, Heart (1), Heart (2)

    /// @brief Texto de TMP donde sale el conteo de cristales.
    /// @note Es opcional; si no lo arrastras, solo no mostrará el número pero no truena.
    public TMP_Text crystalCounter;     // Contador_Crystals (opcional)

    /// @brief Canvas que muestra la pantalla de derrota.
    /// @warning Déjalo desactivado en el editor; el Player lo activará cuando pierdas.
    public GameObject loseCanvas;       // Canvas de derrota (desactivado por defecto)

    /// @brief Canvas que muestra la pantalla de victoria.
    /// @warning Igual: desactivado por defecto; el Player lo prende cuando ganes.
    public GameObject winCanvas;        // Canvas de victoria (desactivado por defecto)

    /**
     * @brief Coroutine de inicio que busca al Player local y le conecta el HUD.
     *
     * @details
     * Aquí hacemos un loop que cada frame checa:
     * - Busca todos los PlayerController.
     * - Se queda con el que sea local (PhotonView.IsMine).
     * - Cuando lo encuentra, llama a BindHUD en el Player y termina.
     *
     * @return IEnumerator Usado por Unity para correr la coroutine.
     *
     * @note Usamos coroutine porque a veces el Player spawnea después que el HUD;
     * así evitamos carreras y nulls.
     */
    private IEnumerator Start()
    {
        // Espera a que el Player local exista (o sea, el que tú controlas con Photon)
        while (true)
        {
            // Buscamos todos los PlayerController que haya vivos
            var players = FindObjectsOfType<PlayerController>();

            // Nos quedamos con el que tenga PhotonView y sea el "mío"
            var local = players.FirstOrDefault(p => p && p.photonView && p.photonView.IsMine);

            if (local != null)
            {
                // Llama a cualquiera de los 2 overloads; ambos existen en PlayerController:
                // - Con canvases (win/lose)
                // - O solo con corazones y contador
                if (loseCanvas || winCanvas)
                    local.BindHUD(hearts, crystalCounter, loseCanvas, winCanvas);
                else
                    local.BindHUD(hearts, crystalCounter);

                // Log para que veas en consola que sí se conectó y cuántos corazones hay
                Debug.Log($"[HUDBinder] Bound -> {local.name} hearts={(hearts != null ? hearts.Length : 0)}");

                // Terminamos la coroutine porque ya hicimos la chamba
                yield break;
            }

            // Si todavía no aparece, esperamos un frame y volvemos a intentar
            yield return null;
        }
    }
}
