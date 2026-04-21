using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun; // Añadimos esto

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    private bool isPaused = false;

    void Start()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        isPaused = false;
        Time.timeScale = 1.0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1.0f;

        // Limpieza de Photon antes de salir
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        SceneManager.LoadScene(0); // Asegúrate de que el menú sea el índice 0
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}