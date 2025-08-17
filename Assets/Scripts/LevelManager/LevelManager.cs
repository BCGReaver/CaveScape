/**
 * @file LevelManager.cs
 * @brief Controla el menú de pausa, salir al menú principal y cerrar el juego.
 *
 * @details
 * La idea es que con la tecla `Esc` abrimos/cerramos el menú de pausa:
 * - Muestra/oculta el canvas de pausa.
 * - Congela/descongela el tiempo con `Time.timeScale`.
 * - Tiene botones para salir al menú (carga la escena 0) o cerrar el juego.
 *
 * Notitas rápidas:
 * - `Time.timeScale = 0` pausa animaciones y `Update()` dependientes de tiempo,
 *   pero cosas como audio o coroutines con `WaitForSecondsRealtime` siguen otro rollo.
 * - Asegúrate de arrastrar el GameObject del UI de pausa en el inspector al campo `pause`.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * @class LevelManager
 * @brief Admin de pausa: toggle con Esc, volver al menú y salir del juego.
 */
public class LevelManager : MonoBehaviour
{
    /// @brief Canvas/raíz del menú de pausa (activar/desactivar).
    [SerializeField]
    private GameObject pause;

    /// @brief Flag para saber si estamos en pausa o no.
    private bool isPaused;

    /**
     * @brief Se corre al inicio, dejamos la pausa apagada por si las dudas.
     */
    void Start()
    {
        isPaused = false;
    }

    /**
     * @brief Revisa cada frame si presionaste `Esc` para pausar/reanudar.
     */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    /**
     * @brief Alterna el estado de pausa (on/off).
     *
     * @details
     * - Si no estaba en pausa: enciende el canvas y pone timeScale en 0.
     * - Si estaba en pausa: apaga el canvas y regresa el timeScale a 1.
     */
    public void Pause()
    {
        if (isPaused == false)
        {
            pause.SetActive(true);
            Time.timeScale = 0.0f;
            isPaused = true;
        }
        else
        {
            pause.SetActive(false);
            Time.timeScale = 1.0f;
            isPaused = false;
        }
    }

    /**
     * @brief Carga la escena del menú principal (índice 0) y quita la pausa.
     * @note Asegúrate de tener la escena 0 configurada como tu Main Menu en Build Settings.
     */
    public void ExitToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(0);
    }

    /**
     * @brief Cierra el juego (o detiene el Play en el editor).
     *
     * @details
     * - En Editor: detiene el modo Play.
     * - En build: llama a `Application.Quit()`.
     */
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
